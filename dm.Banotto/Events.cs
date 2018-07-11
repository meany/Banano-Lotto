using Discord;
using Discord.Commands;
using Discord.WebSocket;
using dm.Banotto.Data;
using dm.Banotto.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dm.Banotto
{
    public class Events
    {
        private readonly CommandService _commands;
        private readonly DiscordSocketClient _client;
        private readonly IServiceProvider _services;
        private readonly Config _config;
        private readonly AppDbContext _db;

        public Events(CommandService commands, DiscordSocketClient client, IServiceProvider services, Config config, AppDbContext db)
        {
            _commands = commands;
            _client = client;
            _services = services;
            _config = config;
            _db = db;
        }

        public async Task HandleCommand(SocketMessage messageParam)
        {
            var message = messageParam as SocketUserMessage;
            if (message == null)
            {
                return;
            }

            int argPos = 0;
            var context = new CommandContext(_client, message);

            // filter to only our betting channel
            if (message.Channel.Id != _config.GameChannelId)
            {
                return;
            }

            // watch for $ dealer command (DM/channel)
            var user = (IGuildUser)message.Author;
            if (user.RoleIds.Contains(_config.DealerRoleId))
            {
                if (message.HasCharPrefix('$', ref argPos))
                {
                    var result = await _commands.ExecuteAsync(context, argPos, _services).ConfigureAwait(false);
                    if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
                        await context.Channel.SendMessageAsync(result.ErrorReason).ConfigureAwait(false);
                    return;
                }
            }

            // determine latest round state
            var round = await _db.Rounds
                .AsNoTracking()
                .OrderByDescending(x => x.Created)
                .Include(x => x.Bets)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);

            if (round.RoundStatus == RoundStatus.Open)
            {
                // filter if isn't @Dealer bet
                if (!message.MentionedUsers.Any(x => x.Id == round.DealerId))
                {
                    return;
                }

                // filter bet prefixes
                var prefixes = new string[]
                {
                    ".t",
                    ".tip",
                };

                foreach (string p in prefixes)
                {
                    if (message.HasStringPrefix(p, ref argPos, StringComparison.OrdinalIgnoreCase))
                    {
                        var result = await _commands.ExecuteAsync(context, 1, _services).ConfigureAwait(false);
                        if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
                            await context.Channel.SendMessageAsync(result.ErrorReason).ConfigureAwait(false);
                        return;
                    }
                }
            }
            else
            {
                return;
            }
        }

        public async Task HandleConnected()
        {
            foreach (var g in _client.Guilds)
            {
                await _client.CurrentUser.ModifyAsync(x =>
                {
                    x.Username = _config.BotName;
                }).ConfigureAwait(false);
            }
        }

        public async Task HandleReaction(Cacheable<IUserMessage, ulong> msg, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (channel == null || reaction == null)
            {
                return;
            }

            if (channel.Id == _config.GameChannelId && reaction.UserId == _config.TipBotId)
            {
                var item = await _db.Bets
                    .Where(x => x.UserBetMessageId == reaction.MessageId)
                    .Include(x => x.Round)
                    .FirstOrDefaultAsync()
                    .ConfigureAwait(false);
                if (item.BetId > 0)
                {
                    var betMsg = reaction.Channel.GetCachedMessage(item.UserBetMessageId);
                    string user = betMsg.Author.Username;

                    if (reaction.Emote.Name == _config.EmoteGood)
                    {
                        await BetConfirmation(item, betMsg, user).ConfigureAwait(false);
                    }
                    else if (reaction.Emote.Name == _config.EmoteBad)
                    {
                        await DeleteBet(item).ConfigureAwait(false);
                    }
                }
            }
        }

        private async Task BetConfirmation(Bet item, SocketMessage betMsg, string user)
        {
            string play = item.PlayType.ToString();
            string quick = (item.IsQuick) ? " (quick)" : string.Empty;

            int win = 0;
            int secs = 0;
            switch (item.Round.RoundType)
            {
                case RoundType.Pick1:
                    win = AdminModule.PICK1_MULTI_SINGLE * item.Amount;
                    secs = _config.Secs1;
                    break;
                case RoundType.Pick2:
                    if (item.PlayType == PlayType.Straight)
                    {
                        win = AdminModule.PICK2_MULTI_STRAIGHT * item.Amount;
                    }
                    else if (item.PlayType == PlayType.Any)
                    {
                        win = AdminModule.PICK2_MULTI_ANY * item.Amount;
                    }
                    secs = _config.Secs2;
                    break;
                case RoundType.Pick3:
                    if (item.PlayType == PlayType.Straight)
                    {
                        win = AdminModule.PICK3_MULTI_STRAIGHT * item.Amount;
                    }
                    else if (item.PlayType == PlayType.Any)
                    {
                        win = AdminModule.PICK3_MULTI_ANY * item.Amount;
                    }
                    secs = _config.Secs3;
                    break;
            }

            bool first = false;
            if (!item.Round.Ends.HasValue)
            {
                item.Round.Ends = DateTime.Now.AddSeconds(secs);
                first = true;
            }

            int secsLeft = (int)(item.Round.Ends.Value - DateTime.Now).TotalSeconds + 1;
            string roundTypeStr = Utils.GetRoundTypeName(item.Round.RoundType);

            var builder = new EmbedBuilder()
                .WithColor(Color.SUCCESS)
                .WithFooter(footer =>
                {
                    footer.WithText($"Bet #{item.BetId} — Round ends in {Utils.ConvertToCompoundDuration(secsLeft)}")
                        .WithIconUrl(Asset.CLOCK);
                })
                .WithAuthor(author =>
                {
                    author.WithName($"Pick Number Lotto | Round #{item.RoundId} ({roundTypeStr})")
                        .WithIconUrl(Asset.SUCCESS);
                })
                .AddField($"{play} play '{item.Pick1}{item.Pick2}{item.Pick3}'{quick}, placed by @{user} for {item.Amount.AddCommas()}.",
                    $"They could win a total of {win.AddCommas()}!");
            var embed = builder.Build();

            await betMsg.Channel.SendMessageAsync(string.Empty, embed: embed).ConfigureAwait(false);

            item.Confirmed = true;
            _db.Update(item);
            await _db.SaveChangesAsync().ConfigureAwait(false);

            if (first)
            {
                var context = new CommandContext(_client, (IUserMessage)betMsg);
                _ = Task.Delay(secsLeft * 1000)
                    .ContinueWith(_ => _commands.ExecuteAsync(context, "close", _services).ConfigureAwait(false))
                    .ConfigureAwait(false);
            }
        }

        private async Task DeleteBet(Bet item)
        {
            _db.Bets.Remove(item);
            await _db.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}

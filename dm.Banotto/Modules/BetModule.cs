using Discord;
using Discord.Commands;
using dm.Banotto.Data;
using dm.Banotto.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dm.Banotto
{
    public class BetModule : ModuleBase
    {
        private readonly Config _config;
        private readonly AppDbContext _db;

        private readonly string REFUND = "DM dealer (@{0}) for refund. Refund not guaranteed.";

        public BetModule(IOptions<Config> config, AppDbContext db)
        {
            _config = config.Value;
            _db = db;
        }

        [Command("bet"), Summary("Place bet."), RequireContext(ContextType.Guild)]
        public async Task Bet([Remainder] string msg)
        {
            await Tip(msg).ConfigureAwait(false);
        }

        [Command("t"), Summary("Place bet."), RequireContext(ContextType.Guild)]
        public async Task T([Remainder] string msg)
        {
            await Tip(msg).ConfigureAwait(false);
        }

        [Command("tip"), Summary("Place bet."), RequireContext(ContextType.Guild)]
        public async Task Tip([Remainder] string msg)
        {
            var b = ParseBet(msg);
            await MakeBet(b).ConfigureAwait(false);
        }

        private async Task MakeBet(BetCommand b)
        {
            if (Context.Message.MentionedUserIds.Count == 1)
            {
                ulong mentionedId = Context.Message.MentionedUserIds.First();
                var dealer = await Context.Guild.GetUserAsync(mentionedId).ConfigureAwait(false);
                try
                {
                    var round = await _db.Rounds
                        .AsNoTracking()
                        .Where(x => x.RoundStatus == RoundStatus.Open)
                        .Include(x => x.Bets)
                        .FirstOrDefaultAsync()
                        .ConfigureAwait(false);
                    if (round != null && round.DealerId == mentionedId)
                    {
                        int min = 0;
                        int max = 0;
                        string roundTypeStr = Utils.GetRoundTypeName(round.RoundType);
                        switch (round.RoundType)
                        {
                            case RoundType.Pick1:
                                min = _config.Min1;
                                max = _config.Max1;
                                break;
                            case RoundType.Pick2:
                                min = (b.PlayType == PlayType.Straight) ? _config.Min2Str : _config.Min2Any;
                                max = (b.PlayType == PlayType.Straight) ? _config.Max2Str : _config.Max2Any;
                                break;
                            case RoundType.Pick3:
                                min = (b.PlayType == PlayType.Straight) ? _config.Min3Str : _config.Min3Any;
                                max = (b.PlayType == PlayType.Straight) ? _config.Max3Str : _config.Max3Any;
                                break;
                        }

                        // parse bet itself
                        if (b.Amount >= min && b.Amount <= max)
                        {
                            // can only bet max
                            int total = round.Bets
                                .Where(x => x.UserId == Context.User.Id &&
                                    (round.RoundType == RoundType.Pick1 || x.PlayType == b.PlayType))
                                .Sum(x => x.Amount);
                            int userTotal = total + b.Amount.Value;
                            if (userTotal > max)
                            {
                                await SendBetError(round, roundTypeStr,
                                    $"Bet amount **{b.Amount.Value.AddCommas()}** is too high.\nFor this round, the max bet is **{max.AddCommas()}** for {b.PlayType} and you've bet a total of **{total.AddCommas()}**.\n" +
                                    string.Format(REFUND, dealer)).ConfigureAwait(false);
                            }
                            // parse picks
                            else if (round.RoundType == RoundType.Pick1)
                            {
                                if (b.Pick1 >= 0 && b.Pick1 <= 9 &&
                                    (b.Quick || (!b.Pick2.HasValue && !b.Pick3.HasValue)))
                                {
                                    await MakeBetDb(b, round).ConfigureAwait(false);
                                }
                                else
                                {
                                    await SendBetError(round, roundTypeStr,
                                        $"Picks not parsed (out of range, **0** - **9**)\n" +
                                            string.Format(REFUND, dealer)).ConfigureAwait(false);
                                }
                            }
                            else if (round.RoundType == RoundType.Pick2)
                            {
                                if (b.Pick1 >= 0 && b.Pick2 >= 0 &&
                                    b.Pick1 <= 9 && b.Pick2 <= 9 &&
                                    (b.Quick || !b.Pick3.HasValue))
                                {
                                    await MakeBetDb(b, round).ConfigureAwait(false);
                                }
                                else
                                {
                                    await SendBetError(round, roundTypeStr,
                                        $"Picks not parsed (out of range, **00** - **99**)\n" +
                                            string.Format(REFUND, dealer)).ConfigureAwait(false);
                                }
                            }
                            else if (round.RoundType == RoundType.Pick3)
                            {
                                if (b.Pick1 >= 0 && b.Pick2 >= 0 && b.Pick3 >= 0 &&
                                    b.Pick1 <= 9 && b.Pick2 <= 9 && b.Pick3 <= 9)
                                {
                                    await MakeBetDb(b, round).ConfigureAwait(false);
                                }
                                else
                                {
                                    await SendBetError(round, roundTypeStr,
                                        $"Picks not parsed (out of range, **000** - **999**)\n" +
                                            string.Format(REFUND, dealer)).ConfigureAwait(false);
                                }
                            }
                        }
                        else
                        {
                            string playType = (round.RoundType == RoundType.Pick1) ? PlayType.Single.ToString() : b.PlayType.Value.ToString();
                            await SendBetError(round, roundTypeStr,
                                $"Bet amount not parsed (out of range, **{min}** - **{max}** for **{playType}** play)\n" +
                                string.Format(REFUND, dealer)).ConfigureAwait(false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    await Context.Message.Channel.SendMessageAsync(ex.Message).ConfigureAwait(false);
                }
            }
        }

        private async Task SendBetError(Round round, string roundTypeStr, string errorText)
        {
            string footerText = string.Empty;
            if (round.Ends.HasValue)
            {
                int secsLeft = (int)(round.Ends.Value - DateTime.Now).TotalSeconds;
                footerText = $"ends in {Utils.ConvertToCompoundDuration(secsLeft)}";
            }
            else
            {
                footerText = "has no end time";
            }

            var builder = new EmbedBuilder()
                .WithColor(Color.ERROR)
                .WithFooter(footer =>
                {
                    footer.WithText($"Round #{round.RoundId} ({roundTypeStr}) {footerText}")
                        .WithIconUrl(Asset.CLOCK);
                })
                .WithAuthor(author =>
                {
                    author.WithName($"Bet Error")
                        .WithIconUrl(Asset.ERROR);
                })
                .WithDescription(errorText);
            var embed = builder.Build();

            await Context.Message.Channel.SendMessageAsync(Context.Message.Author.Mention, embed: embed).ConfigureAwait(false);
        }

        private async Task MakeBetDb(BetCommand b, Round round)
        {
            var bet = new Bet
            {
                Amount = b.Amount.Value,
                BetType = b.BetType.Value,
                Created = DateTime.Now,
                IsQuick = b.Quick,
                Pick1 = b.Pick1,
                Pick2 = (round.RoundType == RoundType.Pick2 || round.RoundType == RoundType.Pick3) ? b.Pick2 : null,
                Pick3 = (round.RoundType == RoundType.Pick3) ? b.Pick3 : null,
                PlayType = (round.RoundType == RoundType.Pick1) ? PlayType.Single : b.PlayType.Value,
                RoundId = round.RoundId,
                UserBetMessageId = Context.Message.Id,
                UserId = Context.User.Id
            };
            _db.Bets.Add(bet);
            await _db.SaveChangesAsync().ConfigureAwait(false);
        }

        private BetCommand ParseBet(string msg)
        {
            var b = new BetCommand
            {
                BetType = BetType.Banano,
                PlayType = PlayType.Straight
            };
            string[] parts = msg.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length >= 1)
            {
                if (int.TryParse(parts[0], out int bet))
                {
                    b.Amount = bet;
                }
            }

            if (parts.Length >= 3)
            {
                b.Command = parts[2].ToLower();
                b.Quick = false;
                if (b.Command == "q" || b.Command == "quick")
                {
                    b.Quick = true;
                    Random random = new Random();
                    b.Pick1 = random.Next(0, 9);
                    b.Pick2 = random.Next(0, 9);
                    b.Pick3 = random.Next(0, 9);
                }
                else
                {
                    if (int.TryParse(b.Command, out int fullPick) &&
                        b.Command.Length >= 1 && b.Command.Length <= 3 &&
                        fullPick >= 0 &&
                        fullPick <= 999)
                    {
                        int pick1 = -1;
                        int pick2 = -1;
                        int pick3 = -1;
                        if (int.TryParse(b.Command[0].ToString(), out pick1) &&
                            (b.Command.Length == 1 || int.TryParse(b.Command[1].ToString(), out pick2)) &&
                            (b.Command.Length <= 2 || int.TryParse(b.Command[2].ToString(), out pick3)))
                        {
                            b.Pick1 = pick1;
                            if (pick2 >= 0)
                            {
                                b.Pick2 = pick2;
                            }
                            if (pick3 >= 0)
                            {
                                b.Pick3 = pick3;
                            }
                        }
                    }
                }
            }

            if (parts.Length >= 4)
            {
                b.Play = parts[3].ToLower();
                switch (b.Play)
                {
                    case "a":
                    case "any":
                        b.PlayType = PlayType.Any;
                        break;
                    case "s":
                    case "str":
                    case "straight":
                        b.PlayType = PlayType.Straight;
                        break;
                }
            }

            return b;
        }
    }
}

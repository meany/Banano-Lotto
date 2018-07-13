using Discord;
using Discord.Commands;
using Discord.WebSocket;
using dm.Banotto.Data;
using dm.Banotto.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace dm.Banotto
{
    public class AdminModule : ModuleBase
    {
        private readonly Config _config;
        private readonly AppDbContext _db;

        public static readonly int PICK3_MULTI_STRAIGHT = 500;
        public static readonly int PICK3_MULTI_ANY = 80;
        public static readonly int PICK2_MULTI_STRAIGHT = 50;
        public static readonly int PICK2_MULTI_ANY = 25;
        public static readonly int PICK1_MULTI_SINGLE = 9; // only 10% house edge @ 9

        public AdminModule(IOptions<Config> config, AppDbContext db)
        {
            _config = config.Value;
            _db = db;
        }

        [Command("open"), Summary("Opens round.")]
        public async Task Open([Remainder] string msg)
        {
            var roundType = ParseRoundType(msg);
            var item = await _db.Rounds
                .Where(x => x.RoundStatus == RoundStatus.Open)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);
            if (item == null)
            {
                await OpenRound(roundType).ConfigureAwait(false);
            }
            else
            {
                string roundTypeStr = Utils.GetRoundTypeName(item.RoundType);
                await ReplyAsync($"Round #{item.RoundId} (**{roundTypeStr}**) still open.").ConfigureAwait(false);
            }
        }

        private async Task OpenRound(RoundType roundType)
        {
            var dealer = Context.Message.Author;
            Game game = new Game
            {
                Dealer = dealer.ToString(),
                RoundType = roundType
            };

            int? roll1 = null;
            int? roll2 = null;
            int? roll3 = null;
            Random random = new Random();
            string pSalt1 = Utils.LongRandom(random).ToString();
            string pSalt2 = Utils.LongRandom(random).ToString();
            switch (roundType)
            {
                case RoundType.Pick1:
                    roll1 = random.Next(0, 9);
                    game.RoundTime = _config.Secs1;
                    game.MinSingleBet = _config.Min1;
                    game.MinSingleWin = game.MinSingleBet * PICK1_MULTI_SINGLE;
                    game.MaxSingleBet = _config.Max1;
                    game.MaxSingleWin = game.MaxSingleBet * PICK1_MULTI_SINGLE;
                    break;
                case RoundType.Pick2:
                    roll1 = random.Next(0, 9);
                    roll2 = random.Next(0, 9);
                    game.RoundTime = _config.Secs2;
                    game.MinStraightBet = _config.Min2Str;
                    game.MinStraightWin = game.MinStraightBet * PICK2_MULTI_STRAIGHT;
                    game.MaxStraightBet = _config.Max2Str;
                    game.MaxStraightWin = game.MaxStraightBet * PICK2_MULTI_STRAIGHT;
                    game.MinAnyBet = _config.Min2Any;
                    game.MinAnyWin = game.MinAnyBet * PICK2_MULTI_ANY;
                    game.MaxAnyBet = _config.Max2Any;
                    game.MaxAnyWin = game.MaxAnyBet * PICK2_MULTI_ANY;
                    break;
                case RoundType.Pick3:
                    roll1 = random.Next(0, 9);
                    roll2 = random.Next(0, 9);
                    roll3 = random.Next(0, 9);
                    game.RoundTime = _config.Secs3;
                    game.MinStraightBet = _config.Min3Str;
                    game.MinStraightWin = game.MinStraightBet * PICK3_MULTI_STRAIGHT;
                    game.MaxStraightBet = _config.Max3Str;
                    game.MaxStraightWin = game.MaxStraightBet * PICK3_MULTI_STRAIGHT;
                    game.MinAnyBet = _config.Min3Any;
                    game.MinAnyWin = game.MinAnyBet * PICK3_MULTI_ANY;
                    game.MaxAnyBet = _config.Max3Any;
                    game.MaxAnyWin = game.MaxAnyBet * PICK3_MULTI_ANY;
                    break;
            }
            string salt = $"{roll1}{roll2}{roll3}-{pSalt1}.{pSalt2}";

            var r = new Round
            {
                DealerId = dealer.Id,
                Created = DateTime.Now,
                RoundStatus = RoundStatus.Open,
                Roll1 = roll1,
                Roll2 = roll2,
                Roll3 = roll3,
                RollHash = Utils.SHA256Hash(salt),
                RollSalt = salt,
                RoundType = roundType
            };
            _db.Rounds.Add(r);
            await _db.SaveChangesAsync().ConfigureAwait(false);

            string roundTypeStr = Utils.GetRoundTypeName(roundType);
            var client = Context.Client as DiscordSocketClient;
            await client.SetGameAsync($"{roundTypeStr} LOTTO").ConfigureAwait(false);
            await RoundStart(r, game).ConfigureAwait(false);
            await UnmuteChannel().ConfigureAwait(false);
        }

        private async Task RoundStart(Round round, Game game)
        {
            var betText = string.Empty;
            var playText = string.Empty;
            if (game.RoundType == RoundType.Pick1)
            {
                betText = $"```ml\nMin Bet: {game.MinSingleBet.AddCommas()} -> wins {game.MinSingleWin.AddCommas()}\n" +
                    $"Max Bet: {game.MaxSingleBet.AddCommas()} -> wins {game.MaxSingleWin.AddCommas()}```";
                playText = $"Random, single number bet:```md\n.t <amount> @{game.Dealer} q```Bet number **0**:```md\n.t <amount> @{game.Dealer} 0```";
            }
            else
            {
                betText = $"```ml\nStraight (s) Min Bet: {game.MinStraightBet.AddCommas()} -> wins {game.MinStraightWin.AddCommas()}\n" +
                  $"Straight (s) Max Bet: {game.MaxStraightBet.AddCommas()} -> wins {game.MaxStraightWin.AddCommas()}```" +
                  $"```ml\nAny (a) Min Bet: {game.MinAnyBet.AddCommas()} -> wins {game.MinAnyWin.AddCommas()}\n" +
                  $"Any (a) Max Bet: {game.MaxAnyBet.AddCommas()} -> wins {game.MaxAnyWin.AddCommas()}```";
                if (game.RoundType == RoundType.Pick2)
                {
                    playText = $"Random, straight number bet:```md\n.t <amount> @{game.Dealer} q s```Bet number **69**, either way (69 or 96):```md\n.t <amount> @{game.Dealer} 69 a```";
                }
                else
                {
                    playText = $"Random, straight number bet:```md\n.t <amount> @{game.Dealer} q s```Bet number **420**, any way (420, 024, etc.):```md\n.t <amount> @{game.Dealer} 420 a```";
                }
            }

            var builder = new EmbedBuilder()
                .WithColor(Color.LOTTO_BOT)
                .WithFooter(footer =>
                {
                    footer.WithText($"Roll Hash (SHA-256): {round.RollHash}")
                        .WithIconUrl(Asset.SHIELD);
                })
                .WithAuthor(author =>
                {
                    author.WithName($"Pick Number Lotto | Round #{round.RoundId}")
                        .WithIconUrl(Context.Client.CurrentUser.GetAvatarUrl());
                })
                .AddField($"— Game Type: {game.RoundTypeLabel}",
                    $"Runs for **{Utils.ConvertToCompoundDuration(game.RoundTime)}** after the *first bet* is placed" + betText)
                .AddField("— Two Ways To Play", playText)
                .AddField("— Important Terms",
                    "Any bets not in range will be returned.\n" +
                    "Any bets with weird numbers/letters/combos the bot cannot parse will be returned.\n" +
                    "**Refund not guaranteed.**");
            var embed = builder.Build();

            var msg = await Context.Channel.SendMessageAsync(string.Empty, embed: embed).ConfigureAwait(false);

            try
            {
                var pins = await msg.Channel.GetPinnedMessagesAsync().ConfigureAwait(false);
                var botPins = pins.Where(x => x.Author.Id == Context.Client.CurrentUser.Id);
                foreach (IUserMessage pin in botPins)
                {
                    await pin.UnpinAsync().ConfigureAwait(false);
                }
                await msg.PinAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync($"Pin Error: {ex.Message}");
            }
        }

        [Command("status"), Summary("Round status.")]
        public async Task Status()
        {
            var item = await _db.Rounds
                .OrderByDescending(x => x.Created)
                .Include(x => x.Bets)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);
            if (item == null)
            {
                await ReplyAsync($"No rounds.").ConfigureAwait(false);
            }
            else
            {
                string roundTypeStr = Utils.GetRoundTypeName(item.RoundType);
                var user = await Context.Channel.GetUserAsync(item.DealerId).ConfigureAwait(false);
                string footerText = string.Empty;
                string subText = string.Empty;
                switch (item.RoundStatus)
                {
                    case RoundStatus.Open:
                        if (item.Ends.HasValue)
                        {
                            int secsLeft = (int)(item.Ends.Value - DateTime.Now).TotalSeconds;
                            footerText = $"Round ends in {Utils.ConvertToCompoundDuration(secsLeft)}";
                        }
                        else
                        {
                            footerText = "Round has no end time";
                        }
                        subText = $"Dealer: **@{user}**\nTotal Bets: **{item.Bets.Count}**";
                        break;
                    case RoundStatus.Rolling:
                        footerText = string.Empty;
                        subText = $"Dealer: **@{user}**\nTotal Bets: **{item.Bets.Count}**";
                        break;
                    case RoundStatus.Complete:
                        footerText = $"Round completed on {item.Completed.ToDate()}";
                        subText = $"Total Bets: {item.Bets.Count}\nTotal Winners: {item.TotalWinners}";
                        break;
                }

                var builder = new EmbedBuilder()
                    .WithColor(Color.INFO)
                    .WithFooter(footer =>
                    {
                        footer.WithText(footerText)
                            .WithIconUrl(Asset.CLOCK);
                    })
                    .WithAuthor(author =>
                    {
                        author.WithName($"Pick Number Lotto | Round #{item.RoundId} ({roundTypeStr})")
                            .WithIconUrl(Asset.INFO);
                    })
                    .AddField(item.RoundStatus.ToString(), subText);
                var embed = builder.Build();

                await Context.Channel.SendMessageAsync(string.Empty, embed: embed).ConfigureAwait(false);
            }
        }

        [Command("poll"), Summary("Run poll.")]
        public async Task Poll()
        {
            var item = await _db.Rounds
                .OrderByDescending(x => x.Created)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);
            if (item == null || item.RoundStatus == RoundStatus.Complete)
            {
                var builder = new EmbedBuilder()
                    .WithColor(Color.INFO)
                    .WithAuthor(author =>
                    {
                        author.WithName("Pick Number Lotto Poll")
                            .WithIconUrl(Asset.INFO);
                    })
                    .AddField("What game should we run next?",
                        ":one: **PICK 1** *(1 minute round, 9x your bet)*\n" +
                        ":two: **PICK 2** *(10 minute round, 50x your bet)*\n" +
                        ":three: **PICK 3** *(60 minute round, 500x your bet)*");
                var embed = builder.Build();

                var msg = await Context.Channel.SendMessageAsync(string.Empty, embed: embed).ConfigureAwait(false);

                await msg.AddReactionAsync(new Emoji("\U00000031\U000020e3")).ConfigureAwait(false);
                await msg.AddReactionAsync(new Emoji("\U00000032\U000020e3")).ConfigureAwait(false);
                await msg.AddReactionAsync(new Emoji("\U00000033\U000020e3")).ConfigureAwait(false);
            }
            else
            {
                await ReplyAsync($"Round is still open, no need for poll <:bebe:463390590382505994>").ConfigureAwait(false);
            }

        }

        [Command("close"), Summary("Closes round.")]
        public async Task Close()
        {
            var item = await _db.Rounds
                .Where(x => x.RoundStatus == RoundStatus.Open)
                .Include(x => x.Bets)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);
            item.Bets.RemoveAll(x => x.Confirmed != true);
            if (item != null)
            {
                item.TotalBets = item.Bets.Count;
                item.TotalAmount = item.Bets.Sum(x => x.Amount);
                string roundTypeStr = Utils.GetRoundTypeName(item.RoundType);
                if (item.TotalBets > 0)
                {
                    item.RoundStatus = RoundStatus.Rolling;
                    string rollEmoji = GenRollEmoji(item.Roll1, item.Roll2, item.Roll3);
                    string s = (item.TotalBets != 1) ? "s" : string.Empty;

                    await MuteChannel().ConfigureAwait(false);

                    // send roll update to main channel
                    var builder2 = new EmbedBuilder()
                        .WithColor(Color.LOTTO_BOT)
                        .WithFooter(footer =>
                        {
                            footer.WithText($"Roll salt: {item.RollSalt}")
                                .WithIconUrl(Asset.SHIELD);
                        })
                        .WithAuthor(author =>
                        {
                            author.WithName($"Pick Number Lotto | Round #{item.RoundId} ({roundTypeStr}) Closed")
                                .WithIconUrl(Context.Client.CurrentUser.GetAvatarUrl());
                        })
                        .AddField($"Roll: {rollEmoji}",
                            $"{item.TotalBets.Value.AddCommas()} total bet{s}");
                    var embed2 = builder2.Build();

                    await Context.Channel.SendMessageAsync(string.Empty, embed: embed2).ConfigureAwait(false);

                    await CompleteRound(item).ConfigureAwait(false);
                }
                else
                {
                    item.RoundStatus = RoundStatus.Complete;
                    item.Completed = DateTime.Now;
                    item.TotalWinners = 0;
                    await MuteChannel().ConfigureAwait(false);
                    await ReplyAsync($"Round #{item.RoundId} closed, no bets placed. <:meh:463390590390763540>").ConfigureAwait(false);
                }
                _db.Rounds.Update(item);
                await _db.SaveChangesAsync().ConfigureAwait(false);

                // send update to fair channel
                var fairChan = await Context.Guild.GetTextChannelAsync(_config.FairChannelId).ConfigureAwait(false);
                var builder = new EmbedBuilder()
                    .WithColor(Color.SHIELD)
                    .WithFooter(footer =>
                    {
                        footer.WithText($"Round completed on {item.Completed.ToDate()}")
                            .WithIconUrl(Asset.CLOCK);
                    })
                    .WithAuthor(author =>
                    {
                        author.WithName($"Pick Number Lotto | Round #{item.RoundId} ({roundTypeStr})")
                            .WithIconUrl(Asset.SHIELD);
                    })
                    .AddField($"Provably Fair Information",
                        $"Roll: {item.Roll1}{item.Roll2}{item.Roll3}\n" +
                        $"Salt: {item.RollSalt}\n" +
                        $"Verify: ``SHA256({item.RollSalt}) = {item.RollHash}``");
                var embed = builder.Build();

                await fairChan.SendMessageAsync(string.Empty, embed: embed).ConfigureAwait(false);
            }
            else
            {
                await ReplyAsync($"No round open.").ConfigureAwait(false);
            }

            var client = Context.Client as DiscordSocketClient;
            await client.SetGameAsync("CLOSED").ConfigureAwait(false);
        }

        private string GenRollEmoji(int? roll1, int? roll2, int? roll3)
        {
            var nums = new[] { ":zero:", ":one:", ":two:", ":three:", ":four:", ":five:", ":six:", ":seven:", ":eight:", ":nine:" };
            string s = string.Empty;
            if (roll1.HasValue)
            {
                s = nums[roll1.Value];
            }
            if (roll2.HasValue)
            {
                s = string.Concat(s, " ", nums[roll2.Value]);
            }
            if (roll3.HasValue)
            {
                s = string.Concat(s, " ", nums[roll3.Value]);
            }
            return s;
        }

        private async Task CompleteRound(Round round)
        {
            round.RoundStatus = RoundStatus.Complete;
            round.Completed = DateTime.Now;
            _db.Update(round);
            await _db.SaveChangesAsync().ConfigureAwait(false);

            var chan = Context.Channel;
            List<Bet> strWins = new List<Bet>();
            List<Bet> anyWins = new List<Bet>();
            List<Bet> sngWins = new List<Bet>();

            switch (round.RoundType)
            {
                case RoundType.Pick1:
                    sngWins = round.Bets
                        .Where(x => x.RoundId == round.RoundId &&
                            x.Confirmed == true &&
                            x.PlayType == PlayType.Single &&
                            x.Pick1 == round.Roll1)
                        .ToList();
                    break;
                case RoundType.Pick2:
                    strWins = round.Bets
                        .Where(x => x.RoundId == round.RoundId &&
                            x.Confirmed == true &&
                            x.PlayType == PlayType.Straight &&
                            x.Pick1 == round.Roll1 &&
                            x.Pick2 == round.Roll2)
                        .ToList();
                    anyWins = round.Bets
                        .Where(x => x.RoundId == round.RoundId &&
                            x.Confirmed == true &&
                            x.PlayType == PlayType.Any &&
                            (x.Pick1 == round.Roll1 && x.Pick2 == round.Roll2) ||
                            (x.Pick1 == round.Roll2 && x.Pick2 == round.Roll1))
                        .ToList();
                    break;
                case RoundType.Pick3:
                    strWins = round.Bets
                        .Where(x => x.RoundId == round.RoundId &&
                            x.Confirmed == true &&
                            x.PlayType == PlayType.Straight &&
                            x.Pick1 == round.Roll1 &&
                            x.Pick2 == round.Roll2 &&
                            x.Pick3 == round.Roll3)
                        .ToList();
                    anyWins = round.Bets
                        .Where(x => x.RoundId == round.RoundId &&
                            x.Confirmed == true &&
                            x.PlayType == PlayType.Any &&
                            (x.Pick1 == round.Roll1 && x.Pick2 == round.Roll2 && x.Pick3 == round.Roll3) ||
                            (x.Pick1 == round.Roll1 && x.Pick2 == round.Roll3 && x.Pick3 == round.Roll2) ||
                            (x.Pick1 == round.Roll2 && x.Pick2 == round.Roll1 && x.Pick3 == round.Roll3) ||
                            (x.Pick1 == round.Roll2 && x.Pick2 == round.Roll3 && x.Pick3 == round.Roll1) ||
                            (x.Pick1 == round.Roll3 && x.Pick2 == round.Roll1 && x.Pick3 == round.Roll2) ||
                            (x.Pick1 == round.Roll3 && x.Pick2 == round.Roll2 && x.Pick3 == round.Roll1)
                            )
                        .ToList();
                    break;
            }

            round.TotalStraightWinners = strWins.Count;
            round.TotalAnyWinners = anyWins.Count;
            round.TotalSingleWinners = sngWins.Count;
            round.TotalWinners = round.TotalStraightWinners + round.TotalAnyWinners + round.TotalSingleWinners;
            if (round.TotalWinners > 0)
            {
                string s = (round.TotalWinners > 1) ? "s" : string.Empty;
                await chan.SendMessageAsync($"{round.TotalWinners} total winner{s}! <:thonkerguns:463390590240030721>").ConfigureAwait(false);
                foreach (var w in strWins)
                {
                    var user = await chan.GetUserAsync(w.UserId).ConfigureAwait(false);
                    int multi = (round.RoundType == RoundType.Pick3) ? PICK3_MULTI_STRAIGHT : PICK2_MULTI_STRAIGHT;
                    await chan.SendMessageAsync($"{user.Mention} WON {(w.Amount * multi).AddCommas()}!! Play: Straight '{w.Pick1}{w.Pick2}{w.Pick3}'").ConfigureAwait(false);
                    w.Winner = true;
                    _db.Update(w);
                }
                foreach (var w in anyWins)
                {
                    var user = await chan.GetUserAsync(w.UserId).ConfigureAwait(false);
                    int multi = (round.RoundType == RoundType.Pick3) ? PICK3_MULTI_ANY : PICK2_MULTI_ANY;
                    await chan.SendMessageAsync($"{user.Mention} WON {(w.Amount * multi).AddCommas()}!! Play: Any '{w.Pick1}{w.Pick2}{w.Pick3}'").ConfigureAwait(false);
                    w.Winner = true;
                    _db.Update(w);
                }
                foreach (var w in sngWins)
                {
                    var user = await chan.GetUserAsync(w.UserId).ConfigureAwait(false);
                    await chan.SendMessageAsync($"{user.Mention} WON {(w.Amount * PICK1_MULTI_SINGLE).AddCommas()}!! Play: Single '{w.Pick1}'").ConfigureAwait(false);
                    w.Winner = true;
                    _db.Update(w);
                }
                await _db.SaveChangesAsync().ConfigureAwait(false);

                var dealer = await chan.GetUserAsync(round.DealerId).ConfigureAwait(false);
                await chan.SendMessageAsync($"{dealer.Mention} will pay out these winners shortly.").ConfigureAwait(false);
            }
            else
            {
                await chan.SendMessageAsync($"No winners this round. <:bebe:463390590382505994>").ConfigureAwait(false);
            }

            return;
        }

        [Command("help"), Summary("Provides lotto help.")]
        public async Task Help([Remainder] string msg)
        {
            var roundType = ParseRoundType(msg);
            switch (roundType)
            {
                case RoundType.Pick1:
                    //await ReplyAsync(Utils.BanHelpPick1(_config.BanMinBet1, _config.BanMaxBet1, Context.User.Username, _config.BanSeconds1)).ConfigureAwait(false);
                    break;
                case RoundType.Pick2:
                    //await ReplyAsync(Utils.BanHelpPick2(_config.BanMinBet2, _config.BanMaxBet2, Context.User.Username, _config.BanSeconds2)).ConfigureAwait(false);
                    break;
                case RoundType.Pick3:
                    //await ReplyAsync(Utils.BanHelpPick3(_config.BanMinBet3, _config.BanMaxBet3, Context.User.Username, _config.BanSeconds3)).ConfigureAwait(false);
                    break;
            }
            return;
        }

        private RoundType ParseRoundType(string msg)
        {
            if (int.TryParse(msg[0].ToString(), out int pick))
            {
                switch (pick)
                {
                    case 2:
                        return RoundType.Pick2;
                    case 3:
                        return RoundType.Pick3;
                }
            }
            return RoundType.Pick1;
        }

        private async Task UnmuteChannel()
        {
            var chan = (IGuildChannel)Context.Channel;
            var perms = new OverwritePermissions(sendMessages: PermValue.Inherit);
            var role = Context.Guild.GetRole(_config.PlayerRoleId);
            await chan.AddPermissionOverwriteAsync(role, perms).ConfigureAwait(false);
        }

        private async Task MuteChannel()
        {
            var chan = (IGuildChannel)Context.Channel;
            var perms = new OverwritePermissions(sendMessages: PermValue.Deny);
            var role = Context.Guild.GetRole(_config.PlayerRoleId);
            await chan.AddPermissionOverwriteAsync(role, perms).ConfigureAwait(false);
        }
    }
}

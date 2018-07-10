using System;
using System.Collections.Generic;
using System.Text;

namespace dm.Banotto.Models
{
    public enum PlayType
    {
        Straight,
        Any,
        Single
    }

    public class Bet
    {
        public int BetId { get; set; }
        public DateTime Created { get; set; }
        public ulong UserId { get; set; }
        public ulong UserBetMessageId { get; set; }
        public BetType BetType { get; set; }
        public int Amount { get; set; }
        public bool IsQuick { get; set; }
        public int? Pick1 { get; set; }
        public int? Pick2 { get; set; }
        public int? Pick3 { get; set; }
        public PlayType PlayType { get; set; }
        public bool? Confirmed { get; set; }
        public bool? Winner { get; set; }

        public Round Round { get; set; }
        public int RoundId { get; set; }
    }
}

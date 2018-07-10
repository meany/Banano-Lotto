using System;
using System.Collections.Generic;

namespace dm.Banotto.Models
{
    public enum RoundStatus
    {
        Open,
        Rolling,
        Complete
    }

    public enum RoundType
    {
        Pick1,
        Pick2,
        Pick3
    }

    public class Round
    {
        public int RoundId { get; set; }
        public ulong DealerId { get; set; }
        public DateTime Created { get; set; }
        public RoundStatus RoundStatus { get; set; }
        public RoundType RoundType { get; set; }
        public DateTime? Ends { get; set; }
        public DateTime? Completed { get; set; }
        public int? Roll1 { get; set; }
        public int? Roll2 { get; set; }
        public int? Roll3 { get; set; }
        public string RollSalt { get; set; }
        public string RollHash { get; set; }
        public int? TotalBets { get; set; }
        public int? TotalAmount { get; set; }
        public int? TotalStraightWinners { get; set; }
        public int? TotalAnyWinners { get; set; }
        public int? TotalSingleWinners { get; set; }
        public int? TotalWinners { get; set; }

        public List<Bet> Bets { get; set; }
    }
}
using dm.Banotto.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace dm.Banotto
{
    public class Game
    {
        public RoundType RoundType { get; set; }
        public string RoundTypeLabel {
            get {
                return Utils.GetRoundTypeName(RoundType);
            }
        }
        public int RoundTime { get; set; }

        public int MinSingleBet { get; set; }
        public int MinSingleWin { get; set; }
        public int MinStraightBet { get; set; }
        public int MinStraightWin { get; set; }
        public int MinAnyBet { get; set; }
        public int MinAnyWin { get; set; }

        public int MaxSingleBet { get; set; }
        public int MaxSingleWin { get; set; }
        public int MaxStraightBet { get; set; }
        public int MaxStraightWin { get; set; }
        public int MaxAnyBet { get; set; }
        public int MaxAnyWin { get; set; }

        public string Dealer { get; set; }
    }
}

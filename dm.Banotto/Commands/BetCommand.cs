using dm.Banotto.Models;

namespace dm.Banotto
{
    public enum BetType
    {
        Banano,
        Nano
    }
    class BetCommand
    {
        public BetType? BetType { get; set; }
        public int? Amount { get; set; }
        public string Command { get; set; }
        public bool Quick { get; set; }
        public int? Pick1 { get; set; }
        public int? Pick2 { get; set; }
        public int? Pick3 { get; set; }
        public string Play { get; set; }
        public PlayType? PlayType { get; set; }
    }
}
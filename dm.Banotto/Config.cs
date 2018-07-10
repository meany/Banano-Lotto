namespace dm.Banotto
{
    public class Config
    {
        public string BotName { get; set; }
        public string Token { get; set; }

        public ulong DealerRoleId { get; set; }
        public ulong PlayerRoleId { get; set; }
        public ulong GameChannelId { get; set; }
        public ulong FairChannelId { get; set; }

        public ulong TipBotId { get; set; }
        public int Min1 { get; set; }
        public int Max1 { get; set; }
        public int Secs1 { get; set; }
        public int Min2Str { get; set; }
        public int Min2Any { get; set; }
        public int Max2Str { get; set; }
        public int Max2Any { get; set; }
        public int Secs2 { get; set; }
        public int Min3Str { get; set; }
        public int Min3Any { get; set; }
        public int Max3Str { get; set; }
        public int Max3Any { get; set; }
        public int Secs3 { get; set; }
        public string EmoteGood { get; set; }
        public string EmoteBad { get; set; }
    }
}
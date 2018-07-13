using dm.Banotto.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace dm.Banotto
{
    public static class Utils
    {
        public static string GetRoundTypeName(RoundType RoundType)
        {
            return RoundType.ToString().ToUpper().Replace("PICK", "PICK ");
        }

        public static string ConvertToCompoundDuration(int Seconds)
        {
            bool neg = false;
            if (Seconds < 0)
            {
                Seconds = Math.Abs(Seconds);
                neg = true;
            }
            if (Seconds == 0) return "0 seconds";

            TimeSpan span = TimeSpan.FromSeconds(Seconds);
            int[] parts = { span.Days / 7, span.Days % 7, span.Hours, span.Minutes, span.Seconds };
            string[] units = { " week", " day", " hour", " minute", " second" };

            string r = string.Join(", ",
                from index in Enumerable.Range(0, units.Length)
                where parts[index] > 0
                select parts[index] + units[index] + ((parts[index] > 1) ? "s" : string.Empty));
            return (neg) ? $"-{r}" : r;
        }

        public static string SHA256Hash(string Data)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(Data));

                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        public static long LongRandom(Random rand)
        {
            long r = 0;
            while (r <= 1125899906842624)
            {
                r = (long)(rand.NextDouble() * Int64.MaxValue);
            }
            return r;
        }

        //public static string BanHelpPick1(int Min, int Max, string Admin, int RoundTime)
        //{
        //    return "```md\n" +
        //        $"# PICK 1 LOTTO\n" +
        //        $"# WIN UP TO {string.Format("{0:n0}", Max * PICK1_MULTI_SINGLE)} BANANOS!\n" +
        //        "\n" +
        //        "Pick a *single* digit number from 0 to 9, or choose 'Quick Pick' (q) to have the monkey machine randomly pick numbers for you.\n" +
        //        $"If your number is drawn, you'll get {PICK1_MULTI_SINGLE}x your bet!\n" +
        //        "\n" +
        //        "Example plays:\n" +
        //        $"* .t {Min} @{Admin} 4\n" +
        //        $"> Single play '4' for {string.Format("{0:n0}", Min)} BAN. It could win {string.Format("{0:n0}", Min * PICK1_MULTI_SINGLE)} BAN.\n" +
        //        $"* .t {Max} @{Admin} q\n" +
        //        $"> Single play '8' (Quick) for {string.Format("{0:n0}", Max)} BAN. It could win {string.Format("{0:n0}", Max * PICK1_MULTI_SINGLE)} BAN.\n" +
        //        "\n" +
        //        $"The round stays open for {ConvertToCompoundDuration(RoundTime)} after the first bet. Bet once or as many times as you'd like!\n" +
        //        "Then we'll close the round and *.roll 9* once. Good luck!\n" +
        //        "\n" +
        //        $"<MIN BET=\"{Min}\">\n" +
        //        $"<MAX BET=\"{Max}\">\n" +
        //        TERMS;
        //}

        //public static string BanHelpPick2(int Min, int Max, string Admin, int RoundTime)
        //{
        //    var mid = Math.Round((double)(Max / 2));
        //    return "```md\n" +
        //        $"# PICK 2 LOTTO\n" +
        //        $"# WIN UP TO {string.Format("{0:n0}", Max * PICK2_MULTI_STRAIGHT)} BANANOS!\n" +
        //        "\n" +
        //        "Pick a *two* digit number from 00 to 99, or choose 'Quick Pick' (q) to have the monkey machine randomly pick numbers for you.\n" +
        //        "Then pick a play type. Either 'Straight' (s) or 'Any' (a).\n" +
        //        $"* Straight play requires the numbers to be in *exact order* and pays {PICK2_MULTI_STRAIGHT}x your bet.\n" +
        //        $"* Any play means the numbers can be in either order and pays {PICK2_MULTI_ANY}x your bet.\n" +
        //        "\n" +
        //        "Example plays:\n" +
        //        $"* .t {Max} @{Admin} 42 s\n" +
        //        $"> Straight play '42' for {string.Format("{0:n0}", Max)} BAN. It could win {string.Format("{0:n0}", Max * PICK2_MULTI_STRAIGHT)} BAN.\n" +
        //        "\n" +
        //        $"* .t {mid} @{Admin} q a\n" +
        //        $"> Any play '18' (Quick Pick) for {string.Format("{0:n0}", mid)} BAN. It could win {string.Format("{0:n0}", mid * PICK2_MULTI_ANY)} BAN.\n" +
        //         "\n" +
        //        $"The round stays open for {ConvertToCompoundDuration(RoundTime)} after the first bet. Bet once or as many times as you'd like!\n" +
        //        "Then we'll close the round and '.roll 9' twice. Good luck!\n" +
        //        "\n" +
        //        $"<MIN BET=\"{Min}\">\n" +
        //        $"<MAX BET=\"{Max}\">\n" +
        //        TERMS;
        //}

        //public static string BanHelpPick3(int Min, int Max, string Admin, int RoundTime)
        //{
        //    var mid = Math.Round((double)(Max / 2));
        //    return "```md\n" +
        //        $"# PICK 3 LOTTO\n" +
        //        $"# WIN UP TO {string.Format("{0:n0}", Max * PICK3_MULTI_STRAIGHT)} BANANOS!\n" +
        //        "\n" +
        //        "Pick a *three* digit number from 000 to 999, or choose 'Quick Pick' (q) to have the monkey machine randomly pick numbers for you.\n" +
        //        "Then pick a play type. Either 'Straight' (s) or 'Any' (a).\n" +
        //        $"Straight play requires the numbers to be in *exact order* and pays {PICK3_MULTI_STRAIGHT}x your bet.\n" +
        //        $"Any play means the numbers can be in any order and pays {PICK3_MULTI_ANY}x your bet.\n" +
        //        "\n" +
        //        "Example plays:\n" +
        //        $"* .t {Min} @{Admin} 420 s\n" +
        //        $"> Straight play '420' for {string.Format("{0:n0}", Min)} BAN. It could win {string.Format("{0:n0}", Min * PICK3_MULTI_STRAIGHT)} BAN.\n" +
        //        "\n" +
        //        $"* .t {mid} @{Admin} q a\n" +
        //        $"> Any play '187' (Quick Pick) for {string.Format("{0:n0}", mid)} BAN. It could win {string.Format("{0:n0}", mid * PICK3_MULTI_ANY)} BAN.\n" +
        //        "\n" +
        //        $"The round stays open for {ConvertToCompoundDuration(RoundTime)} after the first bet. Bet once or as many times as you'd like!\n" +
        //        "Then we'll close the round and '.roll 9' three times. Good luck!\n" +
        //        "\n" +
        //        $"<MIN BET=\"{Min}\">\n" +
        //        $"<MAX BET=\"{Max}\">\n" +
        //        TERMS;
        //}
    }
}

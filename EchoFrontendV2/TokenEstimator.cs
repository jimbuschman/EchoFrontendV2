using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestSQLLite
{
    public static class TokenEstimator
    {
        public static int EstimateTokens(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            return (int)(text.Length / 4.0); // rough token average
        }

        public static int EstimateTokens(List<SessionMessage> messages)
        {
            int total = 0;
            foreach (var m in messages)
            {
                total += EstimateTokens(m.Role + ": " + m.Content);
            }
            return total;
        }
    }
}

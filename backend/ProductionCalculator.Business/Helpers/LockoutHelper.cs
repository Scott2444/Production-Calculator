using Microsoft.Extensions.Configuration;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Helpers
{
    public static class LockoutHelper
    {
        public static void UpdateUserLockout(IConfiguration config, ref User user)
        {
            // Clear any expired lockout
            if (user.Lockout_Until != null && user.Lockout_Until <= DateTime.UtcNow)
            {
                user.Lockout_Until = null;
            }

            // Increment failed attempts
            user.Failed_Login_Attempts++;

            // Get escalation config from appsettings
            var escalationSection = config.GetSection("LoginRateLimit:LockoutEscalation").GetChildren();
            var escalationDict = new SortedDictionary<int, int>();
            int highestThreshold = 0;
            int highestMinutes = 0;
            foreach (var kv in escalationSection)
            {
                if (int.TryParse(kv.Key, out int threshold) && int.TryParse(kv.Value, out int minutes))
                {
                    escalationDict[threshold] = minutes;
                    if (threshold > highestThreshold)
                    {
                        highestThreshold = threshold;
                        highestMinutes = minutes;
                    }
                }
            }

            int attempts = user.Failed_Login_Attempts;
            int lockoutMinutes = 0;
            if (escalationDict.ContainsKey(attempts))
            {
                lockoutMinutes = escalationDict[attempts];
            }
            else if (attempts > highestThreshold && attempts % 5 == 0)
            {
                lockoutMinutes = highestMinutes;
            }

            if (lockoutMinutes > 0)
            {
                user.Lockout_Until = DateTime.UtcNow.AddMinutes(lockoutMinutes);
            }
        }

    }
}
using MultiDriveSync.Client.Properties;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiDriveSync.Client.Helpers
{
    class AppSettings
    {
        private const string CLIENT_ID = "clientId";
        private const string CLIENT_SECRET = "clientSecret";

        public static string AppName { get; } = "MultiDriveSync";
        public static string ClientId { get; } = ConfigurationManager.AppSettings[CLIENT_ID];
        public static string ClientSecret { get; } = ConfigurationManager.AppSettings[CLIENT_SECRET];

        public static void AddUser(string email, string userId)
        {
            if (Settings.Default.Users is null)
            {
                Settings.Default.Users = new StringCollection();
            }

            Settings.Default.Users.Add($"{email};{userId}");
            Settings.Default.Save();
        }

        public static IEnumerable<string> GetUserEmails()
        {
            if (Settings.Default.Users is null)
            {
                yield break;
            }

            foreach (var user in Settings.Default.Users)
            {
                if (TryDeserializeUser(user, out var userInfo))
                {
                    yield return userInfo.Email;
                }
            }
        }

        public static bool TryGetUserId(string userEmail, out string userId)
        {
            if (Settings.Default.Users is null)
            {
                userId = string.Empty;
                return false;
            }

            foreach (var user in Settings.Default.Users)
            {
                if (TryDeserializeUser(user, out var userInfo))
                {
                    if (userInfo.Email == userEmail)
                    {
                        userId = userInfo.UserId;
                        return true;
                    }
                }
            }

            userId = string.Empty;
            return false;
        }

        private static bool TryDeserializeUser(string user, out UserInfo userInfo)
        {
            if (!string.IsNullOrEmpty(user))
            {
                var userInfoParts = user.Split(';');

                if (userInfoParts.Length == 2)
                {
                    userInfo = new UserInfo { Email = userInfoParts[0], UserId = userInfoParts[1] };
                    return true;
                }
            }

            userInfo = null;
            return false;
        }

        private class UserInfo
        {
            public string Email { get; set; }
            public string UserId { get; set; }
        }
    }
}

using MultiDriveSync.Client.Properties;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using MultiDriveSync.Models;

namespace MultiDriveSync.Client.Helpers
{
    public class AppSettings
    {
        private const string CLIENT_ID = "clientId";
        private const string CLIENT_SECRET = "clientSecret";
        
        public static string AppName { get; } = "MultiDriveSync";
        public static string DefaultClientId { get; } = ConfigurationManager.AppSettings[CLIENT_ID];
        public static string DefaultClientSecret { get; } = ConfigurationManager.AppSettings[CLIENT_SECRET];

        public static void AddSession(Session session)
        {
            if (Settings.Default.Sessions is null)
                Settings.Default.Sessions = new StringCollection();

            Settings.Default.Sessions.Add($"{session.UserInfo.Email};{session.UserInfo.UserId};{session.UserInfo.Name};{session.ClientInfo.ClientId};{session.ClientInfo.ClientSecret}");
            Settings.Default.Save();
        }

        public static IEnumerable<Session> GetSessions()
        {
            if (Settings.Default.Sessions is null)
            {
                yield break;
            }

            foreach (var user in Settings.Default.Sessions)
            {
                if (TryDeserializeSession(user, out var userInfo))
                {
                    yield return userInfo;
                }
            }
        }

        public static IEnumerable<string> GetUserEmails()
        {
            return GetSessions().Select(x => x.UserInfo.Email);
        }

        public static bool TryGetUserId(string userEmail, out string userId)
        {
            if (Settings.Default.Sessions is null)
            {
                userId = string.Empty;
                return false;
            }

            foreach (var session in Settings.Default.Sessions)
            {
                if (TryDeserializeSession(session, out var sessionInfo))
                {
                    if (sessionInfo.UserInfo.Email == userEmail)
                    {
                        userId = sessionInfo.UserInfo.UserId;
                        return true;
                    }
                }
            }

            userId = string.Empty;
            return false;
        }

        private static bool TryDeserializeSession(string sessionString, out Session session)
        {
            if (!string.IsNullOrEmpty(sessionString))
            {
                var sessionInfoParts = sessionString.Split(';');

                if (sessionInfoParts.Length == 5)
                {

                    session = new Session
                    {
                        UserInfo = new UserInfo
                        {
                            Email = sessionInfoParts[0],
                            UserId = sessionInfoParts[1],
                            Name = sessionInfoParts[2]
                        },
                        ClientInfo = new ClientInfo
                        {
                            ClientId = sessionInfoParts[3],
                            ClientSecret = sessionInfoParts[4]
                        }
                    };
                    return true;
                }
            }

            session = null;
            return false;
        }
    }
}

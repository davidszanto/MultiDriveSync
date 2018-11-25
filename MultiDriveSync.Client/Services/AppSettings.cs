using MultiDriveSync.Client.Properties;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using MultiDriveSync.Models;
using Newtonsoft.Json;

namespace MultiDriveSync.Client.Services
{
    public class AppSettings
    {
        private const string CLIENT_ID = "clientId";
        private const string CLIENT_SECRET = "clientSecret";
        private const string APP_NAME = "MultiDriveSync";

        public ClientInfo ClientInfo { get; }

        public AppSettings()
        {
            ClientInfo = new ClientInfo
            {
                AppName = APP_NAME,
                ClientName = APP_NAME,
                ClientId = ConfigurationManager.AppSettings[CLIENT_ID],
                ClientSecret = ConfigurationManager.AppSettings[CLIENT_SECRET]
            };
        }

        public void AddSession(Session session)
        {
            if (Settings.Default.Sessions is null)
                Settings.Default.Sessions = new StringCollection();

            var json = JsonConvert.SerializeObject(session);
            Settings.Default.Sessions.Add(json);
            Settings.Default.Save();
        }

        public IEnumerable<Session> GetSessions()
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

        public void DeleteSession(Session session)
        {
            var sessionString = JsonConvert.SerializeObject(session);
            Settings.Default.Sessions.Remove(sessionString);
            Settings.Default.Save();
        }
        
        public IEnumerable<string> GetUserEmails()
        {
            return GetSessions().Select(x => x.UserInfo.Email);
        }

        public bool TryGetUserId(string userEmail, out string userId)
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

        private bool TryDeserializeSession(string sessionString, out Session session)
        {
            try
            {
                session = JsonConvert.DeserializeObject<Session>(sessionString);
                return true;
            }
            catch (JsonException)
            {
                session = null;
                return false;
            }
        }
    }
}

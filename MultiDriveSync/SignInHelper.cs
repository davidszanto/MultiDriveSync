using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Google.Apis.Oauth2.v2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using MultiDriveSync.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MultiDriveSync
{
    public static class SignInHelper
    {
        public static async Task<UserInfo> SignInAsync(ClientInfo clientInfo)
        {
            var clientSecrets = new ClientSecrets
            {
                ClientId = clientInfo.ClientId,
                ClientSecret = clientInfo.ClientSecret
            };

            var credentials = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                clientSecrets,
                new[] { DriveService.Scope.Drive, Oauth2Service.Scope.UserinfoEmail },
                Guid.NewGuid().ToString(), CancellationToken.None, new FileDataStore(clientInfo.AppName));

            var oauthService = new Oauth2Service(new BaseClientService.Initializer
            {
                HttpClientInitializer = credentials,
                ApplicationName = clientInfo.AppName
            });

            var userInfo = await oauthService.Userinfo.Get().ExecuteAsync();

            return new UserInfo
            {
                Email = userInfo.Email,
                Name = userInfo.Name,
                UserId = credentials.UserId
            };
        }
    }
}

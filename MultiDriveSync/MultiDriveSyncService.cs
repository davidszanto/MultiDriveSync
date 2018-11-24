using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using MultiDriveSync.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MultiDriveSync
{
    public class MultiDriveSyncService : IMultiDriveSync
    {
        private readonly LocalFileSynchronizer localFileSynchronizer;
        private readonly RemoteFileSynchronizer remoteFileSynchronizer;
        private readonly Lazy<UserCredential> credential;

        public MultiDriveSyncSettings Settings { get; }

        public MultiDriveSyncService(MultiDriveSyncSettings settings)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));

            credential = new Lazy<UserCredential>(() => GetStoredCredentialAsync(Settings.UserAccountId, Settings.ClientInfo).Result);

            localFileSynchronizer = new LocalFileSynchronizer();
            remoteFileSynchronizer = new RemoteFileSynchronizer();
        }

        public MultiDriveSyncService(Action<MultiDriveSyncSettings> configure) : this(new MultiDriveSyncSettings())
        {
            configure(Settings);
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            var driveClient = new GoogleDriveClient(credential.Value, Settings.ClientInfo.AppName);
            await localFileSynchronizer.StartSynchronization(Settings, driveClient);
            await remoteFileSynchronizer.RunSynchronization(cancellationToken);
        }

        public async Task<List<string>> ListFilesAsync()
        {
            var driveService = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential.Value,
                ApplicationName = Settings.ClientInfo.AppName
            });

            var files = await driveService.Files.List().ExecuteAsync();
            return files.Files.Select(file => file.Name).ToList();
        }

        private async Task<UserCredential> GetStoredCredentialAsync(string userId, ClientInfo clientInfo)
        {
            var dataStore = new FileDataStore(clientInfo.AppName);

            var clientSecrets = new ClientSecrets
            {
                ClientId = clientInfo.ClientId,
                ClientSecret = clientInfo.ClientSecret
            };

            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = clientSecrets,
                DataStore = dataStore
            });

            var token = await dataStore.GetAsync<TokenResponse>(userId);

            return new UserCredential(flow, userId, token);
        }
    }
}

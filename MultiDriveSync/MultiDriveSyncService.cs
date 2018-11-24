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
        private readonly IGoogleDriveClient googleDriveClient;
        private readonly LocalFileSynchronizer localFileSynchronizer;
        private readonly RemoteFileSynchronizer remoteFileSynchronizer;
        private readonly UserCredential credential;

        public MultiDriveSyncSettings Settings { get; }

        public MultiDriveSyncService(Action<MultiDriveSyncSettings> configure)
        {
            Settings = new MultiDriveSyncSettings();
            configure(Settings);

            credential = GetStoredCredential(Settings.StorageAccountId, Settings.ClientInfo);
            googleDriveClient = new GoogleDriveClient(credential, Settings.ClientInfo.AppName);
            localFileSynchronizer = new LocalFileSynchronizer(googleDriveClient, Settings.LocalRootPath, Settings.UserEmail);
            remoteFileSynchronizer = new RemoteFileSynchronizer(googleDriveClient, Settings.LocalRootPath, Settings.StorageRootPath);
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            await remoteFileSynchronizer.InitializeAsync();

            localFileSynchronizer.InitializeParentIdsAndPaths(remoteFileSynchronizer.ParentPathsById);
            localFileSynchronizer.StartSynchronization();

            await remoteFileSynchronizer.RunSynchronization(cancellationToken);
        }

        private UserCredential GetStoredCredential(string userId, ClientInfo clientInfo)
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

            var token = dataStore.GetAsync<TokenResponse>(userId).Result;

            return new UserCredential(flow, userId, token);
        }

        public Task DeleteStoredDataAsync()
        {
            return googleDriveClient.DeleteStoredTokensAsync();
        }
    }
}

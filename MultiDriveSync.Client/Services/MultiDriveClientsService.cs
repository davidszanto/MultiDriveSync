using MultiDriveSync.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MultiDriveSync.Client.Services
{
    public class MultiDriveClientsService
    {
        private readonly List<RegisteredMultiDriveSync> repository = new List<RegisteredMultiDriveSync>();
        private readonly AppSettings appSettings;

        public MultiDriveClientsService(AppSettings appSettings)
        {
            this.appSettings = appSettings;
        }

        public void Start(Session session)
        {
            var multiDriveSync = new MultiDriveSyncService(settings =>
            {
                settings.StorageAccountId = session.StorageAccountInfo.UserId;
                settings.UserAccountId = session.UserInfo.UserId;
                settings.StorageRootPath = session.RemoteRoot;
                settings.LocalRootPath = session.LocalRoot;
                settings.EditAccessMode = session.EditAccessMode;
                settings.ClientInfo = appSettings.ClientInfo;
            });

            var cts = new CancellationTokenSource();

            repository.Add(new RegisteredMultiDriveSync
            {
                MultiDriveSyncService = multiDriveSync,
                CancellationTokenSource = cts
            });

            Task.Run(() => multiDriveSync.RunAsync(cts.Token));
        }

        public void StopAll()
        {
            foreach (var multiDriveSync in repository)
            {
                multiDriveSync.CancellationTokenSource.Cancel();
            }
        }

        class RegisteredMultiDriveSync
        {
            public MultiDriveSyncService MultiDriveSyncService { get; set; }
            public CancellationTokenSource CancellationTokenSource { get; set; }
        }
    }
}

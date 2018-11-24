using MultiDriveSync.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MultiDriveSync
{
    public class LocalFileSynchronizer
    {
        private readonly FileSystemWatcher watcher;
        private IGoogleDriveClient client;

        public LocalFileSynchronizer()
        {
            watcher = new FileSystemWatcher();
        }

        public async Task StartSynchronization(MultiDriveSyncSettings settings, IGoogleDriveClient driveClient)
        {
            // TODO: inicializalni a watchert

            client = driveClient;

            var folders = await client.GetFoldersFromRoot(settings.StorageRootPath, settings.LocalRootPath);

            watcher.Path = settings.LocalRootPath;
            watcher.EnableRaisingEvents = true;

            watcher.Changed += (sender, args) =>
            {
                //TODO: Localwatcher
            };
        }
    }
}

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
        private GoogleDriveClient client;

        public LocalFileSynchronizer()
        {
            watcher = new FileSystemWatcher();
        }

        public async Task StartSynchronization(MultiDriveSyncSettings settings, GoogleDriveClient driveClient)
        {
            client = driveClient;

            var folders = await client.GetFoldersFromRoot(settings.StorageRootPath, settings.LocalRootPath); //TODO: mi legyen a root mappa neve?

            //TODO: Download data from root
            //client.DownloadFileAsync()

            watcher.Path = settings.LocalRootPath;
            watcher.EnableRaisingEvents = true;

            watcher.Changed += (sender, args) =>
            {
                switch (args.ChangeType)
                {
                    case WatcherChangeTypes.Changed:
                        
                        break;
                    case WatcherChangeTypes.Created:
                        break;
                    case WatcherChangeTypes.Deleted:
                        break;
                    case WatcherChangeTypes.Renamed:
                        break;
                    default:
                        break;
                }
            };
        }
    }
}

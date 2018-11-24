using System.Collections.Generic;
using System.IO;

namespace MultiDriveSync
{
    public class LocalFileSynchronizer
    {
        private readonly MultiDriveSyncService _service;
        private readonly FileSystemWatcher watcher;
        private readonly IGoogleDriveClient googleDriveClient;
        private readonly string localRootPath;
        private readonly string userEmail;
        private readonly Dictionary<string, string> parentIdsByPath;

        public LocalFileSynchronizer(IGoogleDriveClient googleDriveClient, string localRootPath, string userEmail, MultiDriveSyncService service)
        {
            _service = service;
            watcher = new FileSystemWatcher();
            this.googleDriveClient = googleDriveClient;
            this.localRootPath = localRootPath;
            parentIdsByPath = new Dictionary<string, string>();
            this.userEmail = userEmail;
        }

        public void ChangeState(bool isEnabled)
        {
            watcher.EnableRaisingEvents = isEnabled;
        }

        public void InitializeParentIdsAndPaths(Dictionary<string, string> parentPathsById)
        {
            foreach (var kvp in parentPathsById)
            {
                parentIdsByPath[kvp.Value] = kvp.Key;
            }
        }

        public void StartSynchronization()
        {
            watcher.Changed += Watcher_Changed;
            watcher.Created += Watcher_Created;
            watcher.Deleted += Watcher_Deleted;
            watcher.Renamed += Watcher_Renamed;

            watcher.Path = localRootPath;
            watcher.IncludeSubdirectories = true;
            watcher.EnableRaisingEvents = true;
        }

        private async void Watcher_Renamed(object sender, RenamedEventArgs e)
        {
            var parentPath = Directory.GetParent(e.FullPath).FullName;
            if (parentIdsByPath.TryGetValue(parentPath, out var parentId))
            {
                var (id, ownerEmail) = await googleDriveClient.GetIdAsync(parentId, e.OldName);
                if (!string.IsNullOrEmpty(id) && ownerEmail == userEmail)
                {
                    await googleDriveClient.RenameAsync(id, e.Name);
                    if (parentIdsByPath.TryGetValue(e.OldFullPath, out var directoryId))
                    {
                        parentIdsByPath[e.FullPath] = directoryId;
                        parentIdsByPath.Remove(e.OldFullPath);
                    } 
                }
            }
        }

        private async void Watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            var parentPath = Directory.GetParent(e.FullPath).FullName;
            if (parentIdsByPath.TryGetValue(parentPath, out var parentId))
            {
                var (id, ownerEmail) = await googleDriveClient.GetIdAsync(parentId, e.Name);
                if (!string.IsNullOrEmpty(id) && ownerEmail == userEmail)
                {
                    await googleDriveClient.DeleteAsync(id);
                    if (parentIdsByPath.ContainsKey(e.FullPath))
                    {
                        parentIdsByPath.Remove(e.FullPath);
                    } 
                }
            }
        }

        private async void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            var parentPath = Directory.GetParent(e.FullPath).FullName;
            if (parentIdsByPath.TryGetValue(parentPath, out var parentId))
            {
                if (File.Exists(e.FullPath))
                {
                    using (var stream = File.OpenRead(e.FullPath))
                    {
                        await googleDriveClient.UploadFileAsync(e.Name, parentId, stream, userEmail);
                    }
                }
                else if (Directory.Exists(e.FullPath))
                {
                    var directoryId = await googleDriveClient.UploadFolderAsync(e.Name, parentId, userEmail);
                    parentIdsByPath[e.FullPath] = directoryId;
                }
            }
        }

        private async void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Changed)
            {
                var parentPath = Directory.GetParent(e.FullPath).FullName;
                if (parentIdsByPath.TryGetValue(parentPath, out var parentId))
                {
                    var (id, ownerEmail) = await googleDriveClient.GetIdAsync(parentId, e.Name);
                    if (!string.IsNullOrEmpty(id) && ownerEmail == userEmail)
                    {
                        using (var stream = File.OpenRead(e.FullPath))
                        {
                            await googleDriveClient.UpdateFileAsync(id, stream);
                        } 
                    }
                }
            }
        }
    }
}

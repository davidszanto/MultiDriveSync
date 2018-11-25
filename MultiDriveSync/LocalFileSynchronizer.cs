using MultiDriveSync.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace MultiDriveSync
{
    public class LocalFileSynchronizer
    {
        private readonly MultiDriveSyncService _service;
        private readonly FileSystemWatcher watcher;
        private readonly IGoogleDriveClient googleDriveClient;
        private readonly Dictionary<string, string> parentIdsByPath;
        private readonly Debouncer<FileSystemEventArgs> eventDebouncer;

        public LocalFileSynchronizer(IGoogleDriveClient googleDriveClient, MultiDriveSyncService service)
        {
            _service = service;
            watcher = new FileSystemWatcher();
            this.googleDriveClient = googleDriveClient;
            parentIdsByPath = new Dictionary<string, string>();
            eventDebouncer = new Debouncer<FileSystemEventArgs>(DebouncedEventHandler);
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
            
            watcher.Path = _service.Settings.LocalRootPath;
            watcher.IncludeSubdirectories = true;
            watcher.EnableRaisingEvents = true;
        }

        private async void Watcher_Renamed(object sender, RenamedEventArgs e)
        {
            var parentPath = Directory.GetParent(e.FullPath).FullName;
            if (parentIdsByPath.TryGetValue(parentPath, out var parentId))
            {
                var (id, ownerEmail) = await googleDriveClient.GetIdAsync(parentId, e.OldName);
                if (!string.IsNullOrEmpty(id) && CanExecuteOperation(ownerEmail))
                {
                    _service.BlackList.TryAdd(e.OldFullPath, null);
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
                if (!string.IsNullOrEmpty(id) && CanExecuteOperation(ownerEmail))
                {
                    _service.BlackList.TryAdd(e.FullPath, null);
                    await googleDriveClient.DeleteAsync(id);
                    if (parentIdsByPath.ContainsKey(e.FullPath))
                    {
                        parentIdsByPath.Remove(e.FullPath);
                    } 
                }
            }
        }

        private void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            eventDebouncer.Debounce(e.FullPath, e);
        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            eventDebouncer.Debounce(e.FullPath, e);
        }

        private void DebouncedEventHandler(FileSystemEventArgs e)
        {
            switch (e.ChangeType)
            {
                case WatcherChangeTypes.Created:
                    Created(e);
                    break;
                case WatcherChangeTypes.Changed:
                    Changed(e);
                    break;
                case WatcherChangeTypes.All:
                case WatcherChangeTypes.Deleted:
                case WatcherChangeTypes.Renamed:
                default:
                    throw new InvalidOperationException(e.ChangeType.ToString());
            }
        }

        private async void Created(FileSystemEventArgs e)
        {
            var parentPath = Directory.GetParent(e.FullPath).FullName;
            if (parentIdsByPath.TryGetValue(parentPath, out var parentId))
            {
                if (File.Exists(e.FullPath))
                {
                    using (var stream = File.OpenRead(e.FullPath))
                    {
                        _service.BlackList.TryAdd(e.FullPath, null);
                        var id = await googleDriveClient.UploadFileAsync(e.Name, parentId, stream, _service.Settings.UserEmail);
                    }
                }
                else if (Directory.Exists(e.FullPath))
                {
                    _service.BlackList.TryAdd(e.FullPath, null);
                    var directoryId = await googleDriveClient.UploadFolderAsync(e.Name, parentId, _service.Settings.UserEmail);
                    parentIdsByPath[e.FullPath] = directoryId;
                }
            }
        }

        private async void Changed(FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Changed)
            {
                var parentPath = Directory.GetParent(e.FullPath).FullName;
                if (parentIdsByPath.TryGetValue(parentPath, out var parentId))
                {
                    var (id, ownerEmail) = await googleDriveClient.GetIdAsync(parentId, e.Name);
                    if (!string.IsNullOrEmpty(id) && CanExecuteOperation(ownerEmail))
                    {
                        _service.BlackList.TryAdd(e.FullPath, null);
                        using (var stream = File.OpenRead(e.FullPath))
                        {
                            await googleDriveClient.UpdateFileAsync(id, stream);
                        }
                    }
                }
            }
        }

        private bool CanExecuteOperation(string ownerEmail)
        {
            switch (_service.Settings.EditAccessMode)
            {
                case EditAccessMode.OwnedOnly: return ownerEmail == _service.Settings.UserEmail;
                case EditAccessMode.All: return true;
                default: throw new ArgumentException(nameof(EditAccessMode));
            }
        }
    }
}

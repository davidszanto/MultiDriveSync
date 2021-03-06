﻿using MultiDriveSync.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MultiDriveSync
{
    internal class RemoteFileSynchronizer
    {
        private readonly IGoogleDriveClient googleDriveClient;
        private readonly MultiDriveSyncService _service;

        public Dictionary<string, string> FolderPathsById { get; }

        public RemoteFileSynchronizer(IGoogleDriveClient googleDriveClient, MultiDriveSyncService service)
        {
            this.googleDriveClient = googleDriveClient ?? throw new ArgumentNullException(nameof(googleDriveClient));
            FolderPathsById = new Dictionary<string, string>();
            _service = service;
            FolderPathsById[_service.Settings.StorageRootId] = _service.Settings.LocalRootPath;
        }

        public async Task InitializeAsync()
        {
            if (!(await googleDriveClient.HasChangesTokenAsync()))
            {
                _service.ChangeLocalWatcherState(false);
                await googleDriveClient.InitializeChangesTokenAsync();
                await DownloadAllFilesAsync();
                _service.ChangeLocalWatcherState(true);
            }
            else
            {
                await InitializeParentPathsByIdDictionaryAsync();
            }
        }

        public async Task RunSynchronization(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                foreach (var change in await googleDriveClient.GetChangesAsync())
                {
                    switch (change.ChangeContentType)
                    {
                        case ChangeContentType.Folder when change.ChangeType == ChangeType.CreatedOrUpdated:
                            CreateOrUpdateFolder(change.ParentId, change.Id, change.Name);
                            break;

                        case ChangeContentType.Folder when change.ChangeType == ChangeType.Deleted:
                            DeleteFolder(change.Id);
                            break;

                        case ChangeContentType.File when change.ChangeType == ChangeType.CreatedOrUpdated:
                            await CreateOrUpdateFile(change.ParentId, change.Id, change.Name);
                            break;

                        case ChangeContentType.File when change.ChangeType == ChangeType.Deleted:
                            DeleteFile(change.ParentId, change.Name);
                            break;

                        default:
                            throw new ArgumentException(nameof(ChangeContentType));
                    }
                }

                try
                {
                    _service.ChangeLocalWatcherState(true);
                    await Task.Delay(TimeSpan.FromMinutes(15), cancellationToken);
                    _service.ChangeLocalWatcherState(false);
                }
                catch (TaskCanceledException) { }
            }
        }

        private async Task DownloadAllFilesAsync()
        {
            var parentIdQueue = new Queue<string>();
            parentIdQueue.Enqueue(_service.Settings.StorageRootId);

            while (parentIdQueue.Count > 0)
            {
                var parentId = parentIdQueue.Dequeue();

                foreach (var child in await googleDriveClient.GetChildrenAsync(parentId))
                {
                    switch (child.ChangeContentType)
                    {
                        case ChangeContentType.File:
                            await CreateOrUpdateFile(child.ParentId, child.Id, child.Name);
                            break;
                        case ChangeContentType.Folder:
                            CreateOrUpdateFolder(child.ParentId, child.Id, child.Name);
                            parentIdQueue.Enqueue(child.Id);
                            break;
                        default:
                            throw new ArgumentException(nameof(ChangeContentType));
                    }
                }
            }
        }

        private async Task InitializeParentPathsByIdDictionaryAsync()
        {
            var parentIdQueue = new Queue<string>();
            parentIdQueue.Enqueue(_service.Settings.StorageRootId);

            while (parentIdQueue.Count > 0)
            {
                var parentId = parentIdQueue.Dequeue();

                foreach (var child in await googleDriveClient.GetChildrenFoldersAsync(parentId))
                {
                    var fullPath = Path.Combine(FolderPathsById[parentId], child.Name);
                    FolderPathsById[child.Id] = fullPath;
                    parentIdQueue.Enqueue(child.Id);
                }
            }
        }

        private void DeleteFolder(string folderId)
        {
            if (FolderPathsById.TryGetValue(folderId, out var path) && Directory.Exists(path))
            {
                if (_service.BlackList.ContainsKey(path))
                {
                    _service.BlackList.TryRemove(path, out var _);
                    return;
                }

                Directory.Delete(path, true);
                FolderPathsById.Remove(folderId);
            }
        }

        private void DeleteFile(string parentFolderId, string fileName)
        {
            if (FolderPathsById.TryGetValue(parentFolderId, out var parentFolderPath) && Directory.Exists(parentFolderId))
            {
                var filePathToDelete = Path.Combine(parentFolderPath, fileName);

                if (_service.BlackList.ContainsKey(filePathToDelete))
                {
                    _service.BlackList.TryRemove(filePathToDelete, out var _);
                    return;
                }

                if (File.Exists(filePathToDelete))
                {
                    File.Delete(filePathToDelete);
                }
            }
        }

        private void CreateOrUpdateFolder(string parentId, string folderId, string folderName)
        {
            if (FolderPathsById.TryGetValue(folderId, out var path) && Directory.Exists(path))
            {
                if (_service.BlackList.ContainsKey(path))
                {
                    _service.BlackList.TryRemove(path, out var _);
                    return;
                }

                var directory = new DirectoryInfo(path);
                var newPath = Path.Combine(directory.Parent.FullName, folderName);

                Directory.Move(path, newPath);
                FolderPathsById[folderId] = newPath;
            }
            else if (FolderPathsById.TryGetValue(parentId, out var parentPath) && Directory.Exists(parentPath))
            {
                var newDirectoryPath = Path.Combine(parentPath, folderName);

                if (_service.BlackList.ContainsKey(newDirectoryPath))
                {
                    _service.BlackList.TryRemove(newDirectoryPath, out var _);
                    return;
                }

                Directory.CreateDirectory(newDirectoryPath);
                FolderPathsById[folderId] = newDirectoryPath;
            }
        }

        private async Task CreateOrUpdateFile(string parentId, string fileId, string fileName)
        {
            if (FolderPathsById.TryGetValue(parentId, out var parentFolderPath) && Directory.Exists(parentFolderPath))
            {
                var filePath = Path.Combine(parentFolderPath, fileName);

                if (_service.BlackList.ContainsKey(filePath))
                {
                    _service.BlackList.TryRemove(filePath, out var _);
                    return;
                }

                using (var stream = File.Create(filePath))
                {
                    await googleDriveClient.DownloadFileAsync(fileId, stream);
                }
            }
        }
    }
}

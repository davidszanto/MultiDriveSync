using MultiDriveSync.Models;
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
        private readonly Dictionary<string, string> parentPathsById;
        private readonly string rootId;

        public RemoteFileSynchronizer(IGoogleDriveClient googleDriveClient, string localRootPath, string rootId)
        {
            this.googleDriveClient = googleDriveClient ?? throw new ArgumentNullException(nameof(googleDriveClient));
            parentPathsById = new Dictionary<string, string>();
            this.rootId = rootId;
            parentPathsById[rootId] = localRootPath;
        }

        public async Task InitializeAsync()
        {
            if (!(await googleDriveClient.HasChangesTokenAsync()))
            {
                await googleDriveClient.InitializeChangesTokenAsync();
                await DownloadAllFilesAsync(rootId);
            }
            else
            {
                await InitializeParentPathsByIdDictionaryAsync(rootId);
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
                    await Task.Delay(TimeSpan.FromMinutes(15), cancellationToken);
                }
                catch (TaskCanceledException) { }
            }
        }

        private async Task DownloadAllFilesAsync(string rootId)
        {
            var parentIdQueue = new Queue<string>();
            parentIdQueue.Enqueue(rootId);

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

        private async Task InitializeParentPathsByIdDictionaryAsync(string rootId)
        {
            var parentIdQueue = new Queue<string>();
            parentIdQueue.Enqueue(rootId);

            while (parentIdQueue.Count > 0)
            {
                var parentId = parentIdQueue.Dequeue();

                foreach (var child in await googleDriveClient.GetChildrenFoldersAsync(parentId))
                {
                    var fullPath = Path.Combine(parentPathsById[parentId], child.Name);
                    parentPathsById[child.Id] = fullPath;
                    parentIdQueue.Enqueue(child.Id);
                }
            }
        }

        private void DeleteFolder(string folderId)
        {
            if (parentPathsById.TryGetValue(folderId, out var path) && Directory.Exists(path))
            {
                Directory.Delete(path, true);
                parentPathsById.Remove(folderId);
            }
        }

        private void DeleteFile(string parentFolderId, string fileName)
        {
            if (parentPathsById.TryGetValue(parentFolderId, out var parentFolderPath) && Directory.Exists(parentFolderId))
            {
                var filePathToDelete = Path.Combine(parentFolderPath, fileName);
                if (File.Exists(filePathToDelete))
                {
                    File.Delete(filePathToDelete);
                }
            }
        }

        private void CreateOrUpdateFolder(string parentId, string folderId, string folderName)
        {
            if (parentPathsById.TryGetValue(folderId, out var path) && Directory.Exists(path))
            {
                var directory = new DirectoryInfo(path);
                var newPath = Path.Combine(directory.Parent.FullName, folderName);

                Directory.Move(path, newPath);
                parentPathsById[folderId] = newPath;
            }
            else
            {
                if (parentPathsById.TryGetValue(parentId, out var parentPath) && Directory.Exists(parentPath))
                {
                    var newDirectoryPath = Path.Combine(parentPath, folderName);
                    Directory.CreateDirectory(newDirectoryPath);
                    parentPathsById[folderId] = newDirectoryPath;
                }
            }
        }

        private async Task CreateOrUpdateFile(string parentId, string fileId, string fileName)
        {
            if (parentPathsById.TryGetValue(parentId, out var parentFolderPath) && Directory.Exists(parentFolderPath))
            {
                var filePath = Path.Combine(parentFolderPath, fileName);
                using (var stream = File.Create(filePath))
                {
                    await googleDriveClient.DownloadFileAsync(fileId, stream);
                }
            }
        }
    }
}

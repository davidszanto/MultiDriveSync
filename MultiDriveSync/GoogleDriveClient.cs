using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using MultiDriveSync.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MultiDriveSync
{
    public class GoogleDriveClient : IGoogleDriveClient
    {
        private const string CHANGES_TOKEN_KEY_TEMPLATE = "{0}_changesToken";
        private const string OWNER_KEY = "owner";

        private readonly UserCredential userCredential;
        private readonly DriveService driveService;
        private readonly IDataStore dataStore;
        private readonly string changesTokenKey;

        public GoogleDriveClient(UserCredential userCredential, string appName)
        {
            this.userCredential = userCredential;
            driveService = new DriveService(new BaseClientService.Initializer
            {
                ApplicationName = appName,
                HttpClientInitializer = userCredential
            });
            dataStore = new FileDataStore(appName);
            changesTokenKey = string.Format(CHANGES_TOKEN_KEY_TEMPLATE, userCredential.UserId);
        }

        public async Task<bool> HasChangesTokenAsync()
        {
            return !string.IsNullOrEmpty(await dataStore.GetAsync<string>(changesTokenKey));
        }

        public async Task InitializeChangesTokenAsync()
        {
            var response = await driveService.Changes.GetStartPageToken().ExecuteAsync();
            await dataStore.StoreAsync(changesTokenKey, response.StartPageTokenValue);
        }

        public async Task<List<Change>> GetChangesAsync()
        {
            var changes = new List<Change>();

            var pageToken = await dataStore.GetAsync<string>(changesTokenKey);
            while (!string.IsNullOrEmpty(pageToken))
            {
                var request = driveService.Changes.List(pageToken);
                request.Fields = "nextPageToken, newStartPageToken, changes(removed, file(id, name, parents, mimeType, modifiedTime, fullFileExtension))";
                var response = await request.ExecuteAsync();

                foreach (var change in response.Changes)
                {
                    var isFolder = change.File.MimeType == "application/vnd.google-apps.folder";

                    changes.Add(new Change
                    {
                        Id = change.File.Id,
                        Name = change.File.Name,
                        ParentId = change.File.Parents.First(),
                        LastModifiedDate = change.File.ModifiedTime ?? DateTime.Now,
                        ChangeContentType = isFolder ? ChangeContentType.Folder : ChangeContentType.File,
                        ChangeType = change.Removed ?? true ? ChangeType.Deleted : ChangeType.CreatedOrUpdated
                    });
                }

                if (!string.IsNullOrEmpty(response.NewStartPageToken))
                {
                    await dataStore.StoreAsync(changesTokenKey, response.NewStartPageToken);
                }

                pageToken = response.NextPageToken;
            }

            return changes;
        }

        public async Task DownloadFileAsync(string fileId, Stream stream)
        {
            await driveService.Files.Get(fileId).DownloadAsync(stream);
        }

        public async Task<string> UploadFolderAsync(string name, string parentId, string ownerEmail)
        {
            var file = new Google.Apis.Drive.v3.Data.File
            {
                Name = name,
                Parents = new List<string> { parentId },
                Properties = new Dictionary<string, string> { [OWNER_KEY] = ownerEmail },
                MimeType = "application/vnd.google-apps.folder"
            };

            var request = driveService.Files.Create(file);
            request.Fields = "id";

            var response = await request.ExecuteAsync();

            return response.Id;
        }

        public async Task UploadFileAsync(string name, string parentId, Stream stream, string ownerEmail)
        {
            var file = new Google.Apis.Drive.v3.Data.File
            {
                Name = name,
                Parents = new List<string> { parentId },
                Properties = new Dictionary<string, string> { [OWNER_KEY] = ownerEmail }
            };

            await driveService.Files.Create(file, stream, "application/octet-stream").UploadAsync();
        }

        public async Task DeleteAsync(string id)
        {
            await driveService.Files.Delete(id).ExecuteAsync(); 
        }

        public async Task RenameAsync(string id, string newName)
        {
            var file = new Google.Apis.Drive.v3.Data.File
            {
                Name = newName
            };

            await driveService.Files.Update(file, id).ExecuteAsync(); 
        }

        public async Task UpdateFileAsync(string id, Stream stream)
        {
            var file = new Google.Apis.Drive.v3.Data.File();
            await driveService.Files.Update(file, id, stream, "application/octet-stream").UploadAsync(); 
        }

        public async Task<(string id, string ownerEmail)> GetIdAsync(string parentId, string name)
        {
            var request = driveService.Files.List();
            request.Q = $"'{parentId}' in parents and name = '{name}' and trashed = false";
            request.Fields = "files(id, properties)";
            var result = await request.ExecuteAsync();

            var file = result.Files.FirstOrDefault();
            if (file != null)
            {
                return (file.Id, file.Properties[OWNER_KEY]);
            }

            return (string.Empty, string.Empty);
        }

        public async Task<string> GetRootIdAsync()
        {
            var response = await driveService.Files.Get("root").ExecuteAsync();
            return response.Id;
        }

        public async Task<List<Change>> GetChildrenAsync(string parentId)
        {
            var pageToken = string.Empty;
            var children = new List<Change>();

            do
            {
                var request = driveService.Files.List();
                request.Q = $"'{parentId}' in parents and trashed = false";
                request.Fields = "nextPageToken, files(id, name, parents, mimeType, modifiedTime, fullFileExtension)";
                request.PageToken = pageToken;
                var result = await request.ExecuteAsync();

                foreach (var child in result.Files)
                {
                    var isFolder = child.MimeType == "application/vnd.google-apps.folder";

                    children.Add(new Change
                    {
                        Id = child.Id,
                        Name = child.Name,
                        ParentId = child.Parents.First(),
                        LastModifiedDate = child.ModifiedTime ?? DateTime.Now,
                        ChangeContentType = isFolder ? ChangeContentType.Folder : ChangeContentType.File
                    });
                }

            } while (!string.IsNullOrEmpty(pageToken));

            return children;
        }

        private async Task<List<Folder>> GetDescendant(Folder parentFolder)
        {
            var children = await GetChildrenFoldersAsync(parentFolder.Id);

            if (!children.Any())
            {
                return new List<Folder>();
            }

            foreach (var folderChild in children)
            {
                folderChild.Children = await GetDescendant(folderChild);
            }

            return children;
        }

        public async Task<List<Folder>> GetChildrenFoldersAsync(string parentId)
        {
            var pageToken = string.Empty;
            var children = new List<Folder>();

            do
            {
                var request = driveService.Files.List();
                request.Q = $"mimeType = 'application/vnd.google-apps.folder' and '{parentId}' in parents and trashed = false";
                request.Fields = "nextPageToken, files(id, name, parents)";
                request.PageToken = pageToken;
                var result = await request.ExecuteAsync();

                foreach (var folder in result.Files)
                {
                    children.Add(new Folder
                    {
                        Id = folder.Id,
                        ParentId = parentId,
                        Name = folder.Name
                    });
                }

            } while (!string.IsNullOrEmpty(pageToken));

            return children;
        }

        public async Task<Folder> GetFoldersFromRoot(string rootId, string name)
        {
            var rootFolder = new Folder
            {
                Id = rootId,
                ParentId = null,
                Name = name
            };

            rootFolder.Children = await GetDescendant(rootFolder);
            return rootFolder;
        }

        public async Task DeleteStoredTokensAsync()
        {
            await dataStore.DeleteAsync<string>(changesTokenKey);
            await dataStore.DeleteAsync<TokenResponse>(userCredential.UserId);
        }
    }
}

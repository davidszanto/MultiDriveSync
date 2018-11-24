using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using MultiDriveSync.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiDriveSync
{
    public class GoogleDriveClient : IGoogleDriveClient
    {
        private readonly UserCredential userCredential;
        private readonly DriveService driveService;

        public GoogleDriveClient(UserCredential userCredential, string appName)
        {
            this.userCredential = userCredential;
            driveService = new DriveService(new Google.Apis.Services.BaseClientService.Initializer
            {
                ApplicationName = appName,
                HttpClientInitializer = userCredential
            });
        }

        public async Task<List<Folder>> GetChildrenFoldersAsync(string parentId)
        {
            var pageToken = string.Empty;
            var children = new List<Folder>();

            do
            {
                var request = driveService.Files.List();

                if (string.IsNullOrEmpty(parentId))
                {
                    parentId = "root";
                }

                request.Q = $"mimeType = 'application/vnd.google-apps.folder' and '{parentId}' in parents";

                request.Fields = "nextPageToken, files(id, name, parents)";

                request.PageToken = pageToken;
                var result = await request.ExecuteAsync();

                foreach (var folder in result.Files)
                {
                    children.Add(new Folder
                    {
                        Id = folder.Id,
                        Name = folder.Name
                    });
                }

            } while (!string.IsNullOrEmpty(pageToken));
            
            return children;
        }
    }
}

using MultiDriveSync.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MultiDriveSync
{
    public interface IGoogleDriveClient
    {
        Task<bool> HasChangesTokenAsync();
        Task InitializeChangesTokenAsync();
        Task<List<Change>> GetChangesAsync();
        Task DownloadFileAsync(string fileId, Stream stream);
        Task<string> GetRootIdAsync();
        Task<List<Change>> GetChildrenAsync(string parentId);
        Task<List<Folder>> GetChildrenFoldersAsync(string parentId);
    }
}

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
        Task<string> UploadFolderAsync(string name, string parentId, string ownerEmail);
        Task UploadFileAsync(string name, string parentId, Stream stream, string ownerEmail);
        Task DeleteAsync(string id);
        Task RenameAsync(string id, string newName);
        Task UpdateFileAsync(string id, Stream stream);
        Task<(string id, string ownerEmail)> GetIdAsync(string parentId, string name);
        Task<string> GetRootIdAsync();
        Task<List<Change>> GetChildrenAsync(string parentId);
        Task<List<Folder>> GetChildrenFoldersAsync(string parentId);
        Task<Folder> GetFoldersFromRoot(string rootId, string name);
        Task DeleteStoredTokensAsync();
    }
}

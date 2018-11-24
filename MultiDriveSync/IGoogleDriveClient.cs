using MultiDriveSync.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MultiDriveSync
{
    public interface IGoogleDriveClient
    {
        Task<List<Folder>> GetChildrenFoldersAsync(string parentId);
    }
}

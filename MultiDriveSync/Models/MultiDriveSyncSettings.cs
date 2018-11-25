using System;
using System.Collections.Generic;
using System.Text;

namespace MultiDriveSync.Models
{
    public class MultiDriveSyncSettings
    {
        public string StorageAccountId { get; set; }
        public string UserEmail { get; set; }
        public string StorageRootId { get; set; }
        public string LocalRootPath { get; set; }
        public EditAccessMode EditAccessMode { get; set; }
        public ClientInfo ClientInfo { get; set; }
    }
}

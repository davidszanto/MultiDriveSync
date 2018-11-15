using System;
using System.Collections.Generic;
using System.Text;

namespace MultiDriveSync.Models
{
    public class MultiDriveSyncSettings
    {
        public string StorageAccountId { get; set; }
        public string UserAccountId { get; set; }
        public string StorageRootPath { get; set; }
        public string LocalRootPath { get; set; }
        public EditAccessMode EditingAccessLevel { get; set; }
        public ClientInfo ClientInfo { get; set; }
    }
}

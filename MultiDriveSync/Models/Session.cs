using System;
using System.Collections.Generic;
using System.Text;

namespace MultiDriveSync.Models
{
    public class Session
    {
        public UserInfo StorageAccountInfo { get; set; }
        public UserInfo UserInfo { get; set; }
        public string LocalRoot { get; set; }
        public string RemoteRoot { get; set; }
        public EditAccessMode EditAccessMode { get; set; }
    }
}

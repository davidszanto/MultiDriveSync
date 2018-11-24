using System;
using System.Collections.Generic;
using System.Text;

namespace MultiDriveSync.Models
{
    public class Change
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ParentId { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public ChangeContentType ChangeContentType { get; set; }
        public ChangeType ChangeType { get; set; }
    }
}

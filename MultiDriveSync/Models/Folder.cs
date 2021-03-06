﻿using System;
using System.Collections.Generic;
using System.Text;

namespace MultiDriveSync.Models
{
    public class Folder
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ParentId { get; set; }
        public List<Folder> Children { get; set; }
    }
}

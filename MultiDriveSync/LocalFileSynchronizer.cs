using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MultiDriveSync
{
    class LocalFileSynchronizer
    {
        private readonly FileSystemWatcher watcher;

        public void StartSynchronization()
        {
            // TODO: inicializalni a watchert

            watcher.EnableRaisingEvents = true;
        }
    }
}

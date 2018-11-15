using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MultiDriveSync
{
    class RemoteFileSynchronizer
    {
        public async Task RunSynchronization(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // TODO: sync

                // TODO: ellenorizni, hogy hibat dob-e megszakitaskor, vagy csak abbahagyja a varakozast
                await Task.Delay(TimeSpan.FromMinutes(15), cancellationToken);
            }
        }
    }
}

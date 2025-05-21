using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace projectTracker.Application.Interfaces
{
    public interface ISyncManager
    {
        Task SyncAsync(CancellationToken ct);
        //Task SyncAsync(DateTime lastSyncTime, CancellationToken ct);
        //Task SyncAsync(string projectKey, CancellationToken ct);
        //Task SyncAsync(string projectKey, DateTime lastSyncTime, CancellationToken ct);
    }
}

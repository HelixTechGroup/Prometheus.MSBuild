using System.Collections.Generic;
using Microsoft.Build.Framework;

namespace Prometheus.MSBuild.Tasks.Caching
{
    public struct ImportCacheResult
    {
        public IList<ImportCacheItem> Cache { get; set; }

        public IList<ITaskItem> CachedItems { get; set; }

        public IList<ITaskItem> NewItems { get; set; }
    }
}

using System.Collections.Generic;
using System.Linq;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Prometheus.MSBuild.Tasks.Caching
{
    public struct ImportCacheResult
    {
        public IList<ImportCacheItem> ImportItems { get; set; }

        public IList<ITaskItem> CachedItems { get; set; }

        public IList<ITaskItem> NewItems { get; set; }

        public ImportCacheResult(IEnumerable<ImportCacheItem> cache)
        {
            ImportItems = cache.ToArray();
            CachedItems = cache.Select(c => new TaskItem(c.FilePath)).ToArray();
            NewItems = cache.Select(c => new TaskItem(c.FilePath)).ToArray();
        }
    }
}

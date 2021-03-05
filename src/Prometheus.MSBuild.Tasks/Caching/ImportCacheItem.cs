using System;
using System.Text.Json.Serialization;
using Microsoft.Build.Construction;
using Prometheus.MSBuild.Tasks.Converters;

namespace Prometheus.MSBuild.Tasks.Caching
{
    [JsonConverter(typeof(ImportCacheItemJsonConverter))]
    public struct ImportCacheItem
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public DateTime DateCached { get; set; }
        public string Checksum { get; set; }
        public ProjectRootElement Contents { get; set; }
    }
}

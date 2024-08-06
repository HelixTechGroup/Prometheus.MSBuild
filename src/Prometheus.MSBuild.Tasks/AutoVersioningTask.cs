using System;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

using Prometheus.MSBuild.Tasks.Settings;

namespace Prometheus.MSBuild.Tasks
{
    public class AutoVersioningTask : PrometheusTask<PropertySearchSettings>
    {
        [Output]
        public Version AssemblyVersion { get; set; }

        /// <inheritdoc />
        public override bool Execute()
        {

            return true;
        }
    }
}
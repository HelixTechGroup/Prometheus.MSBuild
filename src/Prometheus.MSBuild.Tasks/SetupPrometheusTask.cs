using System;
using System.IO;

using Microsoft.Build.Framework;

using Prometheus.MSBuild.Tasks.Settings;

namespace Prometheus.MSBuild.Tasks
{
    public class SetupPrometheusTask : PrometheusTask<PrometheusTaskSettings>
    {
        /// <inheritdoc />
        public override bool Execute()
        {
            Log.LogMessage(MessageImportance.High, $"| {m_settings.SectionSymbol} Current Directory: {Directory.GetCurrentDirectory()}");
            return true;
        }
    }
}
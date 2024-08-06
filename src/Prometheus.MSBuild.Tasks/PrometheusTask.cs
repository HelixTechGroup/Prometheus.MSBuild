using Microsoft.Build.Utilities;

using Prometheus.MSBuild.Tasks.Settings;

namespace Prometheus.MSBuild.Tasks
{
    public abstract class PrometheusTask<TOptions> : Task
    where TOptions : PrometheusTaskSettings, new()
    {
        protected TOptions m_settings = new TOptions();

        public string SectionSymbol
        {
            get { return m_settings.SectionSymbol; }
            set { m_settings.SectionSymbol = value; }
        }
    }
}
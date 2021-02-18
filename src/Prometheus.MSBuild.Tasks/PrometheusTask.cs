using Microsoft.Build.Utilities;

namespace Prometheus.MSBuild.Tasks
{
    public abstract class PrometheusTask : Task
    {
        protected PrometheusTaskOptions m_options = new PrometheusTaskOptions();

        public string SectionSymbol
        {
            get { return m_options.SectionSymbol; }
            set { m_options.SectionSymbol = value; }
        }
    }
}
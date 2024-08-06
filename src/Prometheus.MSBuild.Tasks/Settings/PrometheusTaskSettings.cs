namespace Prometheus.MSBuild.Tasks.Settings
{
    public class PrometheusTaskSettings
    {
        protected string m_sectionSymbol = "X";

        public string SectionSymbol
        {
            get { return m_sectionSymbol; }
            set { m_sectionSymbol = value; }
        }
    }
}

using System.Collections.Generic;

using Microsoft.Build.Execution;

namespace Prometheus.MSBuild.Tasks.Settings
{
    public class PropertySearchSettings : PrometheusTaskSettings
    {
        protected string m_propertyName = @"*";
        protected bool m_localOnly = true;
        protected IList<string> m_files;

        public string ProjectFile { get; set; }

        public string PropertyName
        {
            get { return m_propertyName; }
            set { m_propertyName = value; }
        }

        public bool ImportsOnly
        {
            get { return m_localOnly; }
            set { m_localOnly = value; }
        }

        public IList<ProjectPropertyInstance> PropertiesFound { get; set; }

        public IList<string> ProjectFiles
        {
            get { return m_files; }
            set { m_files = value; }
        }
    }
}

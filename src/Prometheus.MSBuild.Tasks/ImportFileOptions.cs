using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;

namespace Prometheus.MSBuild.Tasks
{
    public class ImportFileOptions : PrometheusTaskOptions
    {
        protected IList<ITaskItem> m_files;

        public string ImportFile { get; set; }

        public IList<ITaskItem> ImportFiles
        {
            get { return m_files; }
            set { m_files = value; }
        }
    }
}

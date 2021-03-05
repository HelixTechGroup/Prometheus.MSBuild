using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Prometheus.MSBuild.Tasks.Caching;

namespace Prometheus.MSBuild.Tasks
{
    public class ImportFileOptions : PrometheusTaskOptions
    {
        protected IList<ITaskItem> m_files;
        protected bool m_useCache = true;

        public string ImportFile { get; set; }

        public bool UseCache
        {
            get { return m_useCache; }
            set { m_useCache = value; }
        }

        public IList<ITaskItem> ImportFiles
        {
            get { return m_files; }
            set { m_files = value; }
        }

        public ImportCacheResult Cache { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prometheus.MSBuild.Tasks
{
    public class PrometheusTaskOptions
    {
        protected string m_sectionSymbol = "X";

        public string SectionSymbol
        {
            get { return m_sectionSymbol; }
            set { m_sectionSymbol = value; }
        }
    }
}

using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

using Microsoft.Build.Framework;
//using Microsoft.IO;

using Prometheus.MSBuild.Tasks.Settings;

namespace Prometheus.MSBuild.Tasks
{
    public class ReplaceFileTokenTask : PrometheusTask<PrometheusTaskSettings>
    {
        protected string m_inputFile;
        protected string m_outputFile;
        protected string m_templateName;
        protected string[] m_fileTokens;
        protected string[] m_fileTokensValue;

        public override bool Execute()
        {
            if (!File.Exists(m_inputFile))
                return false;

            var c = File.ReadAllText(m_inputFile);
            for (var i = 0; m_fileTokens.Length > i; i++)
                c = c.Replace(m_fileTokens[i], m_fileTokensValue[i]);

            File.WriteAllText(m_outputFile, c);
            return true;
        }
    }
}

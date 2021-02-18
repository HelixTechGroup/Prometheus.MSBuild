using System;
using BuildUtil = Microsoft.Build.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Microsoft.Build.Construction;
using Microsoft.Build.Execution;
using Microsoft.Build.Graph;

namespace Prometheus.MSBuild.Tasks
{
    public class ImportFileTask : PrometheusTask
    {
        protected string m_cacheFileName = "imports.pcache";
        protected ImportFileOptions m_options = new ImportFileOptions();

        [Required]
        public ITaskItem[] ImportFiles
        {
            get { return m_options.ImportFiles.ToArray(); }
            set { m_options.ImportFiles = value; }
        }

        [Output]
        public ITaskItem[] ItemList { get; set; }

        public override bool Execute()
        {
            Log.LogMessage(MessageImportance.High, $"| {m_options.SectionSymbol} Importing Files.");

            if (m_options.ImportFiles.Count == 0)
            {
                Log.LogError($"ImportFiles parameter cannot be empty.");
                return false;
            }

            //if (m_options.ImportFiles.Any(f => !File.Exists(f)))
            foreach (var f in m_options.ImportFiles)
            {
                if (!File.Exists(f.ItemSpec))
                {
                    Log.LogError($"ImportFile {f} does not exists.");
                    return false;
                }
            }
            
            var project = this.GetProjectInstance();
            this.AddImports(project, m_options);
            //Log.LogMessage(MessageImportance.High, $"| {m_options.SectionSymbol} Import Files: {m_options.ImportFiles}");
            

            return true;
        }

        protected void CheckCache()
        {
            var instance = this.GetProjectInstance();
            var cachePath = Path.Combine(instance.GetPropertyValue("BaseIntermediateOutputPath"), m_cacheFileName);
            if (File.Exists(cachePath))
            {
                using var stream = File.OpenRead(cachePath);
                var imports = JsonDocument.Parse(stream);
            }
        }

        protected void CreateCache()
        {

        }
    }
}

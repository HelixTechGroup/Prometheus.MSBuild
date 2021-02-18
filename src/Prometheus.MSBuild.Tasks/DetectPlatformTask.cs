using System;
using BuildUtil = Microsoft.Build.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using System.IO;
using System.Reflection;
using Microsoft.Build.Construction;
using Microsoft.Build.Execution;

namespace Prometheus.MSBuild.Tasks
{
    public class DetectPlatformTask : BuildUtil.Task
    {
        protected PrometheusTaskOptions m_options = new PrometheusTaskOptions();

        [Required]
        public string TargetPlatform { get; set; }

        [Output]
        public string RuntimeId { get; set; }

        [Output] 
        public string PlatformId { get; set; }

        public string SectionSymbol
        {
            get { return m_options.SectionSymbol; }
            set { m_options.SectionSymbol = value; }
        }

        /// <inheritdoc />
        public override bool Execute()
        {
            if (string.IsNullOrWhiteSpace(TargetPlatform))
            {
                Log.LogError("TargetPlatform cannot be empty");
                return false;
            }

            if (!TargetPlatform.Contains('-'))
            {
                //Log.LogError("TargetPlatform has incorrect format(runtime-platform)");
                return true;
            }

            var tmp = TargetPlatform.Split('-');
            RuntimeId = tmp[0];
            PlatformId = tmp[1];

            Log.LogMessage(MessageImportance.High, $"| {m_options.SectionSymbol} Platform Detected");
            Log.LogMessage(MessageImportance.High, $"| {m_options.SectionSymbol} RuntimeId: {RuntimeId}");
            Log.LogMessage(MessageImportance.High, $"| {m_options.SectionSymbol} PlatformId: {PlatformId}");

            return true;
        }
    }
}

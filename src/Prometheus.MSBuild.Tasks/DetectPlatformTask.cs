using System;
using BuildUtil = Microsoft.Build.Utilities;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using System.IO;
using System.Reflection;
using Microsoft.Build.Construction;
using Microsoft.Build.Execution;
using Prometheus.MSBuild.Tasks.Platform;

namespace Prometheus.MSBuild.Tasks
{
    public class DetectPlatformTask : BuildUtil.Task
    {
        protected PrometheusTaskOptions m_options = new PrometheusTaskOptions();

        [Required]
        public string TargetPlatform { get; set; }

        [Output] 
        public bool RuntimeDetected { get; set; }

        [Output]
        public bool PlatformDetected { get; set; }

        [Output]
        public string RuntimeId { get; set; }

        [Output]
        public string PlatformId { get; set; }

        [Output] 
        public string PlatformVersion { get; set; }

        [Output]
        public bool IsCoreRuntime { get; set; }

        [Output]
        public string RuntimeVersion    { get; set; }

        [Output]
        public string RuntimeTargetFramework { get; set; }

        public string SectionSymbol
        {
            get { return m_options.SectionSymbol; }
            set { m_options.SectionSymbol = value; }
        }

        /// <inheritdoc />
        public override bool Execute()
        {
            //if (!Debugger.IsAttached)
            //    Debugger.Launch();
            //else
            //    Debugger.Break();

            PlatformId = RuntimeId = RuntimeTargetFramework = "None";
            PlatformVersion = RuntimeVersion = "0.0";
            var versionRegex = new Regex(@$"\d+?(?:\.\d+)+");
            var lazyVersionRegex = new Regex($@"\d+?\.?\d+?");
            var coreVersionRegex = new Regex(@$"^net\d+(?:\.\d+)+");
            var platformVersionRegex = new Regex(@$"\S+(\d+)");

            if (string.IsNullOrWhiteSpace(TargetPlatform))
            {
                Log.LogError("TargetPlatform cannot be empty");
                return false;
            }

            if (!TargetPlatform.Contains('-'))
            {
                Log.LogMessage("No Platform detected.");
                PlatformId = "None";
                RuntimeTargetFramework = RuntimeId = TargetPlatform;
                RuntimeVersion = lazyVersionRegex.IsMatch(RuntimeId) ? lazyVersionRegex.Match(RuntimeId).Value : "0.0";
            }
            else
            {
                var tmp = TargetPlatform.Split('-');
                RuntimeId = tmp[0];
                if (tmp.Length > 1)
                {
                    PlatformDetected = tmp.Length >= 2;
                    PlatformId = PlatformDetected ? tmp[1] : "None";
                }

                if (TargetPlatform.Contains("netcore"))
                {
                    RuntimeTargetFramework = "netcoreapp3.1";
                    IsCoreRuntime = true;
                    RuntimeVersion = "3.1";
                    RuntimeDetected = true;
                }
                else
                {
                    RuntimeTargetFramework = !PlatformDetected ? tmp[0] : @$"{tmp[0]}-{tmp[1]}";
                    RuntimeVersion = lazyVersionRegex.IsMatch(RuntimeId) ? lazyVersionRegex.Match(RuntimeId).Value : "0.0";
                    //Log.LogMessage(MessageImportance.High, $"| {m_options.SectionSymbol} DETECTED VERSION: {RuntimeVersion}");
                    RuntimeDetected = true;
                    IsCoreRuntime = coreVersionRegex.IsMatch(RuntimeId);
                }

                var versionMatch = versionRegex.Match(PlatformId);
                PlatformVersion = versionMatch.Success ? versionMatch.Value : "0.0";
                if (versionMatch.Success)
                {
                    PlatformId = PlatformId.Replace(versionMatch.Value, string.Empty);
                }

                if (PlatformId == "android")
                {
                    if (!versionMatch.Success)
                    {
                        PlatformVersion = AndroidSdkVersion.Latest;
                        //RuntimeTargetFramework = @$"{tmp[0]}-{tmp[1]}{PlatformVersion}";
                    }
                }
            }

            Log.LogMessage(MessageImportance.High, $"| {m_options.SectionSymbol} Platform Detected: {PlatformDetected}");
            Log.LogMessage(MessageImportance.High, $"| {m_options.SectionSymbol} Runtime Detected: {RuntimeDetected}");
            Log.LogMessage(MessageImportance.High, $"| {m_options.SectionSymbol} RuntimeId: {RuntimeId}");
            Log.LogMessage(MessageImportance.High, $"| {m_options.SectionSymbol} RuntimeVersion: {RuntimeVersion}");
            Log.LogMessage(MessageImportance.High, $"| {m_options.SectionSymbol} RuntimeTargetFramework: {RuntimeTargetFramework}");
            Log.LogMessage(MessageImportance.High, $"| {m_options.SectionSymbol} IsCoreRuntime: {IsCoreRuntime}");
            Log.LogMessage(MessageImportance.High, $"| {m_options.SectionSymbol} PlatformId: {PlatformId}");
            Log.LogMessage(MessageImportance.High, $"| {m_options.SectionSymbol} PlatformVersion: {PlatformVersion}");

            return true;
        }
    }
}

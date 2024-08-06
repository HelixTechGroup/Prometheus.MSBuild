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
using System.Threading;
using Microsoft.Build.Construction;
using Microsoft.Build.Execution;
using Prometheus.MSBuild.Tasks.Extension;
using Prometheus.MSBuild.Tasks.Settings;
using Shin.Collections.Concurrent;

namespace Prometheus.MSBuild.Tasks
{
    public class LogPropertyTask : PrometheusTask<PropertySearchSettings>
    {
        [Required]
        public string ProjectFiles
        {
            get { return m_settings.ProjectFiles.ToString(); }
            set { m_settings.ProjectFiles = value.Split(';'); }
        }

        public string PropertyName
        {
            get { return m_settings.PropertyName; }
            set { m_settings.PropertyName = value; }
        }

        public bool ImportsOnly
        {
            get { return m_settings.ImportsOnly; }
            set { m_settings.ImportsOnly = value; }
        }

        //[Output]
        //public IList<ProjectPropertyInstance> PropertiesFound { get; set; }

        public string SectionSymbol
        {
            get { return m_settings.SectionSymbol; }
            set { m_settings.SectionSymbol = value; }
        }

        public override bool Execute()
        {
            //Debugger.NotifyOfCrossThreadDependency();
            //if (!Debugger.IsAttached)
            //    Debugger.Launch();
            //else
            //    Debugger.Break();

            var found = new ConcurrentList<ProjectPropertyInstance>();
            //Log.LogMessage(MessageImportance.High, ProjectFiles);

            if (m_settings.ProjectFiles.Count == 0)
            {
                Log.LogError($"ProjectFile {ProjectFiles} does not exists.");
                return false;
            }

            if (m_settings.ProjectFiles.Any(f => !File.Exists(f)))
            {
                Log.LogError($"ProjectFile {ProjectFiles} does not exists.");
                return false;
            }

            //var file = this.GetFileInstance(ProjectFile);
            //Debugger.Break();

            ProjectInstance project = null;
            for (var a = 0; a < 3; a++)
            {
                project = this.GetProjectInstance();
                if (project == null)
                {
                    Thread.Sleep(1000);
                    continue;
                    //Log.LogWarning($"Could not get Project Instance.");
                    //return false;
                }

                break;
            }

            if (project == null)
            {
                Log.LogWarning($"Could not get Project Instance.");
                return true;
            }

            //m_sectionSymbol = project.GetPropertyValue("SectionSymbol");
            //Log.LogError($"{m_sectionSymbol}");

            //if (file == null)
            //{
            //    Log.LogError($"ProjectFile {ProjectFile} does not exists.");
            //    return false;
            //}

            //if (!Debugger.IsAttached)
            //    Debugger.Launch();
            //else
            //    Debugger.Break();

            Log.LogMessage(MessageImportance.High, $"| {m_settings.SectionSymbol} Property Name: {m_settings.PropertyName}");
            Log.LogMessage(MessageImportance.High, $"| {m_settings.SectionSymbol} Imports Only: {m_settings.ImportsOnly}");
            found = new ConcurrentList<ProjectPropertyInstance>(this.GetProperties(project, m_settings));
            Log.LogMessage(MessageImportance.High, $"| {m_settings.SectionSymbol} Found: {found.Count}");

            foreach (var n in found)
            {
                Log.LogMessage(MessageImportance.High, $"| {m_settings.SectionSymbol} {n.Name}: {n.EvaluatedValue}");
            }

            return true;
        }
    }
}

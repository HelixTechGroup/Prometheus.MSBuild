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
using Shin.Framework.Collections.Concurrent;

namespace Prometheus.MSBuild.Tasks
{
    public class LogPropertyTask : BuildUtil.Task
    {
        PropertySearchOptions m_options = new PropertySearchOptions();

        [Required]
        public string ProjectFiles
        {
            get { return m_options.ProjectFiles.ToString(); }
            set { m_options.ProjectFiles = value.Split(';'); }
        }

        public string PropertyName
        {
            get { return m_options.PropertyName; }
            set { m_options.PropertyName = value; }
        }

        public bool ImportsOnly
        {
            get { return m_options.ImportsOnly; }
            set { m_options.ImportsOnly = value; }
        }

        //[Output]
        //public IList<ProjectPropertyInstance> PropertiesFound { get; set; }

        public string SectionSymbol
        {
            get { return m_options.SectionSymbol; }
            set { m_options.SectionSymbol = value; }
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

            if (m_options.ProjectFiles.Count == 0)
            {
                Log.LogError($"ProjectFile {ProjectFiles} does not exists.");
                return false;
            }

            if (m_options.ProjectFiles.Any(f => !File.Exists(f)))
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
                    Thread.Sleep(5000);
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

            Log.LogMessage(MessageImportance.High, $"| {m_options.SectionSymbol} Property Name: {m_options.PropertyName}");
            Log.LogMessage(MessageImportance.High, $"| {m_options.SectionSymbol} Imports Only: {m_options.ImportsOnly}");
            found = new ConcurrentList<ProjectPropertyInstance>(this.GetProperties(project, m_options));
            Log.LogMessage(MessageImportance.High, $"| {m_options.SectionSymbol} Found: {found.Count}");

            foreach (var n in found)
            {
                Log.LogMessage(MessageImportance.High, $"| {m_options.SectionSymbol} {n.Name}: {n.EvaluatedValue}");
            }

            return true;
        }
    }
}

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
using Prometheus.MSBuild.Tasks.Extension;

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

        [Output]
        public IList<ProjectPropertyInstance> PropertiesFound { get; set; }

        public string SectionSymbol
        {
            get { return m_options.SectionSymbol; }
            set { m_options.SectionSymbol = value; }
        }

        public override bool Execute()
        {
            var found = PropertiesFound = new List<ProjectPropertyInstance>();
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
            var project = this.GetProjectInstance();

            //m_sectionSymbol = project.GetPropertyValue("SectionSymbol");
            //Log.LogError($"{m_sectionSymbol}");

            //if (file == null)
            //{
            //    Log.LogError($"ProjectFile {ProjectFile} does not exists.");
            //    return false;
            //}

            Log.LogMessage(MessageImportance.High, $"| {m_options.SectionSymbol} Property Name: {m_options.PropertyName}");
            Log.LogMessage(MessageImportance.High, $"| {m_options.SectionSymbol} Imports Only: {m_options.ImportsOnly}");
            PropertiesFound = this.GetProperties(project, m_options);
            Log.LogMessage(MessageImportance.High, $"| {m_options.SectionSymbol} Found: {PropertiesFound.Count}");

            foreach (var n in PropertiesFound)
            {
                Log.LogMessage(MessageImportance.High, $"| {m_options.SectionSymbol} {n.Name}: {n.EvaluatedValue}");
            }

            return true;
        }
    }
}

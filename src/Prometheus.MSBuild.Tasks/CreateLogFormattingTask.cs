using System;
using BuildUtil = Microsoft.Build.Utilities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using System.IO;
using System.Reflection;
using System.Security;
using Microsoft.Build.Construction;
using Microsoft.Build.Execution;
using Prometheus.MSBuild.Tasks.Extension;

namespace Prometheus.MSBuild.Tasks
{
    public class CreateLogFormattingTask : BuildUtil.Task
    {
        protected StringExtensions m_headerChar = new StringExtensions(@"~");
        protected StringExtensions m_sectionChar = new StringExtensions(@"=");
        protected string m_headerString = string.Empty;
        protected string m_sectionString = string.Empty;
        protected PrometheusTaskOptions m_options = new PrometheusTaskOptions();
        protected string m_sectionPrefix = @"[]";
        protected string m_headerPrefix = @"[]";
        protected string m_sectionPostfix = @"[]";
        protected string m_headerPostfix = @"[]";
        protected IList<string> m_headerDecoration = new List<string>();

        [DefaultValue(40)]
        public int SectionLength { get; set; }

        [DefaultValue(40)]
        public int HeaderLength { get; set; }

        public string HeaderChar
        {
            get { return m_headerChar.RealString; }
            set { m_headerChar = value; }
        }

        public string SectionChar
        {
            get { return m_sectionChar.RealString; }
            set { m_sectionChar = new StringExtensions(value); }
        }

        [Output]
        public string HeaderString
        {
            get { return $"{m_headerPrefix}{m_headerString}"; }
            set { m_headerString = SecurityElement.Escape(value); }
        }

        [Output]
        public string SectionString
        {
            get { return $"{m_sectionPrefix}{m_sectionString}"; }
            set { m_sectionString = SecurityElement.Escape(value); }
        }

        public string SectionSymbol
        {
            get { return m_options.SectionSymbol; }
            set { m_options.SectionSymbol = value; }
        }

        public string SectionPrefix
        {
            get { return m_sectionPrefix; }
            set { m_sectionPrefix = value; }
        }

        public string HeaderPrefix
        {
            get { return m_headerPrefix; }
            set { m_headerPrefix = value; }
        }

        public string SectionPostfix
        {
            get { return m_sectionPostfix; }
            set { m_sectionPostfix = value; }
        }

        public string HeaderPostfix
        {
            get { return m_headerPostfix; }
            set { m_headerPostfix = value; }
        }

        /// <inheritdoc />
        public override bool Execute()
        {
            m_sectionString = m_sectionPrefix + (m_sectionChar * SectionLength);
            m_headerString = m_headerPrefix + (m_headerChar * HeaderLength) + m_headerPostfix;
            //m_sectionPrefix = m_headerPrefix = @"[]";

            var project = this.GetProjectInstance();
            var name = project.GetPropertyValue("MSBuildProjectName");
            Log.LogMessage(MessageImportance.High, $"| {m_sectionPrefix}{m_sectionString}");
            Log.LogMessage(MessageImportance.High, $"| {m_headerPrefix}==~- --Setting Prometheus Log Format for {name}-- -~=={m_headerPostfix}");
            Log.LogMessage(MessageImportance.High, $"| {m_sectionPrefix}{m_headerString}");
            Log.LogMessage(MessageImportance.High, $"| {m_options.SectionSymbol} SectionLength: {SectionLength}");
            Log.LogMessage(MessageImportance.High, $"| {m_options.SectionSymbol} HeaderLength: {HeaderLength}");
            Log.LogMessage(MessageImportance.High, $"| {m_options.SectionSymbol} SectionChar: {m_sectionChar}");
            Log.LogMessage(MessageImportance.High, $"| {m_options.SectionSymbol} HeaderChar: {m_headerChar}");
            Log.LogMessage(MessageImportance.High, $"| {m_options.SectionSymbol} SectionPrefix: {m_sectionPrefix}");
            Log.LogMessage(MessageImportance.High, $"| {m_options.SectionSymbol} HeaderPrefix: {m_headerPrefix}");
            Log.LogMessage(MessageImportance.High, $"| {m_options.SectionSymbol} SectionPostfix: {m_sectionPostfix}");
            Log.LogMessage(MessageImportance.High, $"| {m_options.SectionSymbol} HeaderPostfix: {m_headerPostfix}");

            Log.LogMessage(MessageImportance.High, $"| {m_options.SectionSymbol} Formatted SectionString: {m_sectionString}");
            Log.LogMessage(MessageImportance.High, $"| {m_options.SectionSymbol} Formatted HeaderString: {m_headerString}");
            return true;
        }
    }
}

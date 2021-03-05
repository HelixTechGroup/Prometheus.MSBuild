using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;

namespace Prometheus.MSBuild.Tasks
{
    public static class ProjectInstanceExtensions
    {
        public static ProjectInstance MergeProject(this ProjectInstance instance, Project project)
        {
            var pathsField = instance.GetType().GetField("_importPaths", BindingFlags.NonPublic | BindingFlags.Instance);
            var paths = pathsField.GetValue(instance) as List<string>;

            foreach (var i in project.Imports)
            {
                if (!paths.All(ps => ps != i.ImportedProject.FullPath))
                {
                    if (!i.IsImported)
                        Console.WriteLine($"Import was not imported correctly.\r\n File: {i.ImportedProject.FullPath}");
                    //task.Log.LogMessage(MessageImportance.High, $"| {options.SectionSymbol} Import Path Added: {i.ImportedProject.FullPath}");
                    paths.Add(i.ImportedProject.FullPath);
                    Console.WriteLine($"Adding Import: {i.ImportedProject.FullPath}");
                }

                foreach (var pp in i.ImportedProject.Properties)
                {
                    instance.SetProperty(pp.Name, pp.Value);
                    Console.WriteLine($"Setting Property: {pp.Name} Value: {pp.Value}");
                }

                //task.Log.LogMessage(MessageImportance.High, $"| {options.SectionSymbol} {i.ImportingElement.Project}: {i.ImportedProject.FullPath}");
                foreach (var pi in i.ImportedProject.Items)
                {
                    var dir = Directory.GetParent(pi.IncludeLocation.File);
                    var v = pi.Include;
                    v = v.Replace("$(MSBuildThisFileDirectory)", dir.FullName + @"\").Replace(@"\\", @"\");

                    if (!instance.Items.Any(ei => ei.EvaluatedInclude == v))
                        instance.AddItem(pi.ItemType, v);

                    Console.WriteLine($"Adding Item: {pi.ItemType} Include: {pi.Include}");
                }
            }

            pathsField.SetValue(instance, paths);

            return instance;
        }
    }
}

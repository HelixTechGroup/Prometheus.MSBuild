﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using BuildUtil = Microsoft.Build.Utilities;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.ObjectModelRemoting;

namespace Prometheus.MSBuild.Tasks
{
    internal static class BuildTaskExtensions
    {
        public static IList<ProjectPropertyInstance> GetProperties(this BuildUtil.Task task, ProjectInstance instance, PropertySearchOptions options)
        {
            var props = new HashSet<ProjectPropertyInstance>();
            try
            {
                var currentProperties = instance.Properties;
                var preFiltered = new HashSet<ProjectProperty>();
                var total = 0;
                foreach (var f in options.ProjectFiles)
                {
                    var project = task.GetFileInstance(f);
                    var properties = project.Properties;
                    //preFiltered.AddRange(properties);

                    foreach (var p in properties)
                    {
                        if (preFiltered.Contains(p))
                        {
                            preFiltered.RemoveWhere((p1) => p1.Name == p.Name);
                        }

                        preFiltered.Add(p);
                    }
                    

                    total += preFiltered.Count;

                    if (options.ImportsOnly)
                    {
                        //!(p.IsReservedProperty | p.IsGlobalProperty | p.IsEnvironmentProperty)
                        properties = properties.Where(p => !(p.IsReservedProperty | p.IsGlobalProperty | p.IsEnvironmentProperty))
                                               .ToArray();
                    }

                    if (options.PropertyName != "*" || string.IsNullOrWhiteSpace(options.PropertyName))
                    {
                        //if (options.PropertyName.StartsWith(@"r\"))
                        //{
                            //Regex.Escape();
                            var eString = options.PropertyName/*.Remove(0, 2)*/;
                            var regex = new Regex(eString);
                            task.Log.LogMessage(MessageImportance.High, $"| {options.SectionSymbol} Using Regex: {options.PropertyName} : {eString}");
                            properties = properties.Where(p => regex.IsMatch(p.Name)).ToArray();
                        //}
                        //else
                        //    properties = properties.Where(p => p.Name == options.PropertyName).ToArray();
                    }

                    //PropertiesFound = new List<ProjectProperty>(properties);
                    //var tmpProps = props;
                    props = new HashSet<ProjectPropertyInstance>(currentProperties
                                                                    .Where(p => properties
                                                                              .Any(cp => cp.Name == p.Name)));
                }

                task.Log.LogMessage(MessageImportance.High, $"| {options.SectionSymbol} Total: {total}");
                 
                if (!props.Any())
                {
                    //task.Log.LogMessage(MessageImportance.High, $"| {options.SectionSymbol} ---------------------------------------");
                    task.Log.LogMessage(MessageImportance.High, $"| {options.SectionSymbol} No Results found, Printing All Properties");
                    task.Log.LogWarning($"No Results found, Printing All Properties");
                    foreach (var n in preFiltered)
                    {
                        task.Log.LogMessage(MessageImportance.High, $"| {options.SectionSymbol} {n.Name}: {n.EvaluatedValue}");
                    }
                }
                //props = new HashSet<ProjectPropertyInstance>(props).ToList();
                //task.Log.LogMessage(MessageImportance.High, $"| {options.SectionSymbol} ---------------------------------------");
            }
            catch (Exception e)
            {
                task.LogError(e);
            }

            return props.ToArray();
        }

        public static Project GetFileInstance(this BuildUtil.Task task, string fileName)
        {
            Project project = null;

            try
            {
                var buildEngine = task.BuildEngine as IBuildEngine7;
                var props = buildEngine?.GetGlobalProperties() as IDictionary<string, string>;

                var collection = new ProjectCollection(props);
                project = collection.LoadProject(fileName);
            }
            catch (Exception e)
            {
                task.LogError(e);
            }

            return project;
        }

        public static ProjectInstance GetProjectInstance(this BuildUtil.Task task)
        {
            var buildEngine = ((IBuildEngine6)task.BuildEngine);
            var requestEntryField = buildEngine.GetType().GetField("_requestEntry", BindingFlags.NonPublic | BindingFlags.Instance);
            var requestEntry = requestEntryField.GetValue(buildEngine);
            var requestConfigProperty = requestEntry.GetType().GetProperty("RequestConfiguration", BindingFlags.Instance | BindingFlags.Public);
            var requestConfig = requestConfigProperty.GetValue(requestEntry);
            var projectProperty = requestConfig.GetType().GetProperty("Project", BindingFlags.Public | BindingFlags.Instance);
            return (ProjectInstance)projectProperty.GetValue(requestConfig);
        }

        public static void AddImports(this BuildUtil.Task task, ProjectInstance instance, ImportFileOptions options)
        {
            try 
            { 
                var buildEngine = ((IBuildEngine6)task.BuildEngine);
                var bm = BuildManager.DefaultBuildManager;
                var requestEntryField = buildEngine.GetType().GetField("_requestEntry", BindingFlags.NonPublic | BindingFlags.Instance);
                var requestEntry = requestEntryField.GetValue(buildEngine);
                var requestConfigProperty = requestEntry.GetType().GetProperty("RequestConfiguration", BindingFlags.Public | BindingFlags.Instance);
                var requestConfig = requestConfigProperty.GetValue(requestEntry);
                var replaceMethod = bm.GetType().GetMethod("ReplaceExistingProjectInstance", BindingFlags.NonPublic | BindingFlags.Instance);
                //var creatConfig = bm.GetType().GetMethod("CreateConfiguration", BindingFlags.NonPublic | BindingFlags.Instance);
                var resolveConfig = bm.GetType().GetMethod("ResolveConfiguration", BindingFlags.NonPublic | BindingFlags.Instance);
                var addConfig = bm.GetType().GetMethod("AddNewConfiguration", BindingFlags.NonPublic | BindingFlags.Instance);
                var cloneMethod = requestConfig.GetType().GetMethod("ShallowCloneWithNewId", BindingFlags.NonPublic | BindingFlags.Instance);

                var root = instance.ToProjectRootElement();
                var project = new Project(root);
                var group = root.AddImportGroup();
                foreach (var f in options.ImportFiles)
                {
                    task.Log.LogMessage(MessageImportance.High, $"| {options.SectionSymbol} Importing File: {f}");
                    var ie = group.AddImport(f.ItemSpec);
                    ie.Label = "Shared";

                    var p = new Project(root);
                    //p.FullPath = MSBuildHelper.AssemblyDirectory;
                    var pathsField = instance.GetType().GetField("_importPaths", BindingFlags.NonPublic | BindingFlags.Instance);
                    //var pathDupField = instance.GetType().GetField("_importPathsIncludingDuplicates", BindingFlags.NonPublic | BindingFlags.Instance);
                    var paths = pathsField.GetValue(instance) as List<string>;
                    //var dupPath = pathDupField.GetValue(instance) as List<string>;


                    //foreach (var di in p.ImportsIncludingDuplicates)
                    //{
                    //    if (!dupPath.Any(ps => ps == di.ImportedProject.FullPath))
                    //        dupPath.Add(di.ImportedProject.FullPath);
                    //}

                    //task.Log.LogMessage(MessageImportance.High, $"| {options.SectionSymbol} {p.FullPath}");
                    foreach (var i in p.Imports)
                    {
                        if (!paths.Any(ps => ps == i.ImportedProject.FullPath))
                        {
                            task.Log.LogMessage(MessageImportance.High, $"| {options.SectionSymbol} Import Path Added: {i.ImportedProject.FullPath}");
                            paths.Add(i.ImportedProject.FullPath);
                        }

                        foreach (var pp in i.ImportedProject.Properties)
                        {
                            instance.SetProperty(pp.Name, pp.Value);
                        }

                        //task.Log.LogMessage(MessageImportance.High, $"| {options.SectionSymbol} {i.ImportingElement.Project}: {i.ImportedProject.FullPath}");
                        foreach (var pi in i.ImportedProject.Items)
                        {
                            var dir = Directory.GetParent(pi.IncludeLocation.File);
                            var v = pi.Include;
                            v = v.Replace("$(MSBuildThisFileDirectory)", dir.FullName + @"\").Replace(@"\\", @"\");

                            if (!instance.Items.Any(ei => ei.EvaluatedInclude == v))
                                instance.AddItem(pi.ItemType, v);
                        }
                    }

                    pathsField.SetValue(instance, paths);

                    var newConfig = cloneMethod?.Invoke(requestConfig, new object[] {int.MaxValue});
                    var projectProp = newConfig.GetType().GetProperty("Project", BindingFlags.Public | BindingFlags.Instance);
                    var configIdField = newConfig.GetType().GetField("_configId", BindingFlags.NonPublic | BindingFlags.Instance);
                    configIdField?.SetValue(newConfig, int.MinValue);
                    projectProp?.SetValue(newConfig, instance);

                    try
                    {
                        replaceMethod?.Invoke(bm, new[] {newConfig, requestConfig});
                    }
                    catch (TargetInvocationException tie)
                    {
                        Console.WriteLine(tie.InnerException.Message);
                        task.Log.LogMessage(MessageImportance.High, tie.InnerException.Message);
                    }
                }
            }
            catch (TargetInvocationException tie)
            {
                Console.WriteLine(tie);
                task.Log.LogErrorFromException(tie, true, true, tie.Source);
                //task.LogError(tie.InnerException);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                task.LogError(e);
                if (e.InnerException != null)
                {
                    Console.WriteLine(e.InnerException);
                    task.LogError(e.InnerException);
                }
            }
        }

        public static void LogError(this BuildUtil.Task task, Exception exception)
        {
            task.Log.LogError($"{exception.Message} \n---- {exception.StackTrace}");
        }
    }
}

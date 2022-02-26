using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Prometheus.MSBuild.Tasks.Caching;
using BuildUtil = Microsoft.Build.Utilities;

namespace Prometheus.MSBuild.Tasks.Extension
{
    internal static class BuildTaskExtensions
    {
        public static IList<ProjectPropertyInstance> GetProperties(this BuildUtil.Task task, ProjectInstance instance, PropertySearchOptions options)
        {
            var props = new HashSet<ProjectPropertyInstance>();
            try
            {
                Debugger.Break();
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

        const BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public;

        public static IEnumerable GetEnvironmentVariable(this BuildUtil.Task task, string key, bool throwIfNotFound)
        {
            var projectInstance = GetProjectInstance(task);

            var items = projectInstance.Items
                .Where(x => string.Equals(x.ItemType, key, StringComparison.InvariantCultureIgnoreCase)).ToList();
            if (items.Count > 0)
            {
                return items.Select(x => x.EvaluatedInclude);
            }


            var properties = projectInstance.Properties
                .Where(x => string.Equals(x.Name, key, StringComparison.InvariantCultureIgnoreCase)).ToList();
            if (properties.Count > 0)
            {
                return properties.Select(x => x.EvaluatedValue);
            }

            if (throwIfNotFound)
            {
                throw new Exception(string.Format("Could not extract from '{0}' environmental variables.", key));
            }

            return Enumerable.Empty<string>();
        }

        //public static ProjectInstance GetProjectInstance(this BuildUtil.Task task)
        //{
        //    string projectFile = Microsoft.Build.BuildEngine.ProjectFile;

        //    //Engine buildEngine = new Engine(System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory());

        //    Project project = new Project(buildEngine);
        //    project.(projectFile);
        //    foreach (var o in project.EvaluatedProperties)
        //    {
        //        // Use properties
        //    }

        //    // Do what you want

        //    return project.CreateProjectInstance();
        //}
        //public static ProjectInstance GetProjectInstance(this BuildUtil.Task task)
        //{
        //    var buildEngine = task.BuildEngine as IBuildEngine10 ;
        //    var buildEngineType = buildEngine.GetType();
        //    var targetBuilderCallbackField = buildEngineType.GetField("targetBuilderCallback", bindingFlags);
        //    if (targetBuilderCallbackField == null)
        //    {
        //        throw new Exception("Could not extract targetBuilderCallback from " + buildEngineType.FullName);
        //    }
        //    var targetBuilderCallback = targetBuilderCallbackField.GetValue(buildEngine);
        //    var targetCallbackType = targetBuilderCallback.GetType();
        //    var projectInstanceField = targetCallbackType.GetField("projectInstance", bindingFlags);
        //    if (projectInstanceField == null)
        //    {
        //        throw new Exception("Could not extract projectInstance from " + targetCallbackType.FullName);
        //    }
        //    return (ProjectInstance)projectInstanceField.GetValue(targetBuilderCallback);
        //}

        public static ProjectInstance GetProjectInstance(this BuildUtil.Task task)
        {
            //if (!Debugger.IsAttached)
            //    Debugger.Launch();
            //else
            //    Debugger.Break();

            var buildEngine = ((IBuildEngine10)task.BuildEngine);
            //var buildEngineType = buildEngine as OutOfProcNode;
            //return new ProjectInstance(task.GetFileInstance(task.BuildEngine.));

            var taskHost = buildEngine.GetType();

            var projectInstance = new ProjectInstance(task.BuildEngine.ProjectFileOfTaskNode);

            if (taskHost.FullName.Contains("Microsoft.Build.BackEnd.TaskHost"))
            {
                Debugger.Break();
                var requestEntryField = taskHost.GetField("_requestEntry", bindingFlags);
                var requestEntry = requestEntryField?.GetValue(buildEngine);

                var requestConfigProperty = requestEntry?.GetType().GetProperty("RequestConfiguration", bindingFlags);
                var requestConfig = requestConfigProperty?.GetValue(requestEntry);

                var projectProperty = requestConfig?.GetType().GetProperty("Project", bindingFlags);
                projectInstance = (ProjectInstance)projectProperty?.GetValue(requestConfig);
            }
            else
            {
                Debugger.Break();
                var currentConfigField = taskHost.GetField("_currentConfiguration", bindingFlags);
                var currentConfig = currentConfigField?.GetValue(buildEngine);
                var buildEngineMember = taskHost.GetProperty("EngineServices", bindingFlags);
                var buildEngineService = buildEngineMember?.GetValue(buildEngine);
            }

            return projectInstance;
        }

        public static void AddImports(this BuildUtil.Task task, ref ProjectInstance instance, ImportFileOptions options)
        {
            try 
            { 
                var buildEngine = /*((IBuildEngine6)*/task.BuildEngine/*)*/;
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

                if (options.Cache.NewItems.Count > 0)
                {
                    var root = instance.ToProjectRootElement();
                    var group = root.AddImportGroup();
                    foreach (var f in options.Cache.NewItems)
                    {
                        task.Log.LogMessage(MessageImportance.High, $"| {options.SectionSymbol} Importing File: {f}");
                        var ie = group.AddImport(f.ItemSpec);
                        ie.Label = "Shared";

                        //p.FullPath = MSBuildHelper.AssemblyDirectory;
                        //var pathDupField = instance.GetType().GetField("_importPathsIncludingDuplicates", BindingFlags.NonPublic | BindingFlags.Instance);
                        //var dupPath = pathDupField.GetValue(instance) as List<string>;


                        //foreach (var di in p.ImportsIncludingDuplicates)
                        //{
                        //    if (!dupPath.Any(ps => ps == di.ImportedProject.FullPath))
                        //        dupPath.Add(di.ImportedProject.FullPath);
                        //}

                        //task.Log.LogMessage(MessageImportance.High, $"| {options.SectionSymbol} {p.FullPath}");
                    }

                    var project = new Project(root);
                    instance = instance.MergeProject(project);
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
                    task.Log.LogErrorFromException(e.InnerException, true, true, e.InnerException.Source);
                }
            }
        }

        public static void LogError(this BuildUtil.Task task, Exception exception)
        {
            task.Log.LogError($"{exception.Message} \n---- {exception.StackTrace}");
        }
    }
}

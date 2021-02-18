using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Build.BuildEngine;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Tasks;
using Project = Microsoft.Build.Evaluation.Project;

namespace Prometheus.MSBuild.Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            //Console.WriteLine("Please hit any key to continue....");
            //Console.ReadKey();

            var currentDirectory = MSBuildHelper.AssemblyDirectory;
            var testsDirectory = Path.Combine(MSBuildHelper.AssemblyDirectory, "tests");
            var prometheusDirectory = $@"{currentDirectory}\..\..\..\..\..\src\Prometheus.MSBuild";
            var prometheusLibraryDirectory = $@"{currentDirectory}\..\..\..\..\..\build\Debug\Prometheus.MSBuild.Tasks\net48";
            var prometheusTasksLibrary = $@"{prometheusLibraryDirectory}\Prometheus.MSBuild.Tasks.dll";

            //var props = new Dictionary<string, string>() {{"Configuration", "Debug"}, {"Platform", "AnyCPU" } };

            //var xml = new XmlDocument();
            //var dec =  xml.AppendChild(xml.CreateXmlDeclaration("1.0", null, null));
            //xml.AppendChild(xml.CreateElement("Project"));
            //using var xmlStream = new MemoryStream();
            //xml.Save(xmlStream);

            //xmlStream.Flush(); //Adjust this if you want read your data 
            //xmlStream.Position = 0;
            //xmlStream.Seek(0L, SeekOrigin.Begin);
            //var reader = XmlReader.Create(xmlStream);

            var fileName = $@"{testsDirectory}\Template.csproj";
            //var collection = new ProjectCollection(props);
            //var project = collection.LoadProject(fileName);
            //var instance = new ProjectInstance(ProjectRootElement.Create(fileName, collection));

            //Project project = null;
            ProjectInstance instance = null;

            //try
            //{
            //    project = MSBuildHelper.CreateProject(fileName);
            //}
            //catch (Exception ex)
            //{
            //    var c = Console.ForegroundColor;
            //    Console.ForegroundColor = ConsoleColor.Red;
            //    Console.WriteLine(ex.Message);
            //    Console.ForegroundColor = c;
            //    return;
            //}

            try
            {
                instance = MSBuildHelper.CreateProjectInstance(fileName);
            }
            catch (Exception ex)
            {
                var c = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ForegroundColor = c;
                return;
            }

            //instance.ToProjectRootElement().AddImport(@$"{prometheusDirectory}\build\Prometheus.MSBuild.props");
            //Console.WriteLine($"Default Configuration: {project.GetPropertyValue("Configuration")}");
            Console.WriteLine($"Active Configuration: {instance.GetPropertyValue("Configuration")}");
            Console.WriteLine($"Active PlatformId: {instance.GetPropertyValue("PlatformId")}");

            //instance.ToProjectRootElement().InitialTargets = "ImportTest";
            //instance.InitialTargets.Add("ImportTest");
            var root = instance.ToProjectRootElement();
            //root.Sdk = "Microsoft.Build.NoTargets/2.0.1";
            root.Sdk = "Microsoft.NET.Sdk";
            //var sdk = root.CreateProjectSdkElement("Microsoft.Build.NoTargets", "2.0.1");

            //root.AddUsingTask("Prometheus.MSBuild.Tasks.ImportFileTask",
            //                                             prometheusTasksLibrary,
            //                                             null);
            //root.AddUsingTask("Prometheus.MSBuild.Tasks.LogPropertyTask",
            //                             prometheusTasksLibrary,
            //                             null);
            //root.AddUsingTask("Prometheus.MSBuild.Tasks.CreateLogFormattingTask",
            //                  prometheusTasksLibrary,
            //                  null);
            //root.AddUsingTask("Prometheus.MSBuild.Tasks.DetectPlatformTask",
            //                  prometheusTasksLibrary,
            //                  null);

            //root.AddTarget("ImportTest")
            //    .AddTask("ImportFileTask")
            //    .SetParameter("ImportFiles", @$"{prometheusDirectory}\Platform\Windows\Platform.Build.props");

            root.AddTarget("CoreCompile");
            var prometheusProps = root.CreateImportElement(@$"{prometheusDirectory}\build\Prometheus.MSBuild.props");
            var prometheusTargets = root.CreateImportElement(@$"{prometheusDirectory}\build\Prometheus.MSBuild.targets");

            //var prometheusProps = root.CreateImportElement(@$"{prometheusDirectory}\Sdk\Sdk.props");
            //var prometheusTargets = root.CreateImportElement(@$"{prometheusDirectory}\Sdk\Sdk.targets");

            var prometheusProperties = root.CreatePropertyGroupElement();

            root.InsertBeforeChild(prometheusProps, root.FirstChild);
            root.InsertAfterChild(prometheusTargets, root.LastChild);
            root.InsertAfterChild(prometheusProperties, root.FirstChild);

            var plat = "net5.0-none";
            if (args.Length >= 1)
                plat = args[0];
            prometheusProperties.AddProperty("TargetPlatform", plat);
            prometheusProperties.AddProperty("TargetPlatformIdentifier", "windows");
            prometheusProperties.AddProperty("TargetPlatformVersion", "7.0");
            prometheusProperties.AddProperty("TargetFrameworkIdentifier", ".NETCoreApp");
            prometheusProperties.AddProperty("TargetFrameworkVersion", "5.0");
            prometheusProperties.AddProperty("TargetFramework", "net5.0");
            prometheusProperties.AddProperty("IsLibraryProject", "true");
            prometheusProperties.AddProperty("PrometheusLibDirectory", prometheusLibraryDirectory);
            prometheusProperties.AddProperty("EnableDefaultCompileItems", "false");
            prometheusProperties.AddProperty("IsPackable", "false");
            prometheusProperties.AddProperty("DisableWarnForInvalidRestoreProjects", "true");
            prometheusProperties.AddProperty("FirstTimeLoading", "false");

            ProjectInstance testInstance = null;
            var pTask = new Task(() =>
            {
                testInstance = MSBuildHelper.CreateProjectInstance(root);
            });
            pTask.ConfigureAwait(false);
            pTask.Start();
            var i = 0;
            while (!pTask.IsCompleted)
            {
                drawTextProgressBar(i, 10000);
                Thread.Sleep(500);
                i++;
            }

            var target = "";
            if (args.Length >= 2)
                target = args[1];

            Console.WriteLine();
            Console.WriteLine("--------------------");
            //Copy copy = new Copy();
            //NuGet.Common.PathUtility.GetStringComparerBasedOnOS();
            //NuGet.Build.Tasks.WarnForInvalidProjectsTask warn = new NuGet.Build.Tasks.WarnForInvalidProjectsTask();
            //NuGetMessageTask
            // @"C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\3.1.0\ref\netcoreapp3.1\System.Runtime.dll"
            //Assembly.Load(@"System.Runtime, Version=4.2.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
            var libTask = new Task(() =>
            {
                Assembly.Load(@"Microsoft.Build.Tasks.Core");
                Assembly.Load(@"NuGet.Frameworks");
                Assembly.Load(@"NuGet.ProjectModel");
                Assembly.LoadFile(Path.GetFullPath(Path.Combine(MSBuildHelper.GetGlobalProperties()["RoslynTargetsPath"],
                                                                "Microsoft.Build.Tasks.CodeAnalysis.dll")));
            });
            libTask.ConfigureAwait(false);
            libTask.Start();
            i = 0;
            while (!libTask.IsCompleted)
            {
                drawTextProgressBar(i, 10000);
                Thread.Sleep(500);
                i++;
            }
            
            var buildTask = new Task(() =>
                                       {
                                           testInstance.Build(target, new ILogger[] {new ConsoleLogger(LoggerVerbosity.Normal)});
                                       });
            buildTask.ConfigureAwait(false);
            buildTask.Start();
            i = 0;
            while (!buildTask.IsCompleted)
            {
                drawTextProgressBar(i, 10000);
                Thread.Sleep(500);
                i++;
            }

            Console.WriteLine();
            Console.WriteLine("--------------------");
            Console.WriteLine();
            instance.UpdateStateFrom(testInstance);
            Console.WriteLine($"Active Configuration: {instance.GetPropertyValue("Configuration")}");
            Console.WriteLine($"Active PlatformId: {instance.GetPropertyValue("PlatformId")}");
            foreach (var t in testInstance.InitialTargets)
            {
                Console.WriteLine($"InitialTargets: {t}");
            }

            foreach (var k in instance.Targets.Keys)
            {
                Console.WriteLine($"Target Name: {k}");
            }

            Console.WriteLine();
            Console.WriteLine("--------------------");
            Console.WriteLine();
            Console.WriteLine(root.RawXml);

            //Console.WriteLine("Please hit any key to continue....");
            //Console.ReadKey();
        }

        private static void drawTextProgressBar(int progress, int total)
        {
            //draw empty progress bar
            Console.CursorLeft = 0;
            Console.Write("["); //start
            Console.CursorLeft = 32;
            Console.Write("]"); //end
            Console.CursorLeft = 1;
            float onechunk = 30.0f / total;

            //draw filled part
            int position = 1;
            for (int i = 0; i < onechunk * progress; i++)
            {
                Console.BackgroundColor = ConsoleColor.Gray;
                Console.CursorLeft = position++;
                Console.Write(" ");
            }

            //draw unfilled part
            for (int i = position; i <= 31; i++)
            {
                Console.BackgroundColor = ConsoleColor.Green;
                Console.CursorLeft = position++;
                Console.Write(" ");
            }

            //draw totals
            Console.CursorLeft = 35;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.Write(progress.ToString() + " of " + total.ToString() + "    "); //blanks at the end remove any excess
        }
    }
}

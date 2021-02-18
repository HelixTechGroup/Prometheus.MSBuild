using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Utilities;

namespace Prometheus.MSBuild.Tests
{
    public class MSBuildHelper
    {
        public static ProjectCollection CreateProjectCollection(string projectPath = "", string toolsPath = "", bool isSdkStyle = true, IDictionary<string, string> globalProperties = null)
        {
            var tools = string.IsNullOrWhiteSpace(toolsPath) ? GetToolsPath(isSdkStyle) : toolsPath;
            var props = globalProperties ?? GetGlobalProperties(projectPath, tools);
            var collection = new ProjectCollection(props);
            collection.AddToolset(new Toolset(ToolLocationHelper.CurrentToolsVersion, tools, collection, string.Empty));

            return collection;
        }

        public static Project CreateProject(string projectPath, bool isSdkStyle = true)
        {
            var tools = GetToolsPath(isSdkStyle);
            var collection = CreateProjectCollection(projectPath, tools);
            return collection.LoadProject(projectPath);
        }

        public static ProjectInstance CreateProjectInstance(string projectPath, bool isSdkStyle = true)
        {
            var tools = GetToolsPath(isSdkStyle);
            var collection = CreateProjectCollection(projectPath, tools);
            return new ProjectInstance(ProjectRootElement.Create(projectPath, collection));
        }

        public static ProjectInstance CreateProjectInstance(ProjectRootElement root, bool isSdkStyle = true)
        {
            var tools = GetToolsPath(isSdkStyle);
            var props = GetGlobalProperties(toolsPath: tools);
            var collection = CreateProjectCollection(toolsPath: tools, globalProperties: props);
            return new ProjectInstance(root, props, null, collection);
        }

        public static string GetToolsPath(bool isSdkStyle = true)
        {
            if (isSdkStyle)
                return GetCoreBasePath(AssemblyDirectory);

            string toolsPath = ToolLocationHelper.GetPathToBuildToolsFile("msbuild.exe", ToolLocationHelper.CurrentToolsVersion);
            if (string.IsNullOrEmpty(toolsPath))
            {
                toolsPath = PollForToolsPath().FirstOrDefault();
            }

            if (string.IsNullOrEmpty(toolsPath))
            {
                throw new Exception("Could not locate the tools (MSBuild) path.");
            }

            return Path.GetDirectoryName(toolsPath);
        }

        public static string[] PollForToolsPath()
        {
            var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

            return new[]
                   {
                       Path.Combine(programFilesX86, @"MSBuild\15.0\Bin\MSBuild.exe"),
                       Path.Combine(programFilesX86, @"MSBuild\14.0\Bin\MSBuild.exe")
                   }.Where(File.Exists).ToArray();
        }

        public static IDictionary<string, string> GetGlobalProperties(string projectPath = "", string toolsPath = "", bool isSdkStyle = true)
        {
            var tools = string.IsNullOrWhiteSpace(toolsPath) ? GetToolsPath(isSdkStyle) : toolsPath;
            //string solutionDir = !string.IsNullOrWhiteSpace(projectPath) && File.Exists(projectPath) ? Path.GetDirectoryName(projectPath) : AssemblyDirectory;
            var solutionDir = Path.Combine(AssemblyDirectory, "tests");
            string extensionsPath = isSdkStyle ? tools : Path.GetFullPath(Path.Combine(toolsPath, @"..\..\"));
            string sdksPath = Path.Combine(extensionsPath, "Sdks");
            string roslynTargetsPath = Path.Combine(toolsPath, "Roslyn");

            var props = new Dictionary<string, string>()
                   {
                       {"Configuration", "Debug"},
                       {"Platform", "AnyCpu"},
                       {"RootPath", solutionDir},
                       {"SolutionDir", solutionDir},
                       {"MSBuildExtensionsPath", extensionsPath},
                       {"MSBuildSDKsPath", sdksPath},
                       {"RoslynTargetsPath", roslynTargetsPath}
                   };
            props["DesignTimeBuild"] = "true";
            props["BuildProjectReferences"] = "false";
            props["SkipCompilerExecution"] = "true";
            props["ProvideCommandLineArgs"] = "true";

            Environment.SetEnvironmentVariable(
                                               "SolutionDir",
                                               props["SolutionDir"]);
            Environment.SetEnvironmentVariable(
                                               "MSBuildExtensionsPath",
                                               props["MSBuildExtensionsPath"]);
            Environment.SetEnvironmentVariable(
                                               "MSBuildSDKsPath",
                                               props["MSBuildSDKsPath"]);
            Environment.SetEnvironmentVariable(
                                               "RoslynTargetsPath",
                                               props["RoslynTargetsPath"]);

            return props;

        }

        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        public static string GetCoreBasePath(string projectPath)
        {
            const string DOTNET_CLI_UI_LANGUAGE = nameof(DOTNET_CLI_UI_LANGUAGE);

            // Ensure that we set the DOTNET_CLI_UI_LANGUAGE environment variable to "en-US" before
            // running 'dotnet --info'. Otherwise, we may get localized results.
            string originalCliLanguage = Environment.GetEnvironmentVariable(DOTNET_CLI_UI_LANGUAGE);
            Environment.SetEnvironmentVariable(DOTNET_CLI_UI_LANGUAGE, "en-US");

            try
            {
                // Create the process info
                ProcessStartInfo startInfo = new ProcessStartInfo("dotnet", "--info")
                {
                    // global.json may change the version, so need to set working directory
                    WorkingDirectory = Path.GetDirectoryName(projectPath),
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                // Execute the process
                using (Process process = Process.Start(startInfo))
                {
                    List<string> lines = new List<string>();
                    process.OutputDataReceived += (_, e) =>
                    {
                        if (!string.IsNullOrWhiteSpace(e.Data))
                        {
                            lines.Add(e.Data);
                        }
                    };
                    process.BeginOutputReadLine();
                    process.WaitForExit();
                    return ParseCoreBasePath(lines);
                }
            }
            finally
            {
                Environment.SetEnvironmentVariable(DOTNET_CLI_UI_LANGUAGE, originalCliLanguage);
            }
        }

        public static string ParseCoreBasePath(List<string> lines)
        {
            if (lines == null || lines.Count == 0)
            {
                throw new Exception("Could not get results from `dotnet --info` call");
            }

            foreach (string line in lines)
            {
                int colonIndex = line.IndexOf(':');
                if (colonIndex >= 0
                    && line.Substring(0, colonIndex).Trim().Equals("Base Path", StringComparison.OrdinalIgnoreCase))
                {
                    return line.Substring(colonIndex + 1).Trim();
                }
            }

            throw new Exception("Could not locate base path in `dotnet --info` results");
        }
    }
}

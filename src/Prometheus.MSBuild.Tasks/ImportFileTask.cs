using System;
using System.Buffers;
using System.Collections.Concurrent;
using BuildUtil = Microsoft.Build.Utilities;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using System.IO;
using System.Reflection;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Build.Construction;
using Microsoft.Build.Execution;
using Prometheus.MSBuild.Tasks.Caching;
using Prometheus.MSBuild.Tasks.Converters;
using Prometheus.MSBuild.Tasks.Extension;
using Prometheus.MSBuild.Tasks.Settings;
using Shin.Collections.Concurrent;

namespace Prometheus.MSBuild.Tasks
{
    public class ImportFileTask : PrometheusTask<ImportFileSettings>
    {
        protected string m_cacheFileName = "project.prometheus.import.cache";

        [Required]
        public ITaskItem[] ImportFiles
        {
            get { return m_settings.ImportFiles?.ToArray(); }
            set { m_settings.ImportFiles = value; }
        }

        public bool UseCache
        {
            get { return m_settings.UseCache; }
            set { m_settings.UseCache = value; }
        }

        [Output]
        public ITaskItem[] CachedImportFiles
        {
            get { return m_settings.Cache.CachedItems.ToArray(); }
        }

        public override bool Execute()
        {
            //if (!Debugger.IsAttached)
            //    Debugger.Launch();
            //else
            //    Debugger.Break();
            var project = this.GetProjectInstance();
            var objPath = project?.GetPropertyValue("BaseIntermediateOutputPath");
            if (objPath == null)
                return true;

            var fileName = Path.Combine(objPath, m_cacheFileName);
            Log.LogMessage(MessageImportance.High, $"| {m_settings.SectionSymbol} Importing Files.");
            if (m_settings.ImportFiles.Count == 0)
            {
                Log.LogWarning($"ImportFiles parameter is empty.");
                return true;
            }

            //System.Type type = System.Type.GetTypeFromProgID("VisualStudio.DTE.10.0");
            //Object obj = System.Activator.CreateInstance(type, true);
            //EnvDTE80.DTE2 dte = (EnvDTE100.DTE2)obj;

            //if (m_options.ImportFiles.Any(f => !File.Exists(f)))
            var validFiles = new ConcurrentList<ITaskItem>();
            foreach (var f in m_settings.ImportFiles)
            {
                if (!File.Exists(f.ItemSpec))
                    Log.LogWarning($"ImportFile {f} does not exists.");

                validFiles.Add(f);
            }

            var res = CreateCache(validFiles);
            if (m_settings.UseCache && CheckCache(fileName, ref res))
            {
                m_settings.Cache = res;
                Log.LogMessage(MessageImportance.High, $"| {m_settings.SectionSymbol} Adding Imports.");
                this.AddImports(m_settings.Cache.NewItems, ref project);

                //Log.LogMessage(MessageImportance.High, $"| {m_settings.SectionSymbol} Error Could not check cache.");
                //return false;
            }
            else
                this.AddImports(m_settings.Cache.CachedItems, ref project);

            var buildEngine = ((IBuildEngine6)BuildEngine);
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
            //Log.LogMessage(MessageImportance.High, $"| {m_options.SectionSymbol} Import Files: {m_options.ImportFiles}");

            var newConfig = cloneMethod?.Invoke(requestConfig, new object[] { int.MaxValue });
            var projectProp = newConfig.GetType().GetProperty("Project", BindingFlags.Public | BindingFlags.Instance);
            var configIdField = newConfig.GetType().GetField("_configId", BindingFlags.NonPublic | BindingFlags.Instance);
            configIdField?.SetValue(newConfig, int.MinValue);
            projectProp?.SetValue(newConfig, project);

            try
            {
                replaceMethod?.Invoke(bm, new[] { newConfig, requestConfig });
            }
            catch (TargetInvocationException tie)
            {
                if (tie.InnerException is NullReferenceException)
                    return true;

                throw;
            }

            return true;
        }

        protected bool CheckCache(string cachePath, ref ImportCacheResult cache)
        {
            var toBeLoaded = new ConcurrentList<ImportCacheItem>();
            var newAdd = new ConcurrentList<ITaskItem>();
            var current = new ConcurrentList<ITaskItem>();
            var tmp = cache.ImportItems;

            Log.LogMessage(MessageImportance.High, $"| {m_settings.SectionSymbol} Checking cache {cachePath}");

            if (File.Exists(cachePath))
            {
                //using var wStream = File.Open(cachePath, FileMode.Open, FileAccess.Read);
                var c = File.ReadAllText(cachePath);
                Log.LogMessage(MessageImportance.High, c);
                //var bytes = Encoding.ASCII.GetBytes(c);
                //var reader = new Utf8JsonReader(bytes);
                var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.General)
                {
                    ReferenceHandler = ReferenceHandler.Preserve,
                    DefaultBufferSize = 4096,
                    NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals | JsonNumberHandling.AllowReadingFromString,
                    AllowTrailingCommas = true,
                    WriteIndented = true,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    //Converters =
                    //{
                    //    new ImportCacheItemJsonConverter()

                    //}
                };

                var imports = JsonSerializer.Deserialize(c, typeof(ImportCacheItem[]), jsonOptions) as ImportCacheItem[];
                //var imports = JsonSerializer.Deserialize(reader, typeof(ImportCacheItem),jsonOptions);
                //using var stream = File.OpenRead(cachePath);
                //using var reader = new BinaryReader(stream);
                //var contents = reader.ReadBytes((int)stream.Length);
                ////var test = reader.ReadString();
                //var imports = JsonSerializer.Deserialize<ConcurrentList<ImportCacheItem>>(contents);

                foreach (var cacheItem in tmp)
                {
                    if (imports.Any(i => i.FilePath == cacheItem.FilePath))
                    {
                        var found = imports.Where(i => i.FilePath == cacheItem.FilePath).FirstOrDefault();
                        if (found.Checksum != cacheItem.Checksum)
                        {
                            newAdd.Add(new BuildUtil.TaskItem(found.FilePath));
                        }
                        else
                        {
                            current.Add(new BuildUtil.TaskItem(cacheItem.FilePath));
                        }
                    }
                    else
                    {
                        newAdd.Add(new BuildUtil.TaskItem(cacheItem.FilePath));
                    }
                }
            }
            else
            {
                foreach (var item in tmp)
                {
                    newAdd.Add(new BuildUtil.TaskItem(item.FilePath));
                }
            }

            toBeLoaded.AddRange(tmp);
            if (toBeLoaded.Count > 0)
            {
                using var wStream = File.Open(cachePath, FileMode.Create, FileAccess.Write, FileShare.Read);
                //using var writer = new BinaryWriter(wStream);
                using var writer = new Utf8JsonWriter(wStream);
                var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.General)
                {
                    ReferenceHandler = ReferenceHandler.Preserve,
                    DefaultBufferSize = 4096,
                    NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals | JsonNumberHandling.AllowReadingFromString,
                    AllowTrailingCommas = true,
                    WriteIndented = true,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    //Converters =
                    //{
                    //    new ImportCacheItemJsonConverter()

                    //}
                };
                JsonSerializer.Serialize(writer, toBeLoaded.ToArray(), jsonOptions);
                //writer.WriteStartArray("cache");
                //foreach (var item in toBeLoaded)
                //{
                //    
                //}
                //writer.WriteEndArray();

            }

            cache = new ImportCacheResult()
            {
                ImportItems = toBeLoaded,
                CachedItems = current,
                NewItems = newAdd
            };

            return true;
        }

        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        protected ImportCacheResult CreateCache(IList<ITaskItem> validFiles)
        {
            var sums = new ConcurrentList<ImportCacheItem>();

            foreach (var file in validFiles)
            {
                if (!File.Exists(file.ItemSpec))
                    continue;

                Log.LogMessage(MessageImportance.High, $"| {m_settings.SectionSymbol} -- File: {file.ItemSpec}");
                var info = new FileInfo(file.ItemSpec);
                using var bStream = info.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
                using var copy = new BufferedStream(bStream);
                var checksum = copy.GenerateSha256Checksum();
                var project = MSBuildHelper.CreateProjectInstance(file.ItemSpec);
                var contents = project.ToProjectRootElement();
                Log.LogMessage(MessageImportance.High, $"| {m_settings.SectionSymbol} -- Checksum: {checksum}");
                sums.Add(new ImportCacheItem()
                {
                    FileName = info.Name,
                    FilePath = info.FullName,
                    DateCached = DateTime.UtcNow,
                    Checksum = checksum,
                    Contents = contents
                });
            }

            return new ImportCacheResult(sums);
        }
    }
}

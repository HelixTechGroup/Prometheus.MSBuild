using System;
using System.Buffers;
using System.Collections.Concurrent;
using BuildUtil = Microsoft.Build.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Build.Construction;
using Microsoft.Build.Execution;
using Microsoft.Build.Graph;
using Prometheus.MSBuild.Tasks.Caching;
using Prometheus.MSBuild.Tasks.Converters;
using Prometheus.MSBuild.Tasks.Extension;
using Shin.Framework.Collections.Concurrent;

namespace Prometheus.MSBuild.Tasks
{
    public class ImportFileTask : PrometheusTask
    {
        protected string m_cacheFileName = "project.prometheus.import.cache";
        protected ImportFileOptions m_options = new ImportFileOptions();

        [Required]
        public ITaskItem[] ImportFiles
        {
            get { return m_options.ImportFiles.ToArray(); }
            set { m_options.ImportFiles = value; }
        }

        public bool UseCache
        {
            get { return m_options.UseCache; }
            set { m_options.UseCache = value; }
        }

        [Output]
        public ITaskItem[] CachedImportFiles 
        {
            get { return m_options.Cache.CachedItems.ToArray(); }
        }

        public override bool Execute()
        {
            var fileName = Path.Combine(this.GetProjectInstance().GetPropertyValue("BaseIntermediateOutputPath"), m_cacheFileName);
            Log.LogMessage(MessageImportance.High, $"| {m_options.SectionSymbol} Importing Files.");
            if (m_options.ImportFiles.Count == 0)
            {
                Log.LogWarning($"ImportFiles parameter is empty.");
                return true;
            }

            //if (m_options.ImportFiles.Any(f => !File.Exists(f)))
            foreach (var f in m_options.ImportFiles)
            {
                if (File.Exists(f.ItemSpec))
                    continue;

                Log.LogWarning($"ImportFile {f} does not exists.");
            }


            var res = new ImportCacheResult()
            {
                NewItems = ImportFiles,
                CachedItems = ImportFiles
            };

            if (m_options.UseCache)
                res = CheckCache(fileName);

            m_options.Cache = res;
            var project = this.GetProjectInstance();
            Log.LogMessage(MessageImportance.High, $"| {m_options.SectionSymbol} Adding Imports.");
            this.AddImports(ref project, m_options);

            if (m_options.UseCache)
            {
                foreach (var cItem in m_options.Cache.Cache.Where(c => m_options.Cache.CachedItems.Any(i => i.ItemSpec == c.FilePath)))
                {
                    //var sReader = new StringReader(cItem.Contents);
                    //var newRoot = XmlReader.Create(sReader);
                    //var p = new Project(newRoot);
                    var p = new Project(cItem.Contents);
                    project = project.MergeProject(p);
                }
            }

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

            var newConfig = cloneMethod?.Invoke(requestConfig, new object[] {int.MaxValue});
            var projectProp = newConfig.GetType().GetProperty("Project", BindingFlags.Public | BindingFlags.Instance);
            var configIdField = newConfig.GetType().GetField("_configId", BindingFlags.NonPublic | BindingFlags.Instance);
            configIdField?.SetValue(newConfig, int.MinValue);
            projectProp?.SetValue(newConfig, project);

            try
            {
                replaceMethod?.Invoke(bm, new[] {newConfig, requestConfig});
            }
            catch (TargetInvocationException tie)
            {
                if (tie.InnerException is NullReferenceException)
                    return true;

                throw;
            }

            return true;
        }

        protected ImportCacheResult CheckCache(string cachePath)
        {
            var toBeLoaded = new ConcurrentList<ImportCacheItem>();
            var newAdd = new ConcurrentList<ITaskItem>();
            var current = new ConcurrentList<ITaskItem>();
            var tmp = CreateCache();

            Log.LogMessage(MessageImportance.High, $"| {m_options.SectionSymbol} Checking cache {cachePath}");

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

            return new ImportCacheResult()
            {
                Cache = toBeLoaded,
                CachedItems = current,
                NewItems = newAdd
            };
        }

        protected IList<ImportCacheItem> CreateCache()
        {
            var sums = new ConcurrentList<ImportCacheItem>();

            foreach (var file in ImportFiles)
            {
                if (!File.Exists(file.ItemSpec))
                    continue;

                Log.LogMessage(MessageImportance.High, $"| {m_options.SectionSymbol} -- File: {file.ItemSpec}");
                var info = new FileInfo(file.ItemSpec);
                using var bStream = info.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
                using var copy = new BufferedStream(bStream);
                var checksum = copy.GenerateSha256Checksum();
                var project = MSBuildHelper.CreateProjectInstance(file.ItemSpec);
                Log.LogMessage(MessageImportance.High, $"| {m_options.SectionSymbol} -- Checksum: {checksum}");
                sums.Add(new ImportCacheItem()
                {
                    FileName = info.Name,
                    FilePath = info.FullName,
                    DateCached = DateTime.UtcNow,
                    Checksum = checksum,
                    Contents = project.ToProjectRootElement()
                });
            }

            return sums;
        }
    }
}

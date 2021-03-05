using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Prometheus.MSBuild.Tasks.Caching;

namespace Prometheus.MSBuild.Tasks.Converters
{
    public class ImportCacheItemJsonConverter : JsonConverter<ImportCacheItem>
    {
        /// <inheritdoc />
        public override ImportCacheItem Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("JSON payload expected to start with StartObject token.");
            }

            var list = new ImportCacheItem();
            var startDepth = reader.CurrentDepth;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == startDepth)
                    return list;
                if (reader.TokenType == JsonTokenType.StartArray)
                {
                    reader.Read();
                    reader.Read();
                    var name = reader.GetString();
                    Console.WriteLine($"name: {name}");
                    reader.Read();
                    reader.Read();
                    var checksum = reader.GetString();
                    Console.WriteLine($"checksum: {checksum}");
                    reader.Read();
                    reader.Read();
                    var path = reader.GetString();
                    Console.WriteLine($"path: {path}");
                    reader.Read();
                    reader.Read();
                    var dateCached = reader.GetDateTime();
                    Console.WriteLine($"date: {dateCached}");
                    reader.Read();
                    reader.Read();
                    var item = new ImportCacheItem()
                    {
                        FileName = name,
                        Checksum = checksum,
                        FilePath = path,
                        DateCached = dateCached,
                    };

                    var contents = reader.GetString();
                    Console.WriteLine($"contents: {contents}");
                    //using var stringReader = new StringReader(contents);
                    //var xmlReader = XmlReader.Create(stringReader);
                    //xmlReader.ReadToFollowing("Project");
                    //var project = xmlReader.ReadString();
                    var tmpFile = $@"{Path.GetTempFileName()}";
                    Console.WriteLine($"tmpFile: {tmpFile}");
                    File.WriteAllText(tmpFile, contents);
                    item.Contents = new ProjectInstance(tmpFile).ToProjectRootElement();
                    //item.Contents = xmlReader.ReadElementContentAs(typeof(ProjectRootElement), null) as ProjectRootElement;
                    //var xmlSerializer = new XmlSerializer(typeof(Project));
                    //item.Contents = xmlSerializer.Deserialize(stringReader) as ProjectRootElement;
                    list = item;

                    //if (list != null)
                    //    throw new JsonException("Multiple lists encountered.");
                    //var eodPostions = JsonSerializer.Deserialize<ImportCacheItem>(ref reader, options);
                    //list = eodPostions;
                }
            }

            throw new JsonException(); // Truncated file or internal error

            //reader.Read();
            //reader.Read();
            //var name = reader.GetString();
            //Console.WriteLine($"name: {name}");
            //reader.Read();
            //reader.Read();
            //var checksum = reader.GetString();
            //Console.WriteLine($"checksum: {checksum}");
            //reader.Read();
            //reader.Read();
            //var path = reader.GetString();
            //Console.WriteLine($"path: {path}");
            //reader.Read();
            //reader.Read();
            //var dateCached = reader.GetDateTime();
            //Console.WriteLine($"date: {dateCached}");
            //reader.Read();
            //reader.Read();
            //var item = new ImportCacheItem()
            //{
            //    FileName = name,
            //    Checksum = checksum,
            //    FilePath = path,
            //    DateCached = dateCached,
            //};

            //var contents = reader.GetString();
            //Console.WriteLine($"contents: {contents}");
            ////using var stringReader = new StringReader(contents);
            ////var xmlReader = XmlReader.Create(stringReader);
            ////xmlReader.ReadToFollowing("Project");
            ////var project = xmlReader.ReadString();
            //var tmpFile = @$"{Path.GetTempFileName()}";
            //Console.WriteLine($"tmpFile: {tmpFile}");
            //File.WriteAllText(tmpFile, contents);
            //item.Contents = new ProjectInstance(tmpFile).ToProjectRootElement();
            ////item.Contents = xmlReader.ReadElementContentAs(typeof(ProjectRootElement), null) as ProjectRootElement;
            ////var xmlSerializer = new XmlSerializer(typeof(Project));
            ////item.Contents = xmlSerializer.Deserialize(stringReader) as ProjectRootElement;
            //return item;
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, ImportCacheItem value, JsonSerializerOptions options)
        {
            var item = value;
            //writer.WriteStartArray();
            //foreach (var item in value)
            //{
            writer.WriteStartObject();
            writer.WriteString("fileName", item.FileName);
            writer.WriteString("checksum", item.Checksum);
            writer.WriteString("filePath", item.FilePath);
            writer.WriteString("dateCached", item.DateCached);
            writer.WriteString("contents", item.Contents.RawXml);
            writer.WriteEndObject();
            //}
            //writer.WriteEndArray();
        }
    }
}

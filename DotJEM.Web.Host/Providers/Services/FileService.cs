using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using System.Web.Http.Controllers;
using DotJEM.Json.Index;
using DotJEM.Json.Storage.Adapter;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.Services
{
    public interface IFileService
    {
        IEnumerable<FileHeader> Header(string contentType, int skip = 0, int take = 20);
        FileHeader Header(Guid id, string contentType);
        
        FileObject Get(Guid id, string contentType);
        FileObject Get(string name, string contentType);

        FileHeader Post(string contentType, FileObject file);
        FileHeader Put(Guid id, string contentType, FileObject file);

        FileHeader Delete(Guid id, string contentType);
    }

    public class FileHeader
    {
        public string Name { get; protected set; }
        public string Type { get; protected set; }
        public int Length { get; protected set; }

        protected FileHeader()
        {
            
        }

        public FileHeader(JObject json)
            : this(json["name"].ToObject<string>(), json["type"].ToObject<string>(), json["length"].ToObject<int>())
        {
        }

        public FileHeader(string name, string type, int length)
        {
            Length = length;
            Name = name;
            Type = type;
        }
    }

    public class FileObject : FileHeader
    {
        public byte[] Data { get; private set; }

        [JsonIgnore]
        public FileHeader Header
        {
            get
            {
                return new FileHeader(Name, Type, Length);
            }
        }

        public FileObject(HttpRequestMessage request)
        {
            if (!request.Content.IsMimeMultipartContent())
            {
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
            }

            //NOTE: MultipartFormDataStreamProvider might be more appropriate as it is specially suited for file uploads.
            //      but in that case we need to store the files locally, read them into memmory and store them in the database.
            MultipartMemoryStreamProvider provider = request.Content.ReadAsMultipartAsync(new MultipartMemoryStreamProvider()).Result;
            if (provider.Contents.Count != 1)
                throw new InvalidDataException("Expected content to have data, but it was empty.");

            HttpContent content = provider.Contents.Single();
            if (!content.Headers.ContentLength.HasValue)
                throw new InvalidDataException("Expected header to have content-lenght set, but it was not.");

            Type = content.Headers.ContentType.MediaType;
            Length = (int) content.Headers.ContentLength.Value;
            Name = content.Headers.ContentDisposition.FileName.Trim('"');
            Data = ReadAllBytes(content.ReadAsStreamAsync().Result, Length);
        }

        private static byte[] ReadAllBytes(Stream stream, int length)
        {
            using (var reader = new BinaryReader(stream))
                return reader.ReadBytes(length);
        }

        public FileObject(string name, string type, byte[] data)
            : base(name, type, data.Length)
        {
            Data = data;
        }
    }

    public static class FileObjectExtentions
    {
        public static HttpResponseMessage ToResponseMessage(this FileObject self)
        {
            var response = new HttpResponseMessage();
            response.Content = new ByteArrayContent(self.Data);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue(self.Type);
            response.StatusCode = HttpStatusCode.OK;
            response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("file") { FileName = self.Name };
            return response;
        }

        public static JObject ToJson(this FileObject self)
        {
            return JObject.FromObject(self);
        }

        public static JObject ToJson(this FileHeader self)
        {
            return JObject.FromObject(self);
        }
    }

    public class FileService : IFileService
    {
        private readonly IStorageIndex index;
        private readonly IStorageArea area;

        public FileService(IStorageIndex index, IStorageArea area)
        {
            this.index = index;
            this.area = area;
        }

        public IEnumerable<FileHeader> Header(string contentType, int skip = 0, int take = 20)
        {
            //TODO: Paging and other neat stuff...
            return area
                .Get(contentType)
                .Skip(skip)
                .Take(take)
                .Select(json => new FileHeader(json));
        }

        public FileHeader Header(Guid id, string contentType)
        {
            JObject entity = area.Get(id);
            if (entity == null)
                throw new FileNotFoundException();

            return new FileHeader(entity);
        }

        public FileObject Get(Guid id, string contentType)
        {
            dynamic entity = area.Get(id);
            if (entity == null)
                throw new FileNotFoundException();

            return new FileObject((string)entity.name, (string)entity.type, Convert.FromBase64String((string)entity.data));
        }

        public FileObject Get(string name, string contentType)
        {
            return null;
        }

        public FileHeader Post(string contentType, FileObject file)
        {
            JObject entity = area.Insert(contentType, file.ToJson());

            FileHeader header = new FileHeader(entity);
            index.Write(header.ToJson());
            return header;
        }

        public FileHeader Put(Guid id, string contentType, FileObject file)
        {
            JObject entity = area.Update(id, file.ToJson());

            FileHeader header = new FileHeader(entity);
            index.Write(header.ToJson());
            return header;
        }

        public FileHeader Delete(Guid id, string contentType)
        {
            JObject deleted = area.Delete(id);
            if (deleted == null)
                throw new FileNotFoundException();

            FileHeader header = new FileHeader(deleted);
            index.Delete(header.ToJson());
            return header;
        }
    }
}
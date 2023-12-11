using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using DotJEM.Json.Index2;
using DotJEM.Json.Storage.Adapter;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.Services;

public interface IFileService
{
    IEnumerable<FileHeader> Header(string contentType, int skip = 0, int take = 20);
    FileHeader Header(Guid id, string contentType);

    //HttpResponseMessage -> FileResponse Get(Guid id, string contentType);
    FileObject Get(Guid id, string contentType);
    //HttpResponseMessage -> FileResponse Get(string name, string contentType);
    FileObject Get(string name, string contentType);

    FileHeader Post(string contentType, FileObject file);
    FileHeader Put(Guid id, string contentType, FileObject file);

    FileHeader Delete(Guid id, string contentType);
}

//TODO: this is garbage... converting back and forth is redicilous...
public class FileHeader
{
    private readonly JObject entity;

    [JsonIgnore]
    public JObject Entity
    {
        get
        {
            JObject clone = (JObject) entity.DeepClone();
            clone.Remove("data");
            return clone;
        }
    }

    [JsonProperty(PropertyName = "id")]
    public Guid Id { get; protected set; }

    //TODO: these should just be accessors to the underlying JSON.
    [JsonProperty(PropertyName = "name")]
    public string Name { get; protected set; }

    [JsonProperty(PropertyName = "contentType")]
    public string ContentType { get; set; }

    [JsonProperty(PropertyName = "mediaType")]
    public string MediaType { get; protected set; }

    [JsonProperty(PropertyName = "length")]
    public int Length { get; protected set; }

    protected FileHeader()
    {
    }

    public FileHeader(JObject entity)
        : this(
            entity["name"].ToObject<string>(),
            entity["contentType"].ToObject<string>(),
            entity["mediaType"].ToObject<string>(), 
            entity["length"].ToObject<int>())
    {
        this.entity = entity;
        this.Id = entity["id"].ToObject<Guid>();
    }

    public FileHeader(string name, string contentType, string type, int length)
    {
        Length = length;
        Name = name;
        ContentType = contentType;
        MediaType = type;
    }
}

public class FileObject : FileHeader
{
    [JsonProperty(PropertyName = "data")]
    public byte[] Data { get; private set; }

    [JsonIgnore]
    public FileHeader Header { get { return new FileHeader(Name, ContentType, MediaType, Length); } }

    public FileObject(string name, string contentType, string type, byte[] data)
        : base(name, contentType, type, data.Length)
    {
        Data = data;
    }

    public FileObject(JObject entity) 
        : base(entity)
    {
        Data = Convert.FromBase64String((string)entity["data"]);
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

        MediaType = content.Headers.ContentType.MediaType;
        Length = (int) content.Headers.ContentLength.Value;
        Name = content.Headers.ContentDisposition.FileName.Trim('"');
        Data = ReadAllBytes(content.ReadAsStreamAsync().Result, Length);
    }

    private static byte[] ReadAllBytes(Stream stream, int length)
    {
        using (var reader = new BinaryReader(stream))
            return reader.ReadBytes(length);
    }
}

public static class FileObjectExtentions
{
    public static HttpResponseMessage ToResponseMessage(this FileObject self)
    {
        var response = new HttpResponseMessage();
        response.Content = new ByteArrayContent(self.Data);
        response.Content.Headers.ContentType = new MediaTypeHeaderValue(self.MediaType);
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
    private readonly IJsonIndex index;
    private readonly IStorageArea area;

    public FileService(IJsonIndex index, IStorageArea area)
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
        JObject entity = area.Get(id);
        if (entity == null)
            throw new FileNotFoundException();

        return new FileObject(entity);
    }

    public FileObject Get(string name, string contentType)
    {
        //TODO: utilize search to find the file with the given name.

        return null;
    }

    public FileHeader Post(string contentType, FileObject file)
    {
        JObject entity = area.Insert(contentType, file.ToJson());

        FileHeader header = new FileHeader(entity); 
        index.Update(header.ToJson());
        return header;
    }

    public FileHeader Put(Guid id, string contentType, FileObject file)
    {
        JObject entity = area.Update(id, file.ToJson());

        FileHeader header = new FileHeader(entity);
        index.Update(header.ToJson());
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
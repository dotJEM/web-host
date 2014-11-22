using System;
using System.Net.Http;
using DotJEM.Json.Index;
using DotJEM.Json.Storage;
using DotJEM.Json.Storage.Adapter;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.Services
{
    public interface IFileService
    {
        JObject Header(string contentType, int skip = 0, int take = 20);
        JObject Header(string contentType, Guid id);

        HttpResponseMessage Get(string contentType, Guid id);

        JObject Post(string contentType, JObject entity);
        JObject Put(string contentType, JObject entity);

        JObject Delete(string contentType, Guid id);

        //// FILE CONTROLLER
        //protected IStorageIndex Index { get; private set; }
        //protected IStorageArea Area { get; private set; }
        //protected IStorageContext Storage { get; private set; }

        //protected AbstractFileController(IStorageContext storage, IStorageIndex index, string storageArea)
        //{
        //    Index = index;
        //    Storage = storage;
        //    Area = storage.Area(storageArea);
        //}

        //[HttpGet]
        //public virtual dynamic Get([FromUri]string contentType, [FromUri]int skip = 0, [FromUri]int take = 20)
        //{
        //    //TODO: Paging and other neat stuff...
        //    return Area
        //        .Get(contentType)
        //        .Skip(skip)
        //        .Take(take)
        //        .Select(TrimData);
        //}

        //[HttpGet]
        //public virtual dynamic Get([FromUri]Guid id, [FromUri]string contentType, [FromUri]string head = null)
        //{
        //    dynamic entity = Area.Get(id);
        //    if (entity == null)
        //        return Request.CreateResponse(HttpStatusCode.NotFound, "Could not find cotent of type '" + contentType + "' with id [" + id + "] in area '" + Area.Name + "'");

        //    if (head != null)
        //        return TrimData(entity);

        //    var response = new HttpResponseMessage();
        //    response.Content = new ByteArrayContent(Convert.FromBase64String((string)entity.data));
        //    response.Content.Headers.ContentType = new MediaTypeHeaderValue((string)entity.type);
        //    response.StatusCode = HttpStatusCode.OK;
        //    response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("file") { FileName = (string)entity.name };
        //    return response;
        //}

        //[HttpPost]
        //public virtual dynamic Post([FromUri]string contentType)
        //{
        //    JObject entity = CreateFileObject();
        //    entity = Area.Insert(contentType, entity);

        //    //Note: We trim data for files before we add them to the index.
        //    //      as we don't wan't to use extra space on the raw data when we have to fetch the actual file here anyways.
        //    entity = TrimData(entity);
        //    Index.Write(entity);
        //    return entity;
        //}

        //[HttpPut]
        //public virtual dynamic Put([FromUri]Guid id, [FromUri]string contentType)
        //{
        //    JObject entity = CreateFileObject();
        //    entity = Area.Update(id, contentType, entity);

        //    //Note: We trim data for files before we add them to the index.
        //    //      as we don't wan't to use extra space on the raw data when we have to fetch the actual file here anyways.
        //    entity = TrimData(entity);
        //    Index.Write(entity);
        //    return entity;
        //}

        //[HttpDelete]
        //public virtual dynamic Delete([FromUri]Guid id)
        //{
        //    JObject deleted = Area.Delete(id);
        //    if (deleted == null)
        //        return Request.CreateResponse(HttpStatusCode.NotFound, "Could not delete cotent with id [" + id + "] in area '" + Area.Name + "' as it could not be found.");

        //    Index.Delete(deleted);
        //    return TrimData(deleted);
        //}

        //protected static JObject TrimData(JObject json)
        //{
        //    json.Remove("data");
        //    return json;
        //}

        //protected JObject CreateFileObject()
        //{
        //    if (!Request.Content.IsMimeMultipartContent())
        //    {
        //        throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
        //    }
        //    //NOTE: MultipartFormDataStreamProvider might be more appropriate as it is specially suited for file uploads.
        //    //      but in that case we need to store the files locally, read them into memmory and store them in the database.
        //    MultipartMemoryStreamProvider provider = Request.Content.ReadAsMultipartAsync(new MultipartMemoryStreamProvider()).Result;
        //    if (provider.Contents.Count != 1)
        //        throw new InvalidDataException();

        //    HttpContent content = provider.Contents.Single();

        //    if (!content.Headers.ContentLength.HasValue)
        //        throw new InvalidDataException();

        //    int lenght = (int)content.Headers.ContentLength;

        //    dynamic file = new JObject();
        //    file.lenght = lenght;
        //    file.type = content.Headers.ContentType.MediaType;
        //    file.name = content.Headers.ContentDisposition.FileName.Trim('"');
        //    file.data = Convert.ToBase64String(ReadAllBytes(content.ReadAsStreamAsync().Result, lenght));
        //    return file;
        //}

        //private static byte[] ReadAllBytes(Stream stream, int length)
        //{
        //    using (var reader = new BinaryReader(stream))
        //        return reader.ReadBytes(length);
        //}
    }

    public class FileServiceProvider : ServiceProvider<IFileService>
    {
        public FileServiceProvider(IStorageContext storage, IStorageIndex index)
            : base(name => new FileService(index, storage.Area(name)))
        {
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

        public JObject Header(string contentType, int skip = 0, int take = 20)
        {
            throw new NotImplementedException();
        }

        public JObject Header(string contentType, Guid id)
        {
            throw new NotImplementedException();
        }

        public HttpResponseMessage Get(string contentType, Guid id)
        {
            throw new NotImplementedException();
        }

        public JObject Post(string contentType, JObject entity)
        {
            throw new NotImplementedException();
        }

        public JObject Put(string contentType, JObject entity)
        {
            throw new NotImplementedException();
        }

        public JObject Delete(string contentType, Guid id)
        {
            throw new NotImplementedException();
        }

        protected static JObject TrimData(JObject json)
        {
            json.Remove("data");
            return json;
        }

        //protected JObject CreateFileObject()
        //{
        //    if (!Request.Content.IsMimeMultipartContent())
        //    {
        //        throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
        //    }
        //    //NOTE: MultipartFormDataStreamProvider might be more appropriate as it is specially suited for file uploads.
        //    //      but in that case we need to store the files locally, read them into memmory and store them in the database.
        //    MultipartMemoryStreamProvider provider = Request.Content.ReadAsMultipartAsync(new MultipartMemoryStreamProvider()).Result;
        //    if (provider.Contents.Count != 1)
        //        throw new InvalidDataException();

        //    HttpContent content = provider.Contents.Single();

        //    if (!content.Headers.ContentLength.HasValue)
        //        throw new InvalidDataException();

        //    int lenght = (int)content.Headers.ContentLength;

        //    dynamic file = new JObject();
        //    file.lenght = lenght;
        //    file.type = content.Headers.ContentType.MediaType;
        //    file.name = content.Headers.ContentDisposition.FileName.Trim('"');
        //    file.data = Convert.ToBase64String(ReadAllBytes(content.ReadAsStreamAsync().Result, lenght));
        //    return file;
        //}

        //private static byte[] ReadAllBytes(Stream stream, int length)
        //{
        //    using (var reader = new BinaryReader(stream))
        //        return reader.ReadBytes(length);
        //}
    }
}
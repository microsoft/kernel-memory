// Copyright (c) Microsoft. All rights reserved.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.KernelMemory.ContentStorage;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace Microsoft.KernelMemory.MongoDbAtlas;

public class MongoDbAtlasStorage : MongoDbAtlasBaseStorage, IContentStorage
{
    public MongoDbAtlasStorage(MongoDbAtlasConfig config) : base(config)
    {
    }

    public Task CreateIndexDirectoryAsync(string index, CancellationToken cancellationToken = new CancellationToken())
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task DeleteIndexDirectoryAsync(string index, CancellationToken cancellationToken = new CancellationToken())
    {
        // get the bucket related to this index ant then drop it.
        var bucket = this.GetBucketForIndex(index);
        await bucket.DropAsync(cancellationToken).ConfigureAwait(false);

        await this.Database.DropCollectionAsync(index, cancellationToken).ConfigureAwait(false);
    }

    public async Task EmptyDocumentDirectoryAsync(string index, string documentId,
        CancellationToken cancellationToken = new CancellationToken())
    {
        // delete all document in GridFS that have index as metadata
        var bucket = this.GetBucketForIndex(index);
        var filter = Builders<GridFSFileInfo<string>>.Filter.Eq("metadata.documentId", documentId);

        // load all id then delete all id
        var files = await bucket.FindAsync(filter, cancellationToken: cancellationToken).ConfigureAwait(false);
        var ids = await files.ToListAsync(cancellationToken).ConfigureAwait(false);
        foreach (var id in ids)
        {
            await bucket.DeleteAsync(id.Id, cancellationToken).ConfigureAwait(false);
        }

        // delete all document in mongodb that have index as metadata
        var collection = this.GetCollection(index);
        var filter2 = Builders<BsonDocument>.Filter.Eq("documentId", documentId);
        await collection.DeleteManyAsync(filter2, cancellationToken).ConfigureAwait(false);
    }

    public Task DeleteDocumentDirectoryAsync(string index, string documentId,
        CancellationToken cancellationToken = new CancellationToken())
    {
        return this.EmptyDocumentDirectoryAsync(index, documentId, cancellationToken);
    }

    public async Task WriteFileAsync(string index, string documentId, string fileName, Stream streamContent,
        CancellationToken cancellationToken = new CancellationToken())
    {
        // txt files are extracted text, and are stored in mongodb in the collection
        // we need to come up with a unique id for the document
        var id = $"{documentId}/{fileName}";
        var extension = Path.GetExtension(fileName);
        if (extension == ".txt")
        {
            using var reader = new StreamReader(streamContent);
            var doc = new BsonDocument()
            {
                { "_id", id },
                { "documentId", documentId },
                { "fileName", fileName },
                { "content", new BsonString(await reader.ReadToEndAsync().ConfigureAwait(false)) }
            };
            await this.SaveDocumentAsync(index, id, doc, cancellationToken).ConfigureAwait(false);
        }
        else if (extension == ".text_embedding")
        {
            //ok the file is a text embedding formatted as json, I'd like to save parsing the document.
            //saving everything in the content field not as string but as json.
            using var reader = new StreamReader(streamContent);
            var content = await reader.ReadToEndAsync().ConfigureAwait(false);
            // now deserialize the json
            var doc = BsonDocument.Parse(content);
            doc["_id"] = id;
            doc["documentId"] = documentId;
            doc["fileName"] = fileName;
            doc["content"] = content;
            await this.SaveDocumentAsync(index, id, doc, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            GridFSBucket<string> bucket = this.GetBucketForIndex(index);
            var options = new GridFSUploadOptions
            {
                Metadata = new BsonDocument
                {
                    { "index", index },
                    { "documentId", documentId },
                    { "fileName", fileName }
                }
            };

            // Since the pattern of usage is that you can upload a file for a document id and then update, we need to delete
            // any existing file with the same id check if the file exists and delete it
            IAsyncCursor<GridFSFileInfo<string>> existingFile = await GetFromBucketByIdAsync(id, bucket, cancellationToken).ConfigureAwait(false);
            if (existingFile.Any(cancellationToken))
            {
                await bucket.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
            }

            await bucket.UploadFromStreamAsync(id, fileName, streamContent, options, cancellationToken).ConfigureAwait(false);
        }
    }

    public Task CreateDocumentDirectoryAsync(string index, string documentId,
        CancellationToken cancellationToken = new CancellationToken())
    {
        //no need to create anything for the document
        return Task.CompletedTask;
    }

    public async Task<BinaryData> ReadFileAsync(string index, string documentId, string fileName, bool logErrIfNotFound = true,
        CancellationToken cancellationToken = new CancellationToken())
    {
        // Read from mongodb but you need to check extension to load correctly
        var extension = Path.GetExtension(fileName);
        var id = $"{documentId}/{fileName}";
        if (extension == ".txt")
        {
            var collection = this.GetCollection(index);
            var filterById = Builders<BsonDocument>.Filter.Eq("_id", id);
            var doc = await collection.Find(filterById).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            if (doc == null)
            {
                var error = $"File {fileName} not found in index {index} and document {documentId}";
                if (logErrIfNotFound)
                {
                    Console.WriteLine(error);
                }

                throw new ContentStorageFileNotFoundException(error);
            }

            return new BinaryData(doc["content"].AsString);
        }
        else if (extension == ".text_embedding")
        {
            var collection = this.GetCollection(index);
            var filterById = Builders<BsonDocument>.Filter.Eq("_id", id);
            var doc = await collection.Find(filterById).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            if (doc == null)
            {
                if (logErrIfNotFound)
                {
                    Console.WriteLine($"File {fileName} not found in index {index} and document {documentId}");
                }

                throw new ContentStorageFileNotFoundException("File not found");
            }

            return new BinaryData(doc["content"].AsString);
        }
        else
        {
            var bucket = this.GetBucketForIndex(index);
            var filter = Builders<GridFSFileInfo<string>>.Filter.Eq(x => x.Id, id);

            var files = await bucket.FindAsync(filter, cancellationToken: cancellationToken).ConfigureAwait(false);
            var file = await files.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            if (file == null)
            {
                if (logErrIfNotFound)
                {
                    Console.WriteLine($"File {fileName} not found in index {index} and document {documentId}");
                }

                throw new ContentStorageFileNotFoundException("File not found");
            }

            using var stream = await bucket.OpenDownloadStreamAsync(file.Id, cancellationToken: cancellationToken).ConfigureAwait(false);
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);
            memoryStream.Position = 0;
            return new BinaryData(memoryStream.ToArray());
        }
    }

    private async Task SaveDocumentAsync(string index, string id, BsonDocument doc, CancellationToken cancellationToken)
    {
        var collection = this.GetCollection(index);

        //upsert the doc based on the id
        await collection.ReplaceOneAsync(
            Builders<BsonDocument>.Filter.Eq("_id", id),
            doc,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken
        ).ConfigureAwait(false);
    }

    private static async Task<IAsyncCursor<GridFSFileInfo<string>>> GetFromBucketByIdAsync(string id, GridFSBucket<string> bucket, CancellationToken cancellationToken)
    {
        return await bucket.FindAsync(GetBucketFilterById(id), cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private static FilterDefinition<GridFSFileInfo<string>> GetBucketFilterById(string id)
    {
        return Builders<GridFSFileInfo<string>>.Filter.Eq(x => x.Id, id);
    }
}

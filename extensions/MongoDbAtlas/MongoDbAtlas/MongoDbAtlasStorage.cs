// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.KernelMemory.DocumentStorage;
using Microsoft.KernelMemory.Pipeline;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace Microsoft.KernelMemory.MongoDbAtlas;

[Experimental("KMEXP03")]
public sealed class MongoDbAtlasStorage : MongoDbAtlasBaseStorage, IDocumentStorage
{
    private readonly IMimeTypeDetection _mimeTypeDetection;

    public MongoDbAtlasStorage(
        MongoDbAtlasConfig config,
        IMimeTypeDetection? mimeTypeDetection = null) : base(config)
    {
        this._mimeTypeDetection = mimeTypeDetection ?? new MimeTypesDetection();
    }

    public Task CreateIndexDirectoryAsync(string index, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task DeleteIndexDirectoryAsync(string index, CancellationToken cancellationToken = default)
    {
        // get the bucket related to this index ant then drop it.
        var bucket = this.GetBucketForIndex(index);
        await bucket.DropAsync(cancellationToken).ConfigureAwait(false);

        await this.Database.DropCollectionAsync(index, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task EmptyDocumentDirectoryAsync(
        string index, string documentId, CancellationToken cancellationToken = default)
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

    /// <inheritdoc />
    public Task DeleteDocumentDirectoryAsync(string index, string documentId, CancellationToken cancellationToken = default)
    {
        return this.EmptyDocumentDirectoryAsync(index, documentId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task WriteFileAsync(
        string index, string documentId, string fileName, Stream streamContent, CancellationToken cancellationToken = default)
    {
        // txt files are extracted text, and are stored in mongodb in the collection
        // we need to come up with a unique id for the document
        var id = $"{documentId}/{fileName}";
        var extension = Path.GetExtension(fileName);
        if (extension == ".txt")
        {
            using var reader = new StreamReader(streamContent);
            var doc = new BsonDocument
            {
                { "_id", id },
                { "documentId", documentId },
                { "fileName", fileName },
                { "content", new BsonString(await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false)) },
                { "contentType", MimeTypes.PlainText }
            };
            await this.SaveDocumentAsync(index, id, doc, cancellationToken).ConfigureAwait(false);
        }
        else if (extension == ".text_embedding")
        {
            //ok the file is a text embedding formatted as json, I'd like to save parsing the document.
            //saving everything in the content field not as string but as json.
            using var reader = new StreamReader(streamContent);
            var content = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            // now deserialize the json
            var doc = BsonDocument.Parse(content);
            doc["_id"] = id;
            doc["documentId"] = documentId;
            doc["fileName"] = fileName;
            doc["content"] = content;
            doc["contentType"] = MimeTypes.PlainText;
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
                    { "fileName", fileName },
                    { "contentType", this._mimeTypeDetection.GetFileType(fileName) }
                }
            };

            // Since the pattern of usage is that you can upload a file for a document id and then update, we need to delete
            // any existing file with the same id check if the file exists and delete it
            IAsyncCursor<GridFSFileInfo<string>> existingFile = await GetFromBucketByIdAsync(id, bucket, cancellationToken).ConfigureAwait(false);
            if (await existingFile.AnyAsync(cancellationToken).ConfigureAwait(false))
            {
                await bucket.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
            }

            await bucket.UploadFromStreamAsync(id, fileName, streamContent, options, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public Task CreateDocumentDirectoryAsync(string index, string documentId, CancellationToken cancellationToken = default)
    {
        //no need to create anything for the document
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<StreamableFileContent> ReadFileAsync(
        string index, string documentId, string fileName, bool logErrIfNotFound = true, CancellationToken cancellationToken = default)
    {
        // IMPORTANT: documentId can be empty, e.g. when deleting an index
        ArgumentNullExceptionEx.ThrowIfNullOrEmpty(index, nameof(index), "Index name is empty");
        ArgumentNullExceptionEx.ThrowIfNullOrEmpty(fileName, nameof(fileName), "Filename is empty");

        // Read from mongodb but you need to check extension to load correctly
        var extension = Path.GetExtension(fileName);
        var id = $"{documentId}/{fileName}";

        // TODO: fix code duplication and inconsistencies of file timestamp
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

                throw new DocumentStorageFileNotFoundException(error);
            }

            BinaryData docData = new(doc["content"].AsString);
            Task<Stream> AsyncStreamDelegate() => Task.FromResult(docData.ToStream());
            StreamableFileContent file = new(
                fileName,
                docData.Length,
                doc["contentType"].AsString,
                DateTimeOffset.UtcNow,
                AsyncStreamDelegate);
            return file;
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

                throw new DocumentStorageFileNotFoundException("File not found");
            }

            BinaryData docData = new(doc["content"].AsString);
            Task<Stream> AsyncStreamDelegate() => Task.FromResult(docData.ToStream());
            StreamableFileContent file = new(
                fileName,
                docData.Length,
                doc["contentType"].AsString,
                DateTimeOffset.UtcNow,
                AsyncStreamDelegate);
            return file;
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

                throw new DocumentStorageFileNotFoundException("File not found");
            }

            async Task<Stream> AsyncStreamDelegate() => await bucket.OpenDownloadStreamAsync(file.Id, cancellationToken: cancellationToken).ConfigureAwait(false);

            StreamableFileContent streamableFile = new(
                file.Filename,
                file.Length,
                file.Metadata["contentType"].AsString,
                file.UploadDateTime,
                AsyncStreamDelegate);
            return streamableFile;
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

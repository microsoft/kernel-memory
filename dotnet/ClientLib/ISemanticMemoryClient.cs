// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.SemanticMemory.Client;

public interface ISemanticMemoryClient
{
    public Task<string> ImportFileAsync(Document file);
    public Task<IList<string>> ImportFilesAsync(Document[] files);
    public Task<string> ImportFileAsync(string fileName);
    public Task<string> ImportFileAsync(string fileName, DocumentDetails details);
    public Task<string> AskAsync(string question, string userId);
}

// Copyright (c) Microsoft. All rights reserved.

using System;
using System.IO;
using MsgReader.Outlook;

namespace Microsoft.KernelMemory.DataFormats.Office;

public class MsOutlookDecoder
{
    public string DocToText(string filename)
    {
        using var stream = File.OpenRead(filename);
        return this.DocToText(stream);
    }

    public string DocToText(BinaryData data)
    {
        using var stream = data.ToStream();
        return this.DocToText(stream);
    }

    public string DocToText(Stream data)
    {
        using var message = new Storage.Message(data);
        var body = message.BodyText.Trim();

        return body;
    }
}

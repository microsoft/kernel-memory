// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.Prompts;

namespace Microsoft.KM.Core.UnitTests.Prompts;

public class PromptUtilsTest
{
    [Fact]
    [Trait("Category", "UnitTest")]
    public void ItRendersSimpleFactTemplates()
    {
        Assert.Equal("", PromptUtils.RenderFactTemplate(template: "", factContent: ""));
        Assert.Equal("x", PromptUtils.RenderFactTemplate(template: "x", factContent: "text"));
        Assert.Equal("text", PromptUtils.RenderFactTemplate(template: "{{$content}}", factContent: "text"));
        Assert.Equal("\ntext\n", PromptUtils.RenderFactTemplate(template: "\n{{$content}}\n", factContent: "text"));
        Assert.Equal("text--", PromptUtils.RenderFactTemplate(template: "{{$content}}-{{$relevance}}-{{$memoryId}}", factContent: "text"));
        Assert.Equal("text-0.23-id0", PromptUtils.RenderFactTemplate(template: "{{$content}}-{{$relevance}}-{{$memoryId}}", factContent: "text", source: "src", relevance: "0.23", recordId: "id0"));
        Assert.Equal("==== [File:src;Relevance:0.23]:\ntext", PromptUtils.RenderFactTemplate(template: "==== [File:{{$source}};Relevance:{{$relevance}}]:\n{{$content}}", factContent: "text", source: "src", relevance: "0.23", recordId: "id0"));
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    public void ItRendersFactTemplatesWithTags()
    {
        Assert.Equal("text; Foo:-", PromptUtils.RenderFactTemplate(template: "{{$content}}; Foo:{{$tags[foo]}}", factContent: "text"));

        var tags = new TagCollection { "foo" };
        Assert.Equal("text; Foo:-", PromptUtils.RenderFactTemplate(template: "{{$content}}; Foo:{{$tags[foo]}}", factContent: "text", tags: tags));

        tags = new TagCollection { { "foo", "bar" } };
        Assert.Equal("text; Foo:bar", PromptUtils.RenderFactTemplate(template: "{{$content}}; Foo:{{$tags[foo]}}", factContent: "text", tags: tags));

        tags = new TagCollection { { "foo", ["bar", "baz"] } };
        Assert.Equal("text; Foo:[bar, baz]", PromptUtils.RenderFactTemplate(template: "{{$content}}; Foo:{{$tags[foo]}}", factContent: "text", tags: tags));

        Assert.Equal("text; Tags:", PromptUtils.RenderFactTemplate(template: "{{$content}}; Tags:{{$tags}}", factContent: "text"));

        tags = new TagCollection { { "foo", ["bar", "baz"] } };
        Assert.Equal("text; Tags=foo:[bar, baz]", PromptUtils.RenderFactTemplate(template: "{{$content}}; Tags={{$tags}}", factContent: "text", tags: tags));

        tags = new TagCollection { { "foo", ["bar", "baz"] }, { "car", ["red", "pink"] } };
        Assert.Equal("text; Tags=foo:[bar, baz];car:[red, pink]", PromptUtils.RenderFactTemplate(template: "{{$content}}; Tags={{$tags}}", factContent: "text", tags: tags));
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    public void ItRendersFactTemplatesWithMetadata()
    {
        Assert.Equal("text; Foo:-", PromptUtils.RenderFactTemplate(template: "{{$content}}; Foo:{{$meta[foo]}}", factContent: "text"));
        Assert.Equal("text; Foo:-", PromptUtils.RenderFactTemplate(template: "{{$content}}; Foo:{{$meta[foo]}}", factContent: "text", metadata: []));
        Assert.Equal("text; Foo:bar", PromptUtils.RenderFactTemplate(template: "{{$content}}; Foo:{{$meta[foo]}}", factContent: "text", metadata: new Dictionary<string, object> { { "foo", "bar" } }));
        Assert.Equal("text; Foo:bar", PromptUtils.RenderFactTemplate(template: "{{$content}}; Foo:{{$meta[foo]}}", factContent: "text", metadata: new Dictionary<string, object> { { "foo", "bar" }, { "car", "red" } }));
        Assert.Equal("text; Foo:bar; Car:red", PromptUtils.RenderFactTemplate(template: "{{$content}}; Foo:{{$meta[foo]}}; Car:{{$meta[car]}}", factContent: "text", metadata: new Dictionary<string, object> { { "foo", "bar" }, { "car", "red" } }));
    }
}

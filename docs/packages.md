---
nav_order: 91
has_children: false
title: Packages
permalink: /packages
layout: default
---
# .NET packages

* **Microsoft.KernelMemory.WebClient:** .NET web client to call a running instance of Kernel Memory web service.

  [![Nuget package](https://img.shields.io/nuget/vpre/Microsoft.KernelMemory.WebClient)](https://www.nuget.org/packages/Microsoft.KernelMemory.WebClient/)
  [![Example code](https://img.shields.io/badge/example-code-blue)](https://github.com/microsoft/kernel-memory/tree/main/examples/001-dotnet-WebClient)

* **Microsoft.KernelMemory.Core:** Kernel Memory core library including all extensions, can be used to build custom
  pipelines and handlers, contains
  also the serverless client to use memory in a synchronous way without the web service.

  [![Nuget package](https://img.shields.io/nuget/vpre/Microsoft.KernelMemory.Core)](https://www.nuget.org/packages/Microsoft.KernelMemory.Core/)
  [![Example code](https://img.shields.io/badge/example-code-blue)](https://github.com/microsoft/kernel-memory/tree/main/examples/002-dotnet-Serverless)

* **Microsoft.KernelMemory.Service.AspNetCore:** an extension to load Kernel Memory into your ASP.NET apps.

  [![Nuget package](https://img.shields.io/nuget/vpre/Microsoft.KernelMemory.Service.AspNetCore)](https://www.nuget.org/packages/Microsoft.KernelMemory.Service.AspNetCore/)
  [![Example code](https://img.shields.io/badge/example-code-blue)](https://github.com/microsoft/kernel-memory/tree/main/examples/204-dotnet-ASP.NET-MVC-integration)

* **Microsoft.KernelMemory.SemanticKernelPlugin:** a Memory plugin for Semantic Kernel,
  replacing the original Semantic Memory available in SK.

  [![Nuget package](https://img.shields.io/nuget/vpre/Microsoft.KernelMemory.SemanticKernelPlugin)](https://www.nuget.org/packages/Microsoft.KernelMemory.SemanticKernelPlugin/)
  [![Example code](https://img.shields.io/badge/example-code-blue)](https://github.com/microsoft/kernel-memory/tree/main/examples/003-dotnet-SemanticKernel-plugin)

### Packages for Python, Java and other languages

Kernel Memory service offers a **Web API** out of the box, including the **OpenAPI
swagger** documentation that you can leverage to test the API and create custom
web clients. For instance, after starting the service locally, see http://127.0.0.1:9001/swagger/index.html.

A python package with a Web Client and Semantic Kernel plugin will soon be available.
We also welcome PR contributions to support more languages.

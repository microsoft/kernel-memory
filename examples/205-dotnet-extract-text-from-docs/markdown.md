Kernel Memory
=============

This repository presents best practices and a reference implementation for Memory in specific AI
and LLMs application scenarios. Please note that **the code provided serves as a demonstration**
and is **not an officially supported** Microsoft offering.

**Kernel Memory** (KM) is a **multi-modal [AI Service](service/Service/README.md)** specialized
in the efficient indexing of datasets through custom continuous data hybrid pipelines, with support
for **[Retrieval Augmented Generation](https://en.wikipedia.org/wiki/Prompt_engineering#Retrieval-augmented_generation)**
(RAG), synthetic memory, prompt engineering, and custom semantic memory processing.

KM is available as a **Web Service**, as a **[Docker container](https://hub.docker.com/r/kernelmemory/service)**,
a **[Plugin](https://learn.microsoft.com/copilot/plugins/overview)** for ChatGPT/Copilot/Semantic
Kernel, and as a .NET library for embedded applications.

Utilizing advanced embeddings and LLMs, the system enables Natural Language querying for obtaining
answers from the indexed data, complete with citations and links to the original sources.

Kernel Memory is designed for seamless integration as a Plugin with [Semantic Kernel](https://github.com/microsoft/semantic-kernel),
Microsoft Copilot and ChatGPT.


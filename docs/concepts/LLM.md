---
nav_order: 5
parent: Concepts
title: LLM
permalink: /concepts/llm
layout: default
---
# LLM

When setting up Kernel Memory you will have to select one or more AI provider, required
to extract meaning from documents and to generate sentences when asking questions.

AI providers like Azure offer a wide selection of Large Language Models, such as
GPT-4, Whisper, and Ada-2 that KM leverages internally to analyze data and produce
content.

Kernel Memory has been designed to work well with GPT-4, GPT 3.5 for text generation,
and Ada-2 for [semantic extraction with embeddings](/concepts/embedding). However,
you can also setup KM to use models like Phi, LLama, Mixtral, Hugging Face, etc.

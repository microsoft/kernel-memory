---
nav_order: 1
parent: Concepts
title: Index
permalink: /concepts/indexes
layout: default
---
# Index

Kernel Memory leverages vector storage to save the meaning of the documents
ingested into the service, solutions like Azure AI Search, Qdrant, Elastic Search,
Redis etc.

Typically, storage solutions offer a maximum capacity for each collection, and
often one needs to clearly separate data over distinct collections for security,
privacy or other important reasons.

In KM terms, these collection are called "indexes".

When storing information, when searching, and when asking questions, KM is always
working within the boundaries of one index. Data in one index never leaks into
other indexes.

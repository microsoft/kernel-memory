---
nav_order: 10
has_children: true
title: Service
permalink: /service
layout: default
---

# Kernel Memory service

KM service offers the best performing, most secure and scalable approach to
Memory organization and usage with RAG.

By using KM service, as opposed to the embedded serverless mode, you get these benefits:

* KM can be used from **any language**, such as Python, C#, Java, **and platforms**,
  simply by sending HTTP requests.
* KM service can be **distributed over multiple machines**, and run long ingestion
  processes without blocking your client applications. This includes the need
  to **retry in case of errors and throttling** by external services like OpenAI.
* Only the service needs **credentials** to access AI models, storage and other
  dependencies, so secrets are not exposed via client apps.
* You can run the service locally, getting all the benefits also during tests
  and development.
// Copyright (c) Microsoft. All rights reserved.

using System;

/* === 1 ===
 * Use SemanticMemoryClient to run the default import pipeline
 * in the same process, without distributed queues.
 * The pipeline might use settings in appsettings.json, but uses
 * 'InProcessPipelineOrchestrator' explicitly.
 * Note: no web service required to run this.
 */

Example1_ImportWithMemoryClient.RunAsync().Wait();

/* === 2 ===
 * Use SemanticMemoryWebClient to run the default import pipeline
 * deployed as a web service at "http://127.0.0.1:9001/".
 *
 * Note: start the web service before running this.
 * Note: if the web service uses distributed handlers, make sure
 *       handlers are running to get the pipeline to complete,
 *       otherwise the web service might just upload the files
 *       without extracting memories.
 */

Console.WriteLine("============================");
Console.WriteLine("Make sure the semantic memory web service is running and handlers are running");
Console.WriteLine("Press Enter to continue...");
Console.ReadLine();
Example2_ImportWithMemoryWebClient.RunAsync("http://127.0.0.1:9001/").Wait();

// /* === 3 ===
//  * Define a custom pipeline, 100% C# handlers, and run it in this process.
//  * Note: no web service required to run this.
//  * The pipeline might use settings in appsettings.json, but uses
//  * 'InProcessPipelineOrchestrator' explicitly.
//  */

Console.WriteLine("============================");
Console.WriteLine("Press Enter to continue...");
Console.ReadLine();
Example3_CustomInProcessPipeline.RunAsync().Wait();
Console.WriteLine("============================");

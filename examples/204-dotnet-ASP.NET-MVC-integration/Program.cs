// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.Service.AspNetCore;

var webAppBuilder = WebApplication.CreateBuilder(args);

// Add services to your ASP.NET app
webAppBuilder.Services.AddControllers();

// Add Kernel Memory to your ASP.NET app
webAppBuilder.AddKernelMemory(kmBuilder =>
    {
        // Configure Kernel Memory here if needed
        kmBuilder
            .WithOpenAIDefaults(Environment.GetEnvironmentVariable("OPENAI_API_KEY")!)
            .WithCustomImageOcr(new MyOcrEngine());
    }
);

// Build ASP.NET app
var wepApp = webAppBuilder.Build();

// Typical ASP.NET app setup
wepApp.UseHttpsRedirection();
wepApp.UseAuthorization();
wepApp.MapControllers();

// Optional: add Kernel Memory web endpoints
wepApp.AddGetIndexesEndpoint("/km/"); // GET /km/indexes

// Start ASP.NET app
wepApp.Run();

// Try GET http://localhost:5000/memory     => see MemoryController

// Try GET http://localhost:5000/km/indexes => list of KM indexes

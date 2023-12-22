// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// === Memory stuff begin ===
// * builder.Services is passed to the builder, so memory dependencies like the OCR service can be used also in our ASP.NET controllers
IKernelMemory memory = new KernelMemoryBuilder(builder.Services)
    .WithOpenAIDefaults(Env.Var("OPENAI_API_KEY"))
    .WithCustomImageOcr(new MyOcrEngine())
    .Build();

// Add the memory to the ASP.NET services so our controllers can use it
builder.Services.AddSingleton(memory);

// === Memory stuff end ===

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

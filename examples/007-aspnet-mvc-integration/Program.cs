// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticMemory;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// === Memory stuff begin ===
ISemanticMemoryClient memory = new MemoryClientBuilder()
    .WithOpenAIDefaults(Env.Var("OPENAI_API_KEY"))
    .Build();

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

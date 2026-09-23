using AI_Agent.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("ClientWebApp", policy =>
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddHttpClient<McpClientService>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["BankingServiceBaseUrl"] ?? "http://localhost:5078/");
});

builder.Services.AddHttpClient<OllamaAgentModel>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Ollama:BaseUrl"] ?? "http://localhost:11434/");
});

builder.Services.AddScoped<IAgentModel>(serviceProvider =>
    serviceProvider.GetRequiredService<OllamaAgentModel>());

builder.Services.AddScoped<AgentService>();

var app = builder.Build();

app.UseCors("ClientWebApp");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();

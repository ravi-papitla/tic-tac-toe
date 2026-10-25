using System.Text.Json.Serialization;
using TicTacToe.Api.Infrastructure;
using TicTacToe.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IGameService, GameService>();

builder.Services
    .AddControllers(options => options.Filters.Add<GameExceptionFilter>())
    .AddJsonOptions(options =>
        // Enums travel as strings ("X", "InProgress", ...); numbers are rejected.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                     ?? new[] { "http://localhost:4200", "http://127.0.0.1:4200" };

builder.Services.AddCors(options =>
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// Swagger stays on so reviewers can try the API at http://localhost:5080/swagger
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("Frontend");

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
app.MapControllers();

app.Run();

// Makes the entry point visible to WebApplicationFactory in the integration tests.
public partial class Program { }

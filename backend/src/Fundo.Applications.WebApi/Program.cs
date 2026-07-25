using Fundo.Application;
using Fundo.Applications.WebApi.Middleware;
using Fundo.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseRouting();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
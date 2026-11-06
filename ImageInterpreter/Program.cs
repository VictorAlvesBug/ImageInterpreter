using System.Security.Cryptography;
using ImageInterpreter.Authentication;
using ImageInterpreter.Problems;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

if (builder.Environment.IsDevelopment())
{
    var signingKey = new DevelopmentSigningKey(RSA.Create(2048));
    builder.Services.AddSingleton(signingKey);
    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Authentication:PublicKeyPem"] = signingKey.ExportPublicKeyPem(),
    });
}

builder.Services.AddImageInterpreterAuthentication();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}

app.UseMiddleware<NonOperationRoutesMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    DevelopmentTokenEndpoint.Map(app);
}

app.MapControllers();

app.Run();

public partial class Program
{
}

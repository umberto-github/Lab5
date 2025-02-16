using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.Net.Mail;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddOpenApi();
builder.Services.AddSingleton<UserService>();
builder.Services.AddHttpLogging(logging =>
{
    logging.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All;
});
builder.Services.AddControllers(); // Add this for controllers

var app = builder.Build();

// Use HttpLogging middleware
app.UseHttpLogging();

// Middleware for request and response logging with Serilog
app.UseSerilogRequestLogging();

// Custom middleware for authentication, authorization, and input validation
app.Use(async (context, next) => 
{
    var username = context.Request.Headers["Username"].FirstOrDefault();
    var password = context.Request.Headers["Password"].FirstOrDefault();

    if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
    {
        context.Response.StatusCode = 401; // Unauthorized
        await context.Response.WriteAsync("Missing or invalid authentication headers.");
        return;
    }

    if (username != "admin" || password != "password") // Credential verification
    {
        context.Response.StatusCode = 403; // Forbidden
        await context.Response.WriteAsync("Invalid username or password.");
        return;
    }

    var input = context.Request.Query["input"];
    if (!IsValidInput(input))
    {
        if (!context.Response.HasStarted)
        {
            context.Response.StatusCode = 404;
            await context.Response.WriteAsync("Invalid input.");
        }
        return;
    }

    await next.Invoke();
});

// Middleware for email validation
app.Use(async (context, next) =>
{
    if (context.Request.Method == HttpMethods.Post || context.Request.Method == HttpMethods.Put)
    {
        // For POST and PUT, validate email in the request body
        context.Request.EnableBuffering();
        using (var reader = new StreamReader(context.Request.Body, leaveOpen: true))
        {
            var body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;

            var email = ExtractEmailFromBody(body);
            if (!IsValidEmail(email))
            {
                Log.Logger.Information($"Invalid email format: {email}"); // Log the email for debugging
                context.Response.StatusCode = 400; // Bad Request
                await context.Response.WriteAsync("Invalid email format in the request body.");
                return;
            }
        }
    }
    else if (context.Request.Query.ContainsKey("email"))
    {
        // For GET and DELETE, validate email in the query parameters
        var email = context.Request.Query["email"].ToString();
        if (!IsValidEmail(email))
        {
            Log.Logger.Information($"Invalid email format in query: {email}"); // Log the email for debugging
            context.Response.StatusCode = 400; // Bad Request
            await context.Response.WriteAsync("Invalid email format in the query parameters.");
            return;
        }
    }

    await next.Invoke();
});

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Routing
app.UseRouting();

app.MapControllers();

app.Run();

static bool IsValidInput(string input)
{
    return string.IsNullOrEmpty(input) || input.Contains("Password");
}

static bool IsValidEmail(string email)
{
    // Check if email is null or empty
    if (string.IsNullOrEmpty(email))
    {
        return false;
    }

    // Regular expression for basic email validation
    string emailRegex = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";

    if (!Regex.IsMatch(email, emailRegex))
    {
        return false;
    }

    try
    {
        var mailAddress = new MailAddress(email);
        return true;
    }
    catch (FormatException)
    {
        return false;
    }
}

static bool IsValidEmailInBody(string body)
{
    // Assume the email is in the "email" field of the JSON
    var email = ExtractEmailFromBody(body);
    return IsValidEmail(email);
}

static string ExtractEmailFromBody(string body)
{
    try
    {
        var jsonDocument = JsonDocument.Parse(body);
        if (jsonDocument.RootElement.TryGetProperty("email", out JsonElement emailElement))
        {
            return emailElement.GetString();
        }
    }
    catch (JsonException)
    {
        // Handle JSON parsing error
    }

    return string.Empty;
}

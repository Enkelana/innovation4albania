using Innovation4Albania.Application.Interfaces;
using Innovation4Albania.Infrastructure.Services;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<IPlatformRepository, PlatformRepository>();
builder.Services.AddSingleton<IDashboardService, DashboardService>();
builder.Services.AddSingleton<IWorkspaceService, WorkspaceService>();
builder.Services.AddHttpClient<IAiWorkspaceService, AiWorkspaceService>();

var app = builder.Build();
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.Use(async (context, next) =>
{
    if (context.Request.Path.Equals("/login", StringComparison.OrdinalIgnoreCase))
    {
        context.Request.Path = "/login.html";
    }
    else if (context.Request.Path.Equals("/expert/select-ministry", StringComparison.OrdinalIgnoreCase))
    {
        context.Request.Path = "/expert/select-ministry.html";
    }
    else if (context.Request.Path.Equals("/expert/dashboard", StringComparison.OrdinalIgnoreCase))
    {
        context.Request.Path = "/dashboard.html";
    }
    else if (context.Request.Path.Equals("/director/calendar", StringComparison.OrdinalIgnoreCase) ||
             context.Request.Path.Equals("/director/okrs", StringComparison.OrdinalIgnoreCase) ||
             context.Request.Path.Equals("/director/settings", StringComparison.OrdinalIgnoreCase) ||
             context.Request.Path.Equals("/director/tasks", StringComparison.OrdinalIgnoreCase) ||
             context.Request.Path.Equals("/expert/calendar", StringComparison.OrdinalIgnoreCase) ||
             context.Request.Path.Equals("/expert/okrs", StringComparison.OrdinalIgnoreCase) ||
             context.Request.Path.Equals("/minister/calendar", StringComparison.OrdinalIgnoreCase) ||
             context.Request.Path.Equals("/notifications", StringComparison.OrdinalIgnoreCase) ||
             context.Request.Path.Equals("/director/import", StringComparison.OrdinalIgnoreCase))
    {
        context.Request.Path = "/dashboard.html";
    }

    await next();
});

app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        context.Context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
        context.Context.Response.Headers["Pragma"] = "no-cache";
        context.Context.Response.Headers["Expires"] = "0";
    }
});

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();

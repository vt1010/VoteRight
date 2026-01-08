using VoteRightWebApp.Services;
using Serilog;

// Configure Serilog for file logging with auto-delete
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .Build())
    .WriteTo.Console()
    .WriteTo.File(
        path: "Logs/app-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        fileSizeLimitBytes: 10485760,
        shared: true,
        flushToDiskInterval: TimeSpan.FromSeconds(1))
    .CreateLogger();

try
{
    Log.Information("Starting VoteRight Web Application");

    var builder = WebApplication.CreateBuilder(args);

    // Use Serilog for logging
    builder.Host.UseSerilog();

    // Register LocationDataService and caches
    builder.Services.AddMemoryCache();

    // Add services to the container.
    builder.Services.AddControllersWithViews()
        .AddJsonOptions(options =>
        {
            // Configure JSON serialization for concurrent requests
            options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        });

    // Add session support for authentication
    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromMinutes(30);
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
    });

    // No Entity Framework: database access handled via ADO.NET in DatabaseService

    // Use Postgres-backed implementation for CSV export
    
    // Remove legacy services from compilation if still present (LocalFileService/S3Service not registered)
    builder.Services.AddScoped<DatabaseService>();

    // Add response compression for faster downloads
    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
        options.MimeTypes = new[] { "text/csv", "application/json", "text/plain" };
    });

    // Configure Kestrel for better concurrent connection handling
    builder.WebHost.ConfigureKestrel(serverOptions =>
    {
        serverOptions.Limits.MaxConcurrentConnections = 100;
        serverOptions.Limits.MaxConcurrentUpgradedConnections = 100;
        serverOptions.Limits.MaxRequestBodySize = 524288000; // 500MB for large CSV files
        serverOptions.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
        serverOptions.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(1);
    });

    var app = builder.Build();

    // No EF: skip EnsureCreated; database is managed externally

    // Enable response compression
    app.UseResponseCompression();

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
           app.UseHttpsRedirection();
    }
    app.UseRouting();

    app.UseSession();
    app.UseAuthorization();

    app.MapStaticAssets();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
        .WithStaticAssets();


    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

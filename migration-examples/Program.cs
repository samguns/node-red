using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NodeRed.Runtime;
using NodeRed.Runtime.Flows;
using NodeRed.Runtime.Nodes;
using NodeRed.Runtime.Registry;
using NodeRed.Runtime.Storage;
using NodeRed.EditorApi;
using Serilog;

namespace NodeRed.CSharp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Setup Serilog
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            try
            {
                var host = CreateHostBuilder(args).Build();
                
                // Initialize runtime
                var runtime = host.Services.GetRequiredService<NodeRedRuntime>();
                await runtime.InitializeAsync(new RuntimeSettings
                {
                    UserDir = ".node-red",
                    FlowsFile = "flows.json",
                    HttpAdminRoot = "/",
                    HttpNodeRoot = "/"
                });

                await runtime.StartAsync();

                await host.RunAsync();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseUrls("http://localhost:1880");
                })
                .ConfigureServices((context, services) =>
                {
                    // Register runtime components
                    services.AddSingleton<NodeRegistry>();
                    services.AddSingleton<FlowManager>();
                    services.AddSingleton<FlowStorage>();
                    services.AddSingleton<NodeRedRuntime>();

                    // Register API controllers
                    services.AddControllers();
                    services.AddSignalR();

                    // CORS
                    services.AddCors(options =>
                    {
                        options.AddDefaultPolicy(builder =>
                        {
                            builder.AllowAnyOrigin()
                                   .AllowAnyMethod()
                                   .AllowAnyHeader();
                        });
                    });

                    // Register editor API
                    services.AddScoped<IEditorApiService, EditorApiService>();
                });
    }

    public class Startup
    {
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseCors();
            app.UseStaticFiles(); // Serve editor client

            app.UseEndpoints(endpoints =>
            {
                // Admin API endpoints
                endpoints.MapControllers();
                
                // WebSocket for real-time updates
                endpoints.MapHub<EditorHub>("/comms");
                
                // Fallback to index.html for SPA
                endpoints.MapFallbackToFile("index.html");
            });
        }
    }
}

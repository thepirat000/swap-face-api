using Audit.Core;
using Microsoft.OpenApi.Models;
using swap_faces.Helpers;
using swap_faces.Swap;
using System.Diagnostics;
using System.Text;
using System.Text.Json.Serialization;

namespace swap_faces
{
    public class Startup
    {
        public Startup(IConfigurationRoot configuration)
        {
            Configuration = configuration;
        }

        public IConfigurationRoot Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors();
            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });

            services.AddHttpContextAccessor();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo()
                {
                    Version = "v1",
                    Title = "SwapFaces API",
                    Description = "Swap faces on images and videos using sberswap from SberBank AI"
                });
                var xmlFiles = Directory.GetFiles(AppContext.BaseDirectory, "*.xml", SearchOption.TopDirectoryOnly).ToList();
                xmlFiles.ForEach(xmlFile => c.IncludeXmlComments(xmlFile));
                c.DescribeAllParametersInCamelCase();
            });

            services.AddTransient<ISwapFaceProcessor, SwapFaceProcessor>();
            services.AddTransient<IImageDownloader, ImageDownloader>();
            services.AddTransient<IYoutubeHelper, YoutubeHelper>();
            services.AddTransient<IFfMpegHelper, FfMpegHelper>();
            services.AddTransient<IShellHelper, ShellHelper>();
        }

        public void Configure(IApplicationBuilder app)
        {
            // app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseCors(options => options.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
            app.UseEndpoints(x => x.MapControllers());
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "SwapFaces API");
            });
            ConfigureAuditNet();

            Directory.CreateDirectory(Settings.RootPath);
            Directory.CreateDirectory(Settings.YoutubeCacheRootPath);
            Directory.CreateDirectory(Settings.RequestRootPath);
        }

        private void ConfigureAuditNet()
        {
            Audit.Core.Configuration.Setup()
                .UseUdp(_ => _
                    .RemoteAddress("127.0.0.1")
                    .RemotePort(2223)
                    .CustomSerializer(ev =>
                    {
                        if (ev.EventType == "Ephemeral")
                        {
                            return Encoding.UTF8.GetBytes(ev.CustomFields["Status"] as string);
                        }
                        else if (ev is Audit.WebApi.AuditEventWebApi)
                        {
                            var action = (ev as Audit.WebApi.AuditEventWebApi)!.Action;
                            var msg = $"Action: {action.ControllerName}/{action.ActionName}{new Uri(action.RequestUrl).Query} - Response: {action.ResponseStatusCode} {action.ResponseStatus}. Event: {action.ToJson()}";
                            return Encoding.UTF8.GetBytes(msg);
                        }
                        return new byte[0];
                    }));

            LogHelper.EphemeralLog($"SwapFaces started at {DateTime.Now}", true);
        }

        
    }
}

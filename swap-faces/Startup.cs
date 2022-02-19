using swap_faces.Helpers;
using swap_faces.Swap;
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

            services.AddTransient<ISwapFaceProcessor, SwapFaceProcessor>();
            services.AddTransient<IImageDownloader, ImageDownloader>();
            services.AddTransient<IYoutubeHelper, YoutubeHelper>();
            services.AddTransient<IFfMpegHelper, FfMpegHelper>();
            services.AddTransient<IShellHelper, ShellHelper>();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseCors(options => options.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
            app.UseEndpoints(x => x.MapControllers());

            Directory.CreateDirectory(Settings.RootPath);
            Directory.CreateDirectory(Settings.YoutubeCacheRootPath);
            Directory.CreateDirectory(Settings.RequestRootPath);
        }

        public static void EphemeralLog(string text, bool important = false)
        {
            // TODO: implement log to UDP port
            Console.WriteLine(text);
        }
    }
}

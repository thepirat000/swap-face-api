using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace SwapFaces.Controllers
{
    [Route("[controller]")]
    [EnableCors]
    public class TestController : ControllerBase
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        public TestController(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }
        [HttpGet]
        public string Get()
        {
            var ip = _httpContextAccessor?.HttpContext?.Connection?.RemoteIpAddress?.ToString();
            var moduleFile = Process.GetCurrentProcess()?.MainModule?.FileName ?? "";
            var lastModified = System.IO.File.GetLastWriteTime(moduleFile);
            var x = JsonSerializer.Serialize(new
            {
                Environment.MachineName,
                OSArchitecture = RuntimeInformation.OSArchitecture.ToString(),
                OSDescription = RuntimeInformation.OSDescription.ToString(),
                BuildDate = lastModified,
                Environment.ProcessorCount,
                ClientIp = ip
            });
            return x;
        }
    }
}

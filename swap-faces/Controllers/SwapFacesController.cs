using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using swap_faces.Dto;
using swap_faces.Helpers;
using swap_faces.Swap;
using System.IO;
using System.Text.Json;

namespace swap_faces.Controllers
{
    [Route("swf")]
    [EnableCors]
    public class SwapFacesController : Controller
    {
        private readonly ILogger<SwapFacesController> _logger;
        private readonly ISwapFaceProcessor _swapFaceProcessor;

        public SwapFacesController(ILogger<SwapFacesController> logger,
            ISwapFaceProcessor swapFaceProcessor)
        {
            _logger = logger;
            _swapFaceProcessor = swapFaceProcessor;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost("p")]
        [Produces("application/json")]
        public async Task<ActionResult<SwapFacesProcessResponse>> Process([FromForm] string swapFacesRequestJson)
        {
            var request = JsonSerializer.Deserialize<SwapFacesRequest>(swapFacesRequestJson);
            var totalBytes = Request.Form.Files.Sum(f => f.Length);

            await _swapFaceProcessor.Process(request, Request.Form.Files);

            return new SwapFacesProcessResponse();
        }

        [HttpPost("process")]
        [Produces("application/json")]
        public async Task f()
        {

        }
    }

}

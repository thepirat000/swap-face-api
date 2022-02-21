using Audit.WebApi;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using swap_faces.Dto;
using swap_faces.Helpers;
using swap_faces.Swap;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace swap_faces.Controllers
{
    [Route("swap")]
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

        private static readonly HashSet<string> ValidTypes = new HashSet<string>(new string[] { "video", "image" }, StringComparer.InvariantCultureIgnoreCase);
        
        private static readonly Regex ValidateUrl = new Regex(@"^http(s)?:\/\/", RegexOptions.IgnoreCase);
        private static readonly Regex ValidateIndex = new Regex(@"^\d{1,2}$");
        private static readonly Regex ValidateTime = new Regex(@"^(\d{1,2}:)?\d{2}:\d{2}(\.\d*)?$");
        private static readonly Regex ValidateRequestId = new Regex(@"^[0-9a-f]{8}$");

        [HttpPost("p/{type}")]
        [Produces("application/json")]
        [AuditApi(IncludeResponseBody = true)]
        public async Task<ActionResult<SwapFacesProcessResponse>> Process(
            [FromRoute] string type, 
            [FromForm(Name = "tm")] string targetMedia,
            [FromForm(Name = "sf")] string sourceFaces,
            [FromForm(Name = "tf")] string? targetFaces = null,
            [FromForm(Name = "tst")] string? targetStartTime = null,
            [FromForm(Name = "tet")] string? targetEndTime = null,
            [FromForm(Name = "sr")] bool superResolution = false)
        {
            if (type == null || !ValidTypes.Contains(type))
            {
                return BadRequest("Invalid type");
            }
            bool isVideo = type.Equals("video", StringComparison.InvariantCultureIgnoreCase);
            if (targetMedia == null)
            {
                return BadRequest("Missing target media");
            }
            if (targetStartTime != null && !ValidateTime.IsMatch(targetStartTime))
            {
                return BadRequest("Invalid target start time");
            }
            if (targetEndTime != null && !ValidateTime.IsMatch(targetEndTime))
            {
                return BadRequest("Invalid target end time");
            }
            if (string.IsNullOrEmpty(sourceFaces))
            {
                return BadRequest("Missing source faces");
            }
            if (isVideo && string.IsNullOrEmpty(targetFaces))
            {
                return BadRequest("Missing target faces for video");
            }

            var totalBytes = Request.Form.Files?.Sum(f => f.Length) ?? 0;
            if (totalBytes > 10000000)
            {
                return BadRequest("Maximum file size reached");
            }
            // Make the request
            var request = new SwapFacesRequest()
            {
                SuperResolution = superResolution,
                TargetMedia = new TargetMedia()
                {
                    Id = targetMedia,
                    StartAtTime = targetStartTime,
                    EndAtTime = targetEndTime
                },
                SwapFaces = new List<SwapFace>()
            };
            if (ValidateUrl.IsMatch(targetMedia))
            {
                request.TargetMedia.Type = TargetMediaType.VideoUrl;
            }
            else if (ValidateIndex.IsMatch(targetMedia))
            {
                request.TargetMedia.Type = isVideo ? TargetMediaType.VideoFileIndex : TargetMediaType.ImageFileIndex;
            }
            else
            {
                request.TargetMedia.Type = isVideo ? TargetMediaType.VideoFileName : TargetMediaType.ImageFileName;
            }
            // Source and Target faces
            var srcFaces = sourceFaces.Split(',');
            var tgtFaces = targetFaces?.Split(',');
            for (int i = 0; i < srcFaces.Length; i++)
            {
                var srcFace = srcFaces[i];
                var tgtFace = tgtFaces != null && tgtFaces.Length >= i ? tgtFaces[i] : null;
                var swapFace = new SwapFace()
                {
                    SourceId = srcFace,
                    SourceType = GetFaceType(srcFace)!.Value,
                    TargetId = tgtFace,
                    TargetType = GetFaceType(tgtFace)
                };
                request.SwapFaces.Add(swapFace);
            }

            LogHelper.EphemeralLog("SwapFaceProcessor Request: " + JsonSerializer.Serialize(request));
            var result = await _swapFaceProcessor.Process(request, Request.Form.Files);
            LogHelper.EphemeralLog("SwapFaceProcessor Response: " + JsonSerializer.Serialize(result));

            var fileName = result.Success == true ? Path.GetFileName(result.OutputFileName) : null;
            var urlDownload = fileName == null ? null : Url.ActionLink("d", null, new { r = request.RequestId, f = fileName });
            return Ok(new SwapFacesProcessResponse()
            {
                ErrorOutput = result.StdError,
                FileName = fileName,
                DownloadUrl = urlDownload,
                RequestId = request.RequestId,
                Success = result.Success
            });
        }

        [HttpGet("d")]
        [AuditApi(IncludeResponseBody = false)]
        public async Task<ActionResult> Download([FromQuery(Name = "r")] string requestId, [FromQuery(Name = "f")] string fileName)
        {
            if (!ValidateRequestId.IsMatch(requestId))
            {
                return BadRequest("Invalid request ID");
            }
            if (string.IsNullOrEmpty(fileName))
            {
                return BadRequest("Must provide a file name");
            }

            var filePath = _swapFaceProcessor.GetFilePathForDownload(requestId, fileName);
            if (filePath != null)
            {
                var ext = Path.GetExtension(filePath);
                var contentType = ext.Equals(".mp4", StringComparison.InvariantCultureIgnoreCase) ? "video/mp4" : "image/jpeg";
                return PhysicalFile(filePath, contentType, $"{requestId}_{fileName}");
            }

            return Problem("File not found");
        }


        private FaceFromType? GetFaceType(string? id)
        {
            if (id == null)
            {
                return null;
            }
            if (ValidateUrl.IsMatch(id))
            {
                return FaceFromType.ImageUrl;
            }
            if (ValidateIndex.IsMatch(id))
            {
                return FaceFromType.FileIndex;
            }
            if (ValidateTime.IsMatch(id))
            {
                return FaceFromType.FrameAtTarget;
            }
            return FaceFromType.FileName;
        }
    }

}

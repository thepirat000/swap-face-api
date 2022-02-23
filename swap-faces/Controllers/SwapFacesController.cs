using Audit.WebApi;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using SwapFaces.Dto;
using SwapFaces.Helpers;
using SwapFaces.Swap;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SwapFaces.Controllers
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

        private static readonly Regex ValidateUrl = new Regex(@"^http(s)?:\/\/", RegexOptions.IgnoreCase);
        private static readonly Regex ValidateTime = new Regex(@"^(\d{1,2}:)?(\d{1,2}:)?\d{1,2}(\.\d*)?$");
        private static readonly Regex ValidateRequestId = new Regex(@"^[0-9a-f]{8}$");

        /// <summary>
        /// Processes a request to swap one o more faces on a given media
        /// </summary>
        /// <param name="type">Type of target media (video OR image)</param>
        /// <param name="targetMedia">Target media (URL to get media OR FileName on the form files collection)</param>
        /// <param name="sourceFaces">Comma separated list of source faces to apply (each one as a URL to get the image, FileName on the form files collection, OR the frame on the source media at the given time in format HH:MM:SS.FFFF)</param>
        /// <param name="targetFaces">Comma separated list of source faces to apply (each one as a URL to get the image, FileName on the form files collection, OR the frame on the source media at the given time in format HH:MM:SS.FFFF)</param>
        /// <param name="targetStartTime">Start time for the target media (only for video target media) in format HH:MM:SS.FFFF. If not given, it processes from the start of the target video</param>
        /// <param name="targetEndTime">End time for the target media (only for video target media) in format HH:MM:SS.FFFF. If not given, it processes to the end of the target video</param>
        /// <param name="superResolution">True to indicate the use of super resolution. Default is false.</param>
        /// <param name="dl">Download type. NULL: returns JSON with the URL to download. 0: file. 1: file stream.</param>
        /// <returns></returns>
        [HttpPost("p/{type}")]
        [Produces("application/json")]
        [AuditApi(IncludeResponseBody = true)]
        public async Task<ActionResult<SwapFacesProcessResponse>> Process(
            [FromRoute] MediaType type, 
            [FromForm(Name = "tm")] string targetMedia,
            [FromForm(Name = "sf")] string sourceFaces,
            [FromForm(Name = "tf")] string? targetFaces = null,
            [FromForm(Name = "tst")] string? targetStartTime = null,
            [FromForm(Name = "tet")] string? targetEndTime = null,
            [FromForm(Name = "sr")] bool superResolution = false,
            [FromQuery] int? dl = null)
        {
            bool isVideo = type == MediaType.Video;
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
                    MediaType = isVideo ? MediaType.Video : MediaType.Image,
                    StartAtTime = targetStartTime,
                    EndAtTime = targetEndTime
                },
                SwapFaces = new List<SwapFace>()
            };
            if (ValidateUrl.IsMatch(targetMedia))
            {
                request.TargetMedia.SourceType = TargetMediaSourceType.Url;
            }
            else
            {
                request.TargetMedia.SourceType = TargetMediaSourceType.FileName;
            }

            // Source and Target faces
            var srcFaces = sourceFaces.Split(',');
            var tgtFaces = targetFaces?.Split(',');
            for (int i = 0; i < srcFaces.Length; i++)
            {
                var srcFace = srcFaces[i];
                var tgtFace = tgtFaces != null && tgtFaces.Length >= i ? tgtFaces[i] : null;
                request.SwapFaces.Add(new SwapFace()
                {
                    SourceId = srcFace,
                    SourceType = GetFaceType(srcFace)!.Value,
                    TargetId = tgtFace,
                    TargetType = GetFaceType(tgtFace)
                });
            }

            LogHelper.EphemeralLog("SwapFaceProcessor Request: " + JsonSerializer.Serialize(request));
            var result = await _swapFaceProcessor.Process(request, Request.Form.Files);
            LogHelper.EphemeralLog("SwapFaceProcessor Response: " + JsonSerializer.Serialize(result));

            var fileName = result.Success == true ? Path.GetFileName(result.OutputFileName) : null;
            var urlDownload = fileName == null ? null : Url.ActionLink("Download", null, new { r = request.RequestId, f = fileName });

            if (dl.HasValue && fileName != null)
            {
                // Direct download requested
                return Download(request.RequestId, fileName, dl.Value);
            }

            return Ok(new SwapFacesProcessResponse()
            {
                ErrorOutput = result.StdError.Length > 4096 ? result.StdError[^4096..] : result.StdError,
                FileName = fileName,
                DownloadUrl = urlDownload,
                RequestId = request.RequestId,
                Success = result.Success
            });
        }

        /// <summary>
        /// Downloads an already processed media
        /// </summary>
        /// <param name="requestId">The original request ID</param>
        /// <param name="fileName">The file name to download</param>
        /// <param name="download">1 to indicate the file should be returned as an attachment</param>
        [HttpGet("d")]
        [AuditApi(IncludeResponseBody = false)]
        public ActionResult Download([FromQuery(Name = "r")] string requestId, [FromQuery(Name = "f")] string fileName, [FromQuery(Name = "dl")] int download = 0)
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
                if (!new FileExtensionContentTypeProvider().TryGetContentType(filePath, out string? contentType))
                {
                    contentType = Path.GetExtension(filePath).Equals(".mp4", StringComparison.InvariantCultureIgnoreCase) ? "video/mp4" : "image/jpeg";
                }
                HttpContext.Response.Headers.Add("x-download-url", Url.ActionLink("Download", null, new { r = requestId, f = fileName, dl = download }));
                if (download > 0)
                {
                    return PhysicalFile(filePath, contentType, $"{requestId}_{fileName}");
                }
                else
                {
                    return new FileStreamResult(new FileStream(filePath, FileMode.Open, FileAccess.Read), contentType);
                }
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
            if (ValidateTime.IsMatch(id))
            {
                return FaceFromType.FrameAtTarget;
            }
            return FaceFromType.FileName;
        }
    }

}

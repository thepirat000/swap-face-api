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
        /// <param name="form">The form data</param>
        /// <returns></returns>
        [HttpPost("p/{type}")]
        [Produces("application/json")]
        [AuditApi(IncludeResponseBody = true)]
        public async Task<ActionResult<SwapFacesProcessResponse>> Process(
            [FromRoute] MediaType type, 
            [FromForm] ProcessForm form
            )
        {
            bool isVideo = type == MediaType.Video;
            var targetMedia = form.TargetMedia;
            var targetStartTime = form.TargetStartTime;
            var targetEndTime = form.TargetEndTime;
            var sourceFaces = form.SourceFaces;
            var targetFaces = form.TargetFaces;
            var superResolution = form.SuperResolution;
            var downloadType = form.Download;

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

            // We can use either Request.Form or form.Files
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
            var urlDownload = fileName == null ? null : Url.ActionLink("Download", null, new { requestId = request.RequestId });

            if (result.Success && downloadType != DownloadType.None)
            {
                // Direct download requested
                return Download(request.RequestId, downloadType);
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
        /// <param name="download">1 to indicate the file should be returned as an attachment</param>
        [HttpGet("d/{requestId}")]
        [AuditApi(IncludeResponseBody = false)]
        public ActionResult Download([FromRoute] string requestId, [FromQuery(Name = "dl")] DownloadType download = DownloadType.None)
        {
            if (!ValidateRequestId.IsMatch(requestId))
            {
                return BadRequest("Invalid request ID");
            }
            if (download == DownloadType.None)
            {
                download = DownloadType.Stream;
            }
            var filePath = _swapFaceProcessor.GetFilePathForDownload(requestId);
            if (filePath != null)
            {
                if (!new FileExtensionContentTypeProvider().TryGetContentType(filePath, out string? contentType))
                {
                    contentType = Path.GetExtension(filePath).Equals(".mp4", StringComparison.InvariantCultureIgnoreCase) ? "video/mp4" : "image/jpeg";
                }
                HttpContext.Response.Headers.Add("x-download-url", Url.ActionLink("Download", null, new { r = requestId, dl = download }));
                var fileDownloadName = $"{requestId}{Path.GetExtension(filePath)}";
                if (download == DownloadType.Attachment)
                {
                    var physicalFile = PhysicalFile(filePath, contentType, fileDownloadName);
                    physicalFile.EnableRangeProcessing = true;
                    return physicalFile;
                }
                else
                {
                    var streamResult = new FileStreamResult(new FileStream(filePath, FileMode.Open, FileAccess.Read), contentType);
                    streamResult.EnableRangeProcessing = true;
                    //streamResult.FileDownloadName = fileDownloadName;
                    return streamResult;
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

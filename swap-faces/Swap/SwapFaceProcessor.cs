using SwapFaces.Dto;
using SwapFaces.Helpers;
using System.Text;

namespace SwapFaces.Swap
{
    public class SwapFaceProcessor : ISwapFaceProcessor
    {
        private readonly IImageDownloader _imageDownloader;
        private readonly IYoutubeHelper _youtubeHelper;
        private readonly IFfMpegHelper _ffMpegHelper;
        private readonly IShellHelper _shellHelper;

        public SwapFaceProcessor(IImageDownloader imageDownloader, IYoutubeHelper youtubeHelper, IFfMpegHelper ffMpegHelper, IShellHelper shellHelper)
        {
            _imageDownloader = imageDownloader;
            _youtubeHelper = youtubeHelper;
            _ffMpegHelper = ffMpegHelper;
            _shellHelper = shellHelper;
        }

        public async Task<ProcessResult> Process(SwapFacesRequest request, IFormFileCollection? formFiles)
        {
            // Create the file for the target video or image
            var inputFilePath = await CreateTargetMedia(request, formFiles);
            var originalTargetFilePath = inputFilePath;

            // Trim the video if needed
            inputFilePath = TrimTargetMedia(request.TargetMedia, inputFilePath);
            // Validate file
            if (!File.Exists(inputFilePath))
            {
                throw new Exception("Unknown error creating target media");
            }
            // Validate media type
            var fileMediaType = _ffMpegHelper.GetMediaType(inputFilePath);
            if (!fileMediaType.HasValue)
            {
                throw new Exception("Unknown error creating target media");
            }

            // Validate video duration
            if (request.TargetMedia.MediaType == MediaType.Video && File.Exists(inputFilePath))
            {
                var duration = _ffMpegHelper.GetVideoDuration(inputFilePath);
                if (duration > Settings.Youtube_MaxDuration)
                {
                    File.Delete(inputFilePath);
                    throw new ArgumentException($"Video duration cannot be longer than {Settings.Youtube_MaxDuration}");
                }
            }
            if (request.TargetMedia.MediaType != fileMediaType)
            {
                File.Delete(inputFilePath);
                throw new ArgumentException($"Wrong media type");
            }

            // Create the file(s) for the source face image(s)
            var sourceImageFilePaths = await CreateSourceImages(request, formFiles, originalTargetFilePath);
            if (sourceImageFilePaths.Any(path => !File.Exists(path)))
            {
                throw new Exception("Unknown error creating source images");
            }

            // Create the file(s) for the target face image(s)
            var targetImageFilePaths = await CreateTargetImages(request, formFiles, originalTargetFilePath);
            if (targetImageFilePaths.Any(path => !File.Exists(path)))
            {
                throw new Exception("Unknown error creating target images");
            }

            // Execute the inference command
            var outputFilePath = GetOutputFilePath(request);
            var inference = await ExecuteInferenceCommand(request, inputFilePath, sourceImageFilePaths, targetImageFilePaths, outputFilePath);

            // outputFilePath var MUST have the final filename to return
            if (File.Exists(outputFilePath) && request.TargetMedia.MediaType == MediaType.Video)
            {
                // Re-add audio from original
                string tempFilePath = Path.Combine(Path.GetDirectoryName(outputFilePath), Path.GetFileNameWithoutExtension(outputFilePath) + "_audio" + Path.GetExtension(outputFilePath));
                _ffMpegHelper.MergeAudio(outputFilePath, inputFilePath, tempFilePath);
                // Remove previous video file
                File.Delete(outputFilePath);
                // Set the final path
                outputFilePath = tempFilePath;

                // Encode to h264 if needed 
                tempFilePath = Path.Combine(Path.GetDirectoryName(outputFilePath), Path.GetFileNameWithoutExtension(outputFilePath) + "_h264" + Path.GetExtension(outputFilePath));
                if (_ffMpegHelper.TryChangeVideoCodec(outputFilePath, "h264", tempFilePath))
                {
                    // Remove previous video file
                    File.Delete(outputFilePath);
                    // Set the final path
                    outputFilePath = tempFilePath;
                }
            }
  
            var fileInfo = new FileInfo(outputFilePath);
            if (fileInfo.Exists)
            {
                // Write the processed filename on the .id file
                await File.WriteAllTextAsync(Path.Combine(Settings.RequestRootPath, request.RequestId, ".id"), Path.GetFileName(outputFilePath));
            }
            return new ProcessResult()
            {
                OutputFileName = fileInfo.Exists ? outputFilePath : null,
                StdError = inference.StdError.ToString(),
                StdOutput = inference.StdOutput.ToString(),
                Success = fileInfo.Exists && fileInfo.Length > 0
            };
        }

        /// <summary>
        /// Trims the target media if needed and return the final target video path
        /// </summary>
        private string TrimTargetMedia(TargetMedia targetMedia, string filePath)
        {
            if (targetMedia.MediaType == MediaType.Video && (targetMedia.StartAtTime != null || targetMedia.EndAtTime != null))
            {
                var start = targetMedia.StartAtTime == null ? "00:00:00" : targetMedia.StartAtTime;
                var end = targetMedia.EndAtTime == null ? "01:00:00" : targetMedia.EndAtTime;
                var trimFilePath = Path.Combine(Path.GetDirectoryName(filePath)!, Path.GetFileNameWithoutExtension(filePath) + "_trim" + Path.GetExtension(filePath));
                _ffMpegHelper.TrimVideo(filePath, start, end, trimFilePath);
                return trimFilePath;
            }
            return filePath;
        }
        /// <summary>
        /// Creates the target media to swap (Video or Image) and returns the file generated
        /// </summary>
        private async Task<string> CreateTargetMedia(SwapFacesRequest request, IFormFileCollection? formFiles)
        {
            // {root}/{requestId}/target/{fileName}.{extension}
            var targetMedia = request.TargetMedia;
            string filePath = null;
            switch (targetMedia.SourceType)
            {
                case TargetMediaSourceType.Url:
                    var mediaUri = new Uri(targetMedia.Id);
                    if (targetMedia.MediaType == MediaType.Video)
                    {
                        // Download video from URL
                        
                        filePath = _youtubeHelper.GetVideoFilePath(mediaUri);
                        if (!File.Exists(filePath))
                        {
                            // Duration validation on URL
                            var info = _youtubeHelper.GetVideoInfo(mediaUri);
                            if (info.DurationSeconds > Settings.Youtube_MaxDuration)
                            {
                                File.Delete(filePath);
                                throw new ArgumentException($"Video duration cannot be longer than {Settings.Youtube_MaxDuration}");
                            }
                            filePath = _youtubeHelper.DownloadVideoAndAudio(mediaUri).VideoFileFullPath;
                        }
                    }
                    else
                    {
                        // Download image from URL
                        var targetfilePathWithoutExtension = Path.Combine(Settings.RequestRootPath, request.RequestId, "target");
                        filePath = await _imageDownloader.DownloadImage(mediaUri, targetfilePathWithoutExtension);
                    }
                    break;
                case TargetMediaSourceType.FileName:
                    var file = formFiles.FirstOrDefault(f => f.FileName.Equals(targetMedia.Id, StringComparison.InvariantCultureIgnoreCase));
                    if (file != null)
                    {
                        filePath = await WriteTargetFile(request.RequestId, file, targetMedia.MediaType == MediaType.Video ? ".mp4" : ".jpg");
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
            return filePath;
        }

        private async Task<string> WriteTargetFile(string requestId, IFormFile file, string defaultExt)
        {
            var folderTarget = Path.Combine(Settings.RequestRootPath, requestId);
            Directory.CreateDirectory(folderTarget);
            var ext = Path.GetExtension(file.FileName);
            if (string.IsNullOrEmpty(ext))
            {
                ext = defaultExt;
            }
            var filePath = Path.Combine(folderTarget, $"target{ext}");
            using (var output = File.Create(filePath))
            {
                await file.CopyToAsync(output);
            }
            return filePath;
        }

        /// <summary>
        /// Creates the image file(s) which will be the input image(s) with the faces to replace the original faces of the target
        /// </summary>
        private async Task<string[]> CreateSourceImages(SwapFacesRequest request, IFormFileCollection formFiles, string inputFilePath)
        {
            var folder = Path.Combine(Settings.RequestRootPath, request.RequestId);
            var paths = new string[request.SwapFaces.Count];
            for (int i = 0; i < request.SwapFaces.Count; i++)
            {
                string filePath = null;
                SwapFace swapFace = request.SwapFaces[i];
                switch (swapFace.SourceType)
                {
                    case FaceFromType.FrameAtTarget:
                        // Capture frame at TargetId duration on inputVideoFilePath using ffmpeg
                        filePath = Path.Combine(folder, $"FS_{i:D2}.jpg");
                        _ffMpegHelper.CreateImageForFrame(inputFilePath, swapFace.SourceId, filePath);
                        if (!File.Exists(filePath))
                        {
                            throw new Exception($"Unknown error extracting frame");
                        }
                        break;
                    case FaceFromType.ImageUrl:
                        filePath = await _imageDownloader.DownloadImage(new Uri(swapFace.SourceId), Path.Combine(folder, $"FS_{i:D2}"));
                        break;
                    case FaceFromType.FileName:
                        var file = formFiles.FirstOrDefault(f => f.FileName.Equals(swapFace.SourceId, StringComparison.InvariantCultureIgnoreCase));
                        if (file != null)
                        {
                            var ext = Path.GetExtension(file.FileName);
                            if (string.IsNullOrEmpty(ext))
                            {
                                ext = ".jpg";
                            }
                            filePath = Path.Combine(folder, $"FS_{i:D2}{ext}");
                            Directory.CreateDirectory(folder);
                            using (var output = File.Create(filePath))
                            {
                                await file.CopyToAsync(output);
                            }
                        }
                        break;
                    default:
                        throw new NotImplementedException();
                }
                paths[i] = filePath;
            }
            return paths;
        }

        /// <summary>
        /// Creates the image file(s) with the matching faces to be replaced from the target video
        /// </summary>
        private async Task<string[]> CreateTargetImages(SwapFacesRequest request, IFormFileCollection formFiles, string inputFilePath)
        {
            var folder = Path.Combine(Settings.RequestRootPath, request.RequestId);
            var paths = new List<string>();
            for (int i = 0; i < request.SwapFaces.Count; i++)
            {
                SwapFace swapFace = request.SwapFaces[i];
                if (swapFace.TargetType != null)
                {
                    string filePath = null;
                    switch (swapFace.TargetType)
                    {
                        case FaceFromType.FrameAtTarget:
                            // Capture frame at TargetId duration on inputVideoFilePath using ffmpeg
                            filePath = Path.Combine(folder, $"FT_{i:D2}.jpg");
                            _ffMpegHelper.CreateImageForFrame(inputFilePath, swapFace.TargetId, filePath);
                            if (!File.Exists(filePath))
                            {
                                throw new Exception($"Unknown error extracting frame");
                            }
                            break;
                        case FaceFromType.ImageUrl:
                            filePath = await _imageDownloader.DownloadImage(new Uri(swapFace.TargetId), Path.Combine(folder, $"FT_{i:D2}"));
                            break;
                        case FaceFromType.FileName:
                            var file = formFiles.FirstOrDefault(f => f.FileName.Equals(swapFace.TargetId, StringComparison.InvariantCultureIgnoreCase));
                            var ext = Path.GetExtension(file.FileName);
                            if (string.IsNullOrEmpty(ext))
                            {
                                ext = ".jpg";
                            }
                            filePath = Path.Combine(folder, $"FT_{i:D2}{ext}");
                            using (var output = File.Create(filePath))
                            {
                                await file.CopyToAsync(output);
                            }
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                    paths.Add(filePath);
                }
            }
            return paths.ToArray();
        }

        private string GetInferenceCommand(SwapFacesRequest request, string inputFilePath, string[] sourceImageFilePaths, string[] targetImageFilePaths, string outputFilePath)
        {
            var superResolutionArg = request.SuperResolution ? "--use_sr True" : "";
            var targetFacesPathArg = targetImageFilePaths?.Length > 0 ? "--target_faces_paths " + string.Join(" ", targetImageFilePaths.Select(s => @"""" + s + @"""")) : "";
            var sourcePaths = string.Join(" ", sourceImageFilePaths.Select(s => @"""" + s + @""""));

            if (request.TargetMedia.MediaType == MediaType.Image)
            {
                // Image target
                // python inference.py --source_paths "a.jpg" --target_image {PATH_TO_IMAGE} --target_faces_paths "b.jpg" --image_to_image True 
                if (string.IsNullOrEmpty(targetFacesPathArg))
                {
                    // Workaround. For image to image, if no target face paths indicated, it should be the same as the target image (otherwise it fails)
                    targetFacesPathArg = @$"--target_faces_paths ""{inputFilePath}""";
                }
                return @$"python inference.py --source_paths {sourcePaths} {targetFacesPathArg} --target_image ""{inputFilePath}"" {superResolutionArg} {Settings.InferenceExtraArguments} --image_to_image True --out_image_name ""{outputFilePath}""";
            }
            else
            {
                // Video target
                // python inference.py --source_paths "/temp/lupi2.jpg" "/temp/fede1.jpg" --target_faces_paths /temp/guerita.JPG /temp/guerito.JPG --target_video /temp/stefan.mp4
                return @$"python inference.py --source_paths {sourcePaths} {targetFacesPathArg} --target_video ""{inputFilePath}"" {superResolutionArg} {Settings.InferenceExtraArguments} --out_video_name ""{outputFilePath}""";
            }
        }
        public class ExecuteInferenceResult
        {
            public ExecuteResult CommandResult { get; set; }
        }
        private async Task<ExecuteResult> ExecuteInferenceCommand(SwapFacesRequest request, string inputFilePath, 
            string[] sourceImageFilePaths, string[] targetImageFilePaths,
            string outputFilePath)
        {
            // Generate the output video or image
            var inferenceCommand = GetInferenceCommand(request, inputFilePath, sourceImageFilePaths, targetImageFilePaths, outputFilePath);
            LogHelper.EphemeralLog($"Inference Command: {inferenceCommand}");

            // Execute Conda code
            var commands = new string[]
            {
                Settings.AnacondaActivateScript,
                @$"cd ""{Settings.AnacondaWorkingDirectory}""",
                inferenceCommand,
                Settings.AnacondaDeactivateScript
            };
            var sbStdErr = new StringBuilder();
            var sbStdOut = new StringBuilder();
            var result = await _shellHelper.ExecuteWithTimeout(
                commands,
                Settings.AnacondaWorkingDirectory,
                Settings.ProcessTimeoutMins,
                e =>
                {
                    sbStdErr.AppendLine(e);
                    LogHelper.EphemeralLog("STDERR: " + e);
                },
                o =>
                {
                    sbStdOut.AppendLine(o);
                    LogHelper.EphemeralLog("STDOUT: " + o);
                });
            return result;
        }

        private string GetOutputFilePath(SwapFacesRequest request)
        {
            return Path.Combine(Settings.RequestRootPath, request.RequestId,
                "processed" + (request.SuperResolution ? "_sr" : "") + (request.TargetMedia.MediaType == MediaType.Video ? ".mp4" : ".jpg")) ;
        }

        public string? GetFilePathForDownload(string requestId)
        {
            var idFile = Path.Combine(Settings.RequestRootPath, requestId, ".id");
            if (File.Exists(idFile))
            {
                var fileName = File.ReadAllText(idFile);
                var filePath = Path.Combine(Settings.RequestRootPath, requestId, fileName);
                return File.Exists(filePath) ? filePath : null;
            }
            return null;
        }
    }
}

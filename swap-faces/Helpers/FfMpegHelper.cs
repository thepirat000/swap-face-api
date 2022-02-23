using SwapFaces.Dto;

namespace SwapFaces.Helpers
{
    public class FfMpegHelper : IFfMpegHelper
    {
        private readonly IShellHelper _shellHelper;

        public FfMpegHelper(IShellHelper shellHelper)
        {
            _shellHelper = shellHelper;
        }

        public void CreateImageForFrame(string inputVideoFilePath, string frameAtTime, string outputFilePath)
        {
            // ffmpeg -ss {frameAtTime} -i {inputVideo} -frames:v 1 -q:v 2 {outputFilePath}
            var ffmpegCmd = @$"ffmpeg -ss {frameAtTime} -i ""{inputVideoFilePath}"" -frames:v 1 -q:v 2 ""{outputFilePath}"" -y";
            var shellResult = _shellHelper.Execute(ffmpegCmd);
            if (shellResult.ExitCode != 0)
            {
                throw new Exception(shellResult.Output);
            }
        }

        public void TrimVideo(string inputVideoFilePath, string startAtTime, string endAtTime, string outputFilePath)
        {
            // ffmpeg -ss {startAtTime} -to {endAtTime} -i {inputVideoFilePath} -c copy {outputFilePath}
            var ffmpegCmd = @$"ffmpeg -ss {startAtTime} -to {endAtTime} -i ""{inputVideoFilePath}"" -c copy ""{outputFilePath}"" -y";
            var shellResult = _shellHelper.Execute(ffmpegCmd);
            if (shellResult.ExitCode != 0)
            {
                throw new Exception(shellResult.Output);
            }
        }

        public void MergeAudio(string inputVideoFilePath, string audioFilePath, string outputFilePath)
        {
            // ffmpeg -i C:\GIT\sber-swap\examples\results\result.mp4 -i C:/temp/chantaje1_trim.mp4 -c:v copy -map 0:v:0? -map 1:a:0 C:/temp/chantaje1_lupi.mp4
            var ffmpegCmd = @$"ffmpeg -i ""{inputVideoFilePath}"" -i ""{audioFilePath}"" -c:v copy -map 0:v:0 -map 1:a:0? -y ""{outputFilePath}""";
            var shellResult = _shellHelper.Execute(ffmpegCmd);
            if (shellResult.ExitCode != 0)
            {
                throw new Exception(shellResult.Output);
            }
        }

        public string GetVideoCodec(string inputVideoFilePath)
        {
            var ffProbeCmd = @$"ffprobe -v error -select_streams v:0 -show_entries stream=codec_name -of default=noprint_wrappers=1:nokey=1 ""{inputVideoFilePath}""";
            var shellResult = _shellHelper.Execute(ffProbeCmd);
            if (shellResult.ExitCode != 0)
            {
                throw new Exception(shellResult.Output);
            }
            return shellResult.Output.Split(Environment.NewLine)[0].Trim();
        }

        public void ChangeVideoCodec(string inputVideoFilePath, string videoCodec, string outputFilePath)
        {
            // ffmpeg -i input.flv -vcodec libx264 -acodec aac output.mp4
            var ffmpegCmd = @$"ffmpeg -i ""{inputVideoFilePath}"" -vcodec libx264 -y ""{outputFilePath}""";
            var shellResult = _shellHelper.Execute(ffmpegCmd);
            if (shellResult.ExitCode != 0)
            {
                throw new Exception(shellResult.Output);
            }
        }

        public bool TryChangeVideoCodec(string inputVideoFilePath, string videoCodec, string outputFilePath)
        {
            if (!GetVideoCodec(inputVideoFilePath).Equals(videoCodec, StringComparison.InvariantCultureIgnoreCase))
            {
                ChangeVideoCodec(inputVideoFilePath, videoCodec, outputFilePath);
                return true;
            }
            return false;
        }

        public double GetVideoDuration(string inputVideoFilePath)
        {
            // ffprobe -v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 "c:\x\y.mp4"
            var ffProbeCmd = @$"ffprobe -v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 ""{inputVideoFilePath}""";
            var shellResult = _shellHelper.Execute(ffProbeCmd);
            if (shellResult.ExitCode != 0)
            {
                throw new Exception(shellResult.Output);
            }
            if (double.TryParse(shellResult.Output, out double duration))
            {
                return duration;
            }
            return 0d;
        }
        
        public MediaType? GetMediaType(string inputFilePath)
        {
            // ffprobe -v error -show_entries format=format_name -of default=nokey=1:noprint_wrappers=1 "c:\x\y.mp4"
            var ffProbeCmd = @$"ffprobe -v error -show_entries format=format_name -of default=noprint_wrappers=1:nokey=1 ""{inputFilePath}""";
            var shellResult = _shellHelper.Execute(ffProbeCmd);
            if (shellResult.ExitCode != 0)
            {
                throw new Exception(shellResult.Output);
            }
            if (shellResult.Output.Contains("image", StringComparison.InvariantCultureIgnoreCase))
            {
                return MediaType.Image;
            }
            if (shellResult.Output.Contains("mp4", StringComparison.InvariantCultureIgnoreCase) || shellResult.Output.Contains("gif", StringComparison.InvariantCultureIgnoreCase))
            {
                return MediaType.Video;
            }
            return null;
        }
    }
}

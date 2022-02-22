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

        public double GetVideoDuration(string inputVideoFilePath)
        {
            // ffprobe -v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 "c:\x\y.mp4"
            var ffmpegCmd = @$"ffprobe -v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 ""{inputVideoFilePath}""";
            var shellResult = _shellHelper.Execute(ffmpegCmd);
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
    }
}

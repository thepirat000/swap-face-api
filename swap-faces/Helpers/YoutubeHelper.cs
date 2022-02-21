using swap_faces.Dto;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace swap_faces.Helpers
{
    public class YoutubeHelper : IYoutubeHelper
    {
        private readonly IShellHelper _shellHelper;

        public YoutubeHelper(IShellHelper shellHelper)
        {
            _shellHelper = shellHelper;
        }

        private static ConcurrentDictionary<string, YoutubeVideoInfo> _videoInfoCache = new ConcurrentDictionary<string, YoutubeVideoInfo>(StringComparer.InvariantCultureIgnoreCase);

        public YoutubeVideoInfo GetVideoInfo(Uri videoUri)
        {
            // https://youtu.be/{vid}
            // https://www.tiktok.com/{user}/video/{vid}
            var url = videoUri.ToString();
            if (_videoInfoCache.TryGetValue(url, out YoutubeVideoInfo? cachedInfo))
            {
                return cachedInfo;
            }
            var cmd = @$"{Settings.Youtube_Dl_Tool} -s --get-filename --get-duration --no-check-certificate ""{url}""";
            var shellResult = _shellHelper.Execute(cmd);
            if (shellResult.ExitCode != 0)
            {
                throw new Exception($"{Settings.Youtube_Dl_Tool} -s exited with code {shellResult.ExitCode}.\n{shellResult.Output}");
            }
            var dataArray = shellResult.Output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
            if (dataArray.Length < 1)
            {
                throw new Exception($"{Settings.Youtube_Dl_Tool} -s returned unformatted data {shellResult.ExitCode}.\n{shellResult.Output}");
            }
            var info = new YoutubeVideoInfo()
            {
                Filename = dataArray[0].Trim(),
                Duration = dataArray.Length > 1 ? dataArray[1] : "0"
            };
            if (info.Filename.Contains('.'))
            {
                info.Filename = _shellHelper.SanitizeFilename(info.Filename.Substring(0, info.Filename.LastIndexOf('.')));
            }
            var dur = info.Duration.Split(':');
            if (dur.Length == 1)
            {
                info.DurationSeconds = int.Parse(dur[0]);
            }
            else if (dur.Length == 2)
            {
                info.DurationSeconds = int.Parse(dur[0]) * 60 + int.Parse(dur[1]);
            }
            else if (dur.Length == 3)
            {
                info.DurationSeconds = int.Parse(dur[0]) * 3600 + int.Parse(dur[1]) * 60 + int.Parse(dur[2]);
            }
            _videoInfoCache[url] = info;
            return info;
        }

        public YoutubeVideoResponse DownloadVideoAndAudio(Uri videoUri)
        {
            var url = videoUri.ToString();
            var result = new YoutubeVideoResponse();
            var fileName = GetVideoFilePath(videoUri);
            if (File.Exists(fileName))
            {
                result.VideoFileFullPath = fileName;
                return result;
            }
            var cmd = @$"{Settings.Youtube_Dl_Tool} -f ""bestvideo[height<=720][ext=mp4]+bestaudio[ext=m4a]/[ext=mp4]"" --max-filesize 50M -o ""{fileName}"" --no-check-certificate ""{url}""";

            var shellResult = _shellHelper.Execute(cmd);
            if (shellResult.ExitCode != 0)
            {
                throw new Exception($"{Settings.Youtube_Dl_Tool} video exited with code {shellResult.ExitCode}.\n{shellResult.Output}");
            }
            if (!File.Exists(fileName))
            {
                throw new Exception($"Audio filename {fileName} not found after {Settings.Youtube_Dl_Tool}");
            }
            result.VideoFileFullPath = fileName;
            return result;
        }

        public string GetVideoFilePath(Uri videoUri)
        {
            string filename = _shellHelper.SanitizeFilename(videoUri.Host + videoUri.PathAndQuery);
            return Path.Combine(Settings.YoutubeCacheRootPath, $"{filename}.mp4");
        }
    }
}

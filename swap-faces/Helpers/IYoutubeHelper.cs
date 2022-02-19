using swap_faces.Dto;

namespace swap_faces.Helpers
{
    public interface IYoutubeHelper
    {
        YoutubeVideoInfo GetVideoInfo(Uri videoUri);
        YoutubeVideoResponse DownloadVideoAndAudio(Uri videoUri);
        string GetVideoFilePath(Uri videoUri);
    }
}

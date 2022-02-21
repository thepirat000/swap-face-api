using swap_faces.Dto;

namespace swap_faces.Helpers
{
    public interface IYoutubeHelper
    {
        YoutubeVideoInfo GetVideoInfo(Uri videoUri);
        /// <summary>
        /// Downloads the Video+Audio from a URL (youtube, tiktok, etc). 
        /// </summary>
        /// <param name="videoUri">The video URL.
        /// Examples of valid URLs:
        /// https://youtu.be/{vid}
        /// https://www.tiktok.com/{user}/video/{vid}
        /// https://i.imgur.com/{id}.mp4
        /// </param>
        YoutubeVideoResponse DownloadVideoAndAudio(Uri videoUri);
        string GetVideoFilePath(Uri videoUri);
    }
}

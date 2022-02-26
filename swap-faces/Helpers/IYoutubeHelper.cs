using SwapFaces.Dto;

namespace SwapFaces.Helpers
{
    public interface IYoutubeHelper
    {
        Task<YoutubeVideoInfo> GetVideoInfo(Uri videoUri);
        /// <summary>
        /// Downloads the Video+Audio from a URL (youtube, tiktok, etc). 
        /// </summary>
        /// <param name="videoUri">The video URL.
        /// Examples of valid URLs:
        /// https://youtu.be/{vid}
        /// https://www.tiktok.com/{user}/video/{vid}
        /// https://i.imgur.com/{id}.mp4
        /// </param>
        Task<YoutubeVideoResponse> DownloadVideoAndAudio(Uri videoUri);
        string GetVideoFilePath(Uri videoUri);
    }
}

﻿
using SwapFaces.Dto;

namespace SwapFaces.Helpers
{
    public interface IFfMpegHelper
    {
        /// <summary>
        /// Creates a jpg image from a video frame at the given time
        /// </summary>
        void CreateImageForFrame(string inputVideoFilePath, string frameAtTime, string outputFilePath);
        /// <summary>
        /// Trims a given video from a start and end time
        /// </summary>
        void TrimVideo(string inputVideoFilePath, string startAtTime, string endAtTime, string outputFilePath);
        /// <summary>
        /// Changes the audio of a given input video by the audtio given in the input audio
        /// </summary>
        void MergeAudio(string inputVideoFilePath, string audioFilePath, string outputFilePath);
        /// <summary>
        /// Returns the video duration in seconds for the given video file
        /// </summary>
        double GetVideoDuration(string inputVideoFilePath);
        /// <summary>
        /// Returns the media type for a file (image, video or NULL for unknown)
        /// </summary>
        /// <param name="inputFilePath"></param>
        /// <returns></returns>
        MediaType? GetMediaType(string inputFilePath);
    }
}

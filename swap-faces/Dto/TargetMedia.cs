namespace swap_faces.Dto
{
    public class TargetMedia
    {
        /// <summary>
        /// Target media type
        /// </summary>
        public MediaType MediaType { get; set; }
        /// <summary>
        /// Target media source type
        /// </summary>
        public TargetMediaSourceType SourceType { get; set; }
        /// <summary>
        /// The video id (youtube vid, file index or file name depending on Type)
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// Starting on frame at, format: "HH:MM:SS.FFFF". (NULL to process the whole video, Ignored for image)
        /// </summary>
        public string? StartAtTime { get; set; }
        /// <summary>
        /// Ending on frame at, format: "HH:MM:SS.FFFF". (NULL to process the whole video, Ignored for image)
        /// </summary>
        public string? EndAtTime { get; set; }
    }
}

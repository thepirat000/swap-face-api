namespace swap_faces.Dto
{
    public class TargetMedia
    {
        /// <summary>
        /// Target media type
        /// </summary>
        public TargetMediaType Type { get; set; }
        /// <summary>
        /// True for image to image, False for Video
        /// </summary>
        public bool IsImage => Type == TargetMediaType.ImageFileIndex || Type == TargetMediaType.ImageFileName;
        /// <summary>
        /// True for Video, False for image to image
        /// </summary>
        public bool IsVideo => !IsImage;
        /// <summary>
        /// The video id (youtube vid, file index or file name depending on Type)
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// Starting on frame at, format: "HH:MM:SS.FFFF". (NULL to process the whole video, Ignored for image)
        /// </summary>
        public string StartAtTime { get; set; }
        /// <summary>
        /// Ending on frame at, format: "HH:MM:SS.FFFF". (NULL to process the whole video, Ignored for image)
        /// </summary>
        public string EndAtTime { get; set; }
    }
}

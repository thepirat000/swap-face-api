namespace swap_faces.Dto
{
    public class TargetMedia
    {
        public enum MediaType
        {
            /// <summary>Youtube, tiktok, etc</summary>
            VideoUrl = 0,
            /// <summary>File index in the form collection</summary>
            VideoFileIndex = 1,
            /// <summary>File name in the form collection</summary>
            VideoFileName = 2,
            /// <summary>File index in the form collection</summary>
            ImageFileIndex = 3,
            /// <summary>File name in the form collection</summary>
            ImageFileName = 4 
        }
        /// <summary>
        /// Target media type
        /// </summary>
        public MediaType Type { get; set; }
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

namespace swap_faces.Dto
{
    public class SwapFace
    {
        public enum FaceSourceType
        {
            ImageUrl = 0,
            FileIndex = 1,
            FileName = 2
        }
        public enum FaceTargetType
        {
            FrameAt = 0,
            FileIndex = 1,
            FileName = 2
        }
        /// <summary>
        /// The source face type 
        /// </summary>
        public FaceSourceType SourceType { get; set; }
        /// <summary>
        /// The source face id (an URL to an image for ImageUrl, or the file index for FileIndex, or the file name for FileName SourceType)
        /// </summary>
        public string SourceId { get; set; }
        /// <summary>
        /// The target face type 
        /// </summary>
        public FaceTargetType TargetType { get; set; }
        /// <summary>
        /// The target face id (time in format HH:MM:SS.FFFF for FrameAt, or the file index for FileIndex, or the file name for FileName TargetType)
        /// </summary>
        public string TargetId { get; set; }
    }
}

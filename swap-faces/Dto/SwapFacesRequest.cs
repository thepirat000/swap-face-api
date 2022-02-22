namespace SwapFaces.Dto
{
    public class SwapFacesRequest
    {
        /// <summary>
        /// A unique auto-generated request ID
        /// </summary>
        public string RequestId { get; } = Guid.NewGuid().ToString().Substring(0, 8);
        /// <summary>
        /// The target media
        /// </summary>
        public TargetMedia TargetMedia { get; set; }
        /// <summary>
        /// Faces to swap
        /// </summary>
        public List<SwapFace> SwapFaces { get; set; }
        /// <summary>
        /// True for super resolution on swap images
        /// </summary>
        public bool SuperResolution { get; set; }
    }
}

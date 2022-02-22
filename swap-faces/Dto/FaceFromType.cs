namespace SwapFaces.Dto
{
    /// <summary>
    /// Indicates the source for a face image
    /// </summary>
    public enum FaceFromType
    {
        /// <summary>
        /// Image from a URL
        /// </summary>
        ImageUrl = 0,
        /// <summary>
        /// File name from the Form Files Collection
        /// </summary>
        FileName = 1,
        /// <summary>
        /// Frame of the target video at given time (HH:MM:SS.FFFF)
        /// </summary>
        FrameAtTarget = 2
    }
}

namespace SwapFaces.Dto
{
    public enum DownloadType
    {
        /// <summary>
        /// Do not download the file, return the URL to download the file
        /// </summary>
        None = 0,
        /// <summary>
        /// Returns the file as a stream to be played
        /// </summary>
        Stream = 1,
        /// <summary>
        /// Returns the file as an attached file to be downloaded
        /// </summary>
        Attachment = 2
    }

}

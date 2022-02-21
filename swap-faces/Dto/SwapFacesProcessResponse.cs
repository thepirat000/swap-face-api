namespace swap_faces.Dto
{
    public class SwapFacesProcessResponse
    {
        public string RequestId { get; set; }
        public bool Success { get; set; }
        public string? FileName { get; set; }
        public string? DownloadUrl { get; set; }
        public string ErrorOutput { get; set; }
    }
}

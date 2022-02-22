namespace SwapFaces.Helpers
{
    public interface IImageDownloader
    {
        Task<string> DownloadImage(Uri uri, string filePath);
    }
}

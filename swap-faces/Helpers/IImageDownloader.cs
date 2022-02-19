namespace swap_faces.Helpers
{
    public interface IImageDownloader
    {
        Task<string> DownloadImageAsync(Uri uri, string filePath);
    }
}

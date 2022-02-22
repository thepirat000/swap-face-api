namespace swap_faces.Helpers
{
    public interface IImageDownloader
    {
        Task<string> DownloadImage(Uri uri, string filePath);
    }
}

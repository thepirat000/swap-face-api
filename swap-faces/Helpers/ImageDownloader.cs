namespace swap_faces.Helpers
{
    public class ImageDownloader : IImageDownloader, IDisposable
    {
        private bool _disposed;
        private readonly HttpClient _httpClient;

        public ImageDownloader(HttpClient httpClient = null)
        {
            _httpClient = httpClient ?? new HttpClient();
        }

        /// <summary>
        /// Downloads an image asynchronously from the <paramref name="uri"/> and places it in the specified <paramref name="filePathWithoutExtension" adding the corresponding extension/>.
        /// </summary>
        public async Task<string> DownloadImage(Uri uri, string filePathWithoutExtension)
        {
            if (_disposed) { throw new ObjectDisposedException(GetType().FullName); }

            // Get the file extension
            var uriWithoutQuery = uri.GetLeftPart(UriPartial.Path);
            var fileExtension = Path.GetExtension(uriWithoutQuery);
            if (string.IsNullOrEmpty(fileExtension))
            {
                fileExtension = ".jpg";
            }
            // Create file path and ensure directory exists
            var path = $"{filePathWithoutExtension}{fileExtension}";

            // Download the image and write to the file
            var imageBytes = await _httpClient.GetByteArrayAsync(uri);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            await File.WriteAllBytesAsync(path, imageBytes);
            return path;
        }

        public void Dispose()
        {
            if (_disposed) { return; }
            _httpClient.Dispose();
            GC.SuppressFinalize(this);
            _disposed = true;
        }
    }
}

using SwapFaces.Dto;

namespace SwapFaces.Swap
{
    public interface ISwapFaceProcessor
    {
        Task<ProcessResult> Process(SwapFacesRequest request, IFormFileCollection? formFiles);
        string? GetFilePathForDownload(string requestId, string fileName);
    }
}

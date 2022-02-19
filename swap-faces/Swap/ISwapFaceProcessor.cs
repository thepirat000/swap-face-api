using swap_faces.Dto;

namespace swap_faces.Swap
{
    public interface ISwapFaceProcessor
    {
        Task<ProcessResult> Process(SwapFacesRequest request, IFormFileCollection formFiles);
    }
}

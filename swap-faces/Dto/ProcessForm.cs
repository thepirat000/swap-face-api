using System.ComponentModel.DataAnnotations;

namespace SwapFaces.Dto
{
    public class ProcessForm
    {
        /// <summary>
        /// Target media (URL to get media OR FileName on the form files collection)
        /// </summary>
        [Required] public string TargetMedia { get; set; }
        /// <summary>
        /// Comma separated list of source faces to apply (each one as a URL to get the image, FileName on the form files collection, OR the frame on the source media at the given time in format HH:MM:SS.FFFF)
        /// </summary>
        [Required] public string SourceFaces { get; set; }
        /// <summary>
        /// Comma separated list of source faces to apply (each one as a URL to get the image, FileName on the form files collection, OR the frame on the source media at the given time in format HH:MM:SS.FFFF)
        /// </summary>
        public string? TargetFaces { get; set; }
        /// <summary>
        /// Start time for the target media (only for video target media) in format HH:MM:SS.FFFF. If not given, it processes from the start of the target video
        /// </summary>
        public string? TargetStartTime { get; set; }
        /// <summary>
        /// End time for the target media (only for video target media) in format HH:MM:SS.FFFF. If not given, it processes to the end of the target video
        /// </summary>
        public string? TargetEndTime { get; set; }
        /// <summary>
        /// True to indicate the use of super resolution. Default is false
        /// </summary>
        public bool SuperResolution { get; set; }
        /// <summary>
        /// Download type
        /// </summary>
        public DownloadType Download { get; set; }

        public List<IFormFile> Files { get; set; }

    }

}

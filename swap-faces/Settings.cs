namespace SwapFaces
{
    public class Settings
    {
        public const string RootPath = @"C:\swap-face";
        public static string RequestRootPath = Path.Combine(RootPath, "requests");
        public static string YoutubeCacheRootPath = Path.Combine(RootPath, "yt-cache");
        public const string AnacondaActivateScript =  @"C:\tools\miniconda3\Scripts\activate.bat sber"; //@"C:\ProgramData\Miniconda3\Scripts\activate.bat sber"
        public const string AnacondaDeactivateScript = "conda deactivate";
        public const string AnacondaWorkingDirectory = @"C:\GIT\sber-swap"; 
        public const string Youtube_Dl_Tool = "yt-dlp";
        public const int Youtube_MaxDuration = 360;  // in seconds
        public const string InferenceExtraArguments = "--ignore_audio True";
    }
}

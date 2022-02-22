namespace SwapFaces.Dto
{
    public class ProcessResult
    {
        public bool Success { get; set; }
        public string StdError { get; set; }
        public string StdOutput { get; set; }
        public string OutputFileName { get; set; }
    }
}

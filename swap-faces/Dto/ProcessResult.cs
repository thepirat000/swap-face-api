namespace swap_faces.Dto
{
    public class ProcessResult
    {
        public string ErrorText { get; set; }
        public bool Success => string.IsNullOrEmpty(ErrorText);
        public string OutputText { get; set; }
        public string OutputFileName { get; set; }
    }
}

namespace swap_faces.Dto
{
    public class ExecuteResult
    {
        public int ExitCode { get; set; }
        public string Output { get; set; }
    }

    public class ExecuteResultEx : ExecuteResult
    {
        public int ErrorCount { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }
}

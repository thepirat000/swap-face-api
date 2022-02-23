using System.Text;

namespace SwapFaces.Dto
{
    public class ExecuteResult
    {
        public int ExitCode { get; set; }
        public string StdOutput { get; set; }
        public string StdError { get; set; }
        public string Output => 
            string.IsNullOrEmpty(StdError) 
                ? StdOutput 
                : StdError + Environment.NewLine + Environment.NewLine + StdOutput;
    }
}

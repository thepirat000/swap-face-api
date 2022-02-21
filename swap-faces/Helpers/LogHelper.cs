using Audit.Core;

namespace swap_faces.Helpers
{
    public static class LogHelper
    {
        public static void EphemeralLog(string text, bool important = false)
        {
            //Debug.WriteLine(text);
            AuditScope.Log("Ephemeral", new { Status = text });
        }
    }
}

using Audit.Core;
using System.Diagnostics;

namespace SwapFaces.Helpers
{
    public static class LogHelper
    {
        public static void EphemeralLog(string text, bool important = false)
        {
            try
            {
                AuditScope.Log("Ephemeral", new { Status = text });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error logging: {ex}");
            }
            
        }
    }
}

using System;
using System.Diagnostics;

namespace DeployToAzure.Utility
{
    public class OurConsoleTraceListener : TraceListener
    {
        public override void Write(string message)
        {
            return;
        }

        public override void WriteLine(string message)
        {
            try
            {
                var logMessage = string.Format("[{0}]{1} ", DateTime.UtcNow.ToString("HH:mm:ss"), message);
                Console.WriteLine(logMessage);
            }
            // ReSharper disable EmptyGeneralCatchClause
            catch
            // ReSharper restore EmptyGeneralCatchClause
            {
                //Deliberately empty
            } 
        }
    }
}
using System.Diagnostics;

namespace DeployToAzure.Utility
{
    public static class OurTrace
    {
        public static readonly TraceSource Source = new TraceSource("Trace");

        public static void AddListener(TraceListener listener)
        {
            Source.Listeners.Add(listener);
        }

        public static void TraceInfo(string message)
        {
            Source.TraceInfo(message);
        }

        public static void TraceWarning(string message)
        {
            Source.TraceWarning(message);
        }

        public static void TraceVerbose(string message)
        {
            Source.TraceVerbose(message);
        }

        public static void TraceError(string message)
        {
            Source.TraceError(message);
        }
    }
}

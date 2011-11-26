using System.Diagnostics;

namespace DeployToAzure.Utility
{
    public static class TraceSourceExtensions
    {
        public static void TraceInfo(this TraceSource source, string message)
        {
            TraceEvent(source, TraceEventType.Information, message);
        }

        public static void TraceCritical(this TraceSource source, string message)
        {
            TraceEvent(source, TraceEventType.Critical, message);
        }

        public static void TraceWarning(this TraceSource source, string message)
        {
            TraceEvent(source, TraceEventType.Warning, message);
        }

        public static void TraceVerbose(this TraceSource source, string message)
        {
            TraceEvent(source, TraceEventType.Verbose, message);
        }

        public static void TraceError(this TraceSource source, string message)
        {
            TraceEvent(source, TraceEventType.Error, message);
        }

        private static void TraceEvent(TraceSource source, TraceEventType traceEventType, string message)
        {
            source.TraceEvent(traceEventType, 0, string.Format("[{0}] {1}", traceEventType, message));
        }
    }
}

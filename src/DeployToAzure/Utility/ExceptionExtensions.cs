using System;
using System.Text;

namespace DeployToAzure.Utility
{
    public static class ExceptionExtensions
    {
        public static string ToLogString(this Exception @this, Func<Exception,string> stackTraceFormatter)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("----------");
            stringBuilder.AppendLine(@this.Message);
            stringBuilder.AppendLine("----------");
            stringBuilder.AppendLine("Debug information:");

            var currentException = @this;
            var first = true;
            while (currentException != null)
            {
                stringBuilder.AppendLine("----------");
                stringBuilder.AppendLine(string.Format("{0}{1}: {2}", first ? "" : "inner ", currentException.GetType().Name, currentException.Message));
                stringBuilder.AppendLine(stackTraceFormatter(currentException));
                currentException = currentException.InnerException;
                first = false;
            }
            stringBuilder.AppendLine("----------");

            return stringBuilder.ToString();
        }

        public static string ToLogString(this Exception @this)
        {
            return @this.ToLogString(FormatStackTrace);
        }

        private static string FormatStackTrace(Exception exception)
        {
            return exception.StackTrace;
        }
    }
}
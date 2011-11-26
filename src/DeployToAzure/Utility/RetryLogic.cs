using System;

namespace DeployToAzure.Utility
{
    public class RetryLogic
    {
        private readonly int _maxRetries;
        private readonly int _sleepIntervalMilliseconds;

        public RetryLogic(int maxRetries, int sleepIntervalMilliseconds)
        {
            _maxRetries = maxRetries;
            _sleepIntervalMilliseconds = sleepIntervalMilliseconds;
        }

        public RetryLogic(int maxRetries, TimeSpan sleepInterval)
            : this(maxRetries, (int)sleepInterval.TotalMilliseconds)
        {
        }

        public void Execute(Action operation, Func<Exception, RetryOrRethrow> outcomeTest)
        {
            Execute(operation, outcomeTest, () => true);
        }

        public void Execute(Action operation, Func<Exception, RetryOrRethrow> outcomeTest, Func<bool> successCheck)
        {
            Exception exception = null;
            var retryCount = 0;
            var success = false;

            SpinLoop.DoUntil(
                () => success = PerformOperation(operation, successCheck, out exception), 
                () => CancelTest(success, outcomeTest, exception, ref retryCount), 
                _sleepIntervalMilliseconds);
        }

        private bool CancelTest(bool success, Func<Exception, RetryOrRethrow> outcomeTest, Exception exception, ref int retryCount)
        {
            if (success)
                return true;

            // have an exception, so apply the retry policy
            if (retryCount >= _maxRetries)
                throw new MaxRetriesExceededException(_maxRetries, exception);

            if (exception != null)
                if (outcomeTest(exception) == RetryOrRethrow.Rethrow)
                    throw exception;

            // didn't get an exit condition, so retry
            retryCount++;
            return false;
        }

        private static bool PerformOperation(Action operation, Func<bool> successCheck, out Exception exception)
        {
            try
            {
                operation();
                exception = null;
                return successCheck();
            }
            catch(Exception ex)
            {
                OurTrace.TraceVerbose(ex.ToLogString());
                exception = ex;
            }
            return false;
        }
    }

    [Serializable]
    public class MaxRetriesExceededException : Exception
    {
        public MaxRetriesExceededException(int maxRetries, Exception innerException) 
            : base(GetExceptionMessage(maxRetries), innerException)
        {

        }

        private static string GetExceptionMessage(int maxRetries)
        {
            return String.Format("Lost connection to Azure and was not able to re-establish connection after {0} retries.", maxRetries);
        }
    }

    public enum RetryOrRethrow
    {
        Retry,
        Rethrow,
    }
}
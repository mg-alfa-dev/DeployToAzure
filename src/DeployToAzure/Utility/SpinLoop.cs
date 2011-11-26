using System;
using System.Threading;

namespace DeployToAzure.Utility
{
    public static class SpinLoop
    {
        private static Action<int> _threadSleep = Thread.Sleep;
        
        public static void DoUntil(Action action, Func<bool> cancellationCheck, int sleepMilliseconds)
        {
            while(true)
            {
                action();
                if (cancellationCheck())
                    break;
                _threadSleep(sleepMilliseconds);
            }
        }

        public static void WaitUntil(Func<bool> cancellationCheck, int sleepMilliseconds)
        {
            DoUntil(() => { }, cancellationCheck, sleepMilliseconds);
        }

        public static IDisposable ForTests(Action<int> customSleepAction)
        {
            var oldSleep = _threadSleep;
            _threadSleep = customSleepAction;
            return new DisposeAction(() => _threadSleep = oldSleep);
        }
    }
}
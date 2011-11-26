using System;

namespace DeployToAzure.Utility
{
    public class DisposeAction: IDisposable
    {
        private readonly Action _compensatingAction;

        public DisposeAction(Action compensatingAction)
        {
            _compensatingAction = compensatingAction;
        }
            
        public void Dispose()
        {
            _compensatingAction();
        }
    }
}
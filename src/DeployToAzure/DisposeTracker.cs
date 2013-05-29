using System;
using System.Collections.Generic;
using System.Linq;

namespace DeployToAzure
{
    public class DisposeTracker: IDisposable
    {
        private readonly IList<IDisposable> _List = new List<IDisposable>();

        public void Track(IDisposable disposable)
        {
            _List.Add(disposable);
        }

        public void Dispose()
        {
            foreach(var item in _List.Reverse())
                item.Dispose();
        }
    }
}
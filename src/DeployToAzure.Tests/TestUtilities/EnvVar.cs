using System;

namespace DeployToAzure.Tests.TestUtilities
{
    public class EnvVar : IDisposable
    {
        private readonly string _name;
        private readonly string _existingValue;

        public EnvVar(string name, string value)
        {
            _name = name;
            _existingValue = Environment.GetEnvironmentVariable(name);
            Environment.SetEnvironmentVariable(name, value);
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable(_name, _existingValue);
        }
    }
}

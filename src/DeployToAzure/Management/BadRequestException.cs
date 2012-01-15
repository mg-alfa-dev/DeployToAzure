using System;

namespace DeployToAzure.Management
{
    public class BadRequestException : Exception
    {
        public BadRequestException(RequestUri requestUri, string content)
            : base(string.Format("BadRequest returned for operation '{0}': response: {1}", requestUri, content))
        {
        }
    }
}
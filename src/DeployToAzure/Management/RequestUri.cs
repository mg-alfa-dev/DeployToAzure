using System;

namespace DeployToAzure.Management
{
    public class RequestUri: IEquatable<RequestUri>
    {
        private readonly string _subscriptionId;
        private readonly string _requestId;

        public RequestUri(string subscriptionId, string requestId)
        {
            _requestId = requestId;
            _subscriptionId = subscriptionId;
        }

        public override string ToString()
        {
            return string.Format("https://management.core.windows.net/{0}/operations/{1}", _subscriptionId, _requestId);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == typeof(RequestUri) 
                && Equals((RequestUri)obj);
        }

        public bool Equals(RequestUri other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other._subscriptionId, _subscriptionId) && Equals(other._requestId, _requestId);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((_subscriptionId != null ? _subscriptionId.GetHashCode() : 0) * 397) ^ (_requestId != null ? _requestId.GetHashCode() : 0);
            }
        }

        public static bool operator ==(RequestUri left, RequestUri right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(RequestUri left, RequestUri right)
        {
            return !Equals(left, right);
        }
    }
}
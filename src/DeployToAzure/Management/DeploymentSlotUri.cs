namespace DeployToAzure.Management
{
    public class DeploymentSlotUri
    {
        private readonly string _subscriptionId;
        private readonly string _serviceName;
        private readonly string _slot;

        public DeploymentSlotUri(string subscriptionId, string serviceName, string slot)
        {
            _serviceName = serviceName;
            _slot = slot;
            _subscriptionId = subscriptionId;
        }

        public override string ToString()
        {
            return string.Format("https://management.core.windows.net/{0}/services/hostedservices/{1}/deploymentslots/{2}", _subscriptionId, _serviceName, _slot);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == typeof (DeploymentSlotUri) 
                && Equals((DeploymentSlotUri)obj);
        }

        private bool Equals(DeploymentSlotUri other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other._subscriptionId, _subscriptionId) && Equals(other._serviceName, _serviceName) && Equals(other._slot, _slot);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var result = (_subscriptionId != null ? _subscriptionId.GetHashCode() : 0);
                result = (result * 397) ^ (_serviceName != null ? _serviceName.GetHashCode() : 0);
                result = (result * 397) ^ (_slot != null ? _slot.GetHashCode() : 0);
                return result;
            }
        }

        public RequestUri ToRequestUri(string requestId)
        {
            return new RequestUri(_subscriptionId, requestId);
        }

        public static bool operator ==(DeploymentSlotUri left, DeploymentSlotUri right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(DeploymentSlotUri left, DeploymentSlotUri right)
        {
            return !Equals(left, right);
        }

        public string SubscriptionId { get { return _subscriptionId; } }
    }
}
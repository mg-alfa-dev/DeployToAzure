namespace DeployToAzure.Management
{
    public enum AzureDeploymentCheckOutcome
    {
        None,
        Failed,
        NotFound,
        Running,
        Suspended,
        Starting,
        Deploying,
        Suspending,
    }
}
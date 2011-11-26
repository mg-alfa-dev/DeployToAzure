using System.Net;
using DeployToAzure.Management;
using NUnit.Framework;

// ReSharper disable InconsistentNaming
namespace DeployToAzure.Tests.Management
{
    [TestFixture]
    public class AzureDeploymentLowLevelApi_BeginSuspend_Tests
    {
        public readonly DeploymentSlotUri TestDeploymentUri = new DeploymentSlotUri(subscriptionId: "subid", serviceName: "svcname", slot: "production");

        [Test]
        public void SendsCorrectPostArguments()
        {
            var http = new ScriptedHttpFake();

            http.Script.Add(() => http.NextResponse = new HttpResponse(HttpStatusCode.Accepted, ""));

            var api = new AzureManagementLowLevelApi(http);
            api.BeginSuspend(TestDeploymentUri);
            
            Assert.That(http.LastPostUri, Is.EqualTo(TestDeploymentUri + "/?comp=status"), "deploymentUri parameter incorrect");
            Assert.That(http.LastPostContent, Contains.Substring("<UpdateDeploymentStatus "), "is an updateDeploymentStatus request");
            Assert.That(http.LastPostContent, Contains.Substring("<Status>Suspended</Status>"), "has the appropriate status");
        }

        [Test]
        public void ThrowsOnAnythingOtherThanAccepted()
        {
            var http = new ScriptedHttpFake();

            http.Script.Add(() => http.NextResponse = new HttpResponse(HttpStatusCode.ServiceUnavailable, ""));

            var api = new AzureManagementLowLevelApi(http);
            Assert.That(() => api.BeginSuspend(TestDeploymentUri), Throws.TypeOf<UnhandledHttpException>());
        }

        [Test]
        public void Http409ConflictReturnsNull()
        {
            var http = new ScriptedHttpFake();

            http.Script.Add(() => http.NextResponse = new HttpResponse(HttpStatusCode.Conflict, ""));

            var api = new AzureManagementLowLevelApi(http);
            Assert.That(api.BeginSuspend(TestDeploymentUri), Is.Null, "RequestUri should be null on 409 conflict");
        }
    }
}
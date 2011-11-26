using System.Net;
using DeployToAzure.Management;
using NUnit.Framework;

// ReSharper disable InconsistentNaming
namespace DeployToAzure.Tests.Management
{
    [TestFixture]
    public class AzureDeploymentLowLevelApi_BeginDelete_Tests
    {
        public readonly DeploymentSlotUri TestDeploymentUri = new DeploymentSlotUri(subscriptionId: "subid", serviceName: "svcname", slot: "production");

        [Test]
        public void SendsCorrectDeleteUri()
        {
            var http = new ScriptedHttpFake();

            http.Script.Add(() => http.NextResponse = new HttpResponse(HttpStatusCode.Accepted, ""));

            var api = new AzureManagementLowLevelApi(http);
            api.BeginDelete(TestDeploymentUri);

            Assert.That(http.LastDeleteUri, Is.EqualTo(TestDeploymentUri.ToString()), "deploymentUri parameter incorrect");
        }

        [Test]
        public void ThrowsOnAnythingOtherThanAcceptedOrConflict()
        {
            var http = new ScriptedHttpFake();

            http.Script.Add(() => http.NextResponse = new HttpResponse(HttpStatusCode.ServiceUnavailable, ""));

            var api = new AzureManagementLowLevelApi(http);
            Assert.That(() => api.BeginDelete(TestDeploymentUri), Throws.TypeOf<UnhandledHttpException>());
        }

        [Test]
        public void Http409ConflictReturnsNull()
        {
            var http = new ScriptedHttpFake();

            http.Script.Add(() => http.NextResponse = new HttpResponse(HttpStatusCode.Conflict, ""));

            var api = new AzureManagementLowLevelApi(http);
            Assert.That(api.BeginDelete(TestDeploymentUri), Is.Null, "Should return null on 409 conflict");
        }
    }
}
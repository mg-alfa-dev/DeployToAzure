using System.Net;
using DeployToAzure.Management;
using NUnit.Framework;
using Rhino.Mocks;

// ReSharper disable InconsistentNaming

namespace DeployToAzure.Tests.Management
{
    [TestFixture]
    public class AzureDeploymentLowLevelApi_BeginCreateDeployment_Tests
    {
        public readonly DeploymentSlotUri TestDeploymentUri = new DeploymentSlotUri(subscriptionId: "subid", serviceName: "svcname", slot: "production");

        [Test]
        public void SendsCorrectArguments()
        {
            var http = new ScriptedHttpFake();

            http.Script.Add(() => http.NextResponse = new HttpResponse(HttpStatusCode.Accepted, "", "requestId"));

            var api = new AzureManagementLowLevelApi(http);

            var config = MockRepository.GenerateStub<IDeploymentConfiguration>();
            config.Stub(x => x.ToXmlString()).Return("foo");
            var requestUrl = api.BeginCreate(TestDeploymentUri, config);

            Assert.That(http.LastPostUri, Is.EqualTo(TestDeploymentUri.ToString()), "deploymentUri parameter incorrect");
            Assert.That(http.LastPostContent, Is.EqualTo("foo"), "expected xml was passed");

            var expectedRequestUri = TestDeploymentUri.ToRequestUri("requestId");
            Assert.That(requestUrl, Is.EqualTo(expectedRequestUri));
        }

        [Test]
        public void ThrowsOnAnythingOtherThanAcceptedAndConflict()
        {
            var http = new ScriptedHttpFake();

            http.Script.Add(() => http.NextResponse = new HttpResponse(HttpStatusCode.InternalServerError, ""));

            var api = new AzureManagementLowLevelApi(http);
            var config = MockRepository.GenerateStub<IDeploymentConfiguration>();
            Assert.That(
                () => api.BeginCreate(TestDeploymentUri, config), 
                Throws.TypeOf<UnhandledHttpException>());
        }

        [Test]
        public void Http409ConflictReturnsNull()
        {
            var http = new ScriptedHttpFake();

            http.Script.Add(() => http.NextResponse = new HttpResponse(HttpStatusCode.Conflict, ""));

            var api = new AzureManagementLowLevelApi(http);
            var config = MockRepository.GenerateStub<IDeploymentConfiguration>();
            Assert.That(api.BeginCreate(TestDeploymentUri, config), Is.Null, "Should return null on 409 conflict");
        }
    }
}
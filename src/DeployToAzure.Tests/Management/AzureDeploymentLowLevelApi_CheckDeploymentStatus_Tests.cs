using System.Net;
using DeployToAzure.Management;
using NUnit.Framework;

// ReSharper disable InconsistentNaming
namespace DeployToAzure.Tests.Management
{
    [TestFixture]
    public class AzureDeploymentLowLevelApi_CheckDeploymentStatus_Tests
    {
        public readonly DeploymentSlotUri TestDeploymentUri = new DeploymentSlotUri(subscriptionId: "subid", serviceName: "svcname", slot: "production");

        [Test]
        public void ReturnsRunningIfRunning()
        {
            var http = new ScriptedHttpFake();

            http.Script.Add(() => http.NextResponse = new HttpResponse(HttpStatusCode.OK, "<xml><Status>Running</Status></xml>"));

            var api = new AzureManagementLowLevelApi(http);
            var status = api.CheckDeploymentStatus(TestDeploymentUri);

            Assert.That(status, Is.EqualTo(AzureDeploymentCheckOutcome.Running));
        }

        [Test]
        public void ReturnsStartingIfStarting()
        {
            var http = new ScriptedHttpFake();

            http.Script.Add(() => http.NextResponse = new HttpResponse(HttpStatusCode.OK, "<xml><Status>Starting</Status></xml>"));

            var api = new AzureManagementLowLevelApi(http);
            var status = api.CheckDeploymentStatus(TestDeploymentUri);

            Assert.That(status, Is.EqualTo(AzureDeploymentCheckOutcome.Starting));
        }

        [Test]
        public void ReturnsSuspendingIfSuspending()
        {
            var http = new ScriptedHttpFake();

            http.Script.Add(() => http.NextResponse = new HttpResponse(HttpStatusCode.OK, "<xml><Status>Suspending</Status></xml>"));

            var api = new AzureManagementLowLevelApi(http);
            var status = api.CheckDeploymentStatus(TestDeploymentUri);

            Assert.That(status, Is.EqualTo(AzureDeploymentCheckOutcome.Suspending));
        }

        [Test]
        public void ReturnsRunningTransitioningIfRunningTransitioning()
        {
            var http = new ScriptedHttpFake();

            http.Script.Add(() => http.NextResponse = new HttpResponse(HttpStatusCode.OK, "<xml><Status>RunningTransitioning</Status></xml>"));

            var api = new AzureManagementLowLevelApi(http);
            var status = api.CheckDeploymentStatus(TestDeploymentUri);

            Assert.That(status, Is.EqualTo(AzureDeploymentCheckOutcome.RunningTransitioning));
        }

        [Test]
        public void ReturnsFailedIfStatusCodeNotOk()
        {
            var http = new ScriptedHttpFake();

            http.Script.Add(() => http.NextResponse = new HttpResponse(HttpStatusCode.RequestTimeout, ""));

            var api = new AzureManagementLowLevelApi(http);
            var status = api.CheckDeploymentStatus(TestDeploymentUri);

            Assert.That(status, Is.EqualTo(AzureDeploymentCheckOutcome.Failed));
        }

        [Test]
        public void ReturnsNotFoundIfStatusCode404()
        {
            var http = new ScriptedHttpFake();

            http.Script.Add(() => http.NextResponse = new HttpResponse(HttpStatusCode.NotFound, ""));

            var api = new AzureManagementLowLevelApi(http);
            var status = api.CheckDeploymentStatus(TestDeploymentUri);

            Assert.That(status, Is.EqualTo(AzureDeploymentCheckOutcome.NotFound));
        }

        [Test]
        public void ReturnsFailedOnWebExceptionsNotHandledByHttpGet()
        {
            var http = new ScriptedHttpFake();

            http.Script.Add(() => { throw new UnhandledHttpException(); });

            var api = new AzureManagementLowLevelApi(http);
            var status = api.CheckDeploymentStatus(TestDeploymentUri);

            Assert.That(status, Is.EqualTo(AzureDeploymentCheckOutcome.Failed));
        }

        [Test]
        public void ReturnsSuspendedIfSuspended()
        {
            var http = new ScriptedHttpFake();

            http.Script.Add(() => http.NextResponse = new HttpResponse(HttpStatusCode.OK, "<xml><Status>Suspended</Status></xml>"));

            var api = new AzureManagementLowLevelApi(http);
            var status = api.CheckDeploymentStatus(TestDeploymentUri);

            Assert.That(status, Is.EqualTo(AzureDeploymentCheckOutcome.Suspended));
        }

        [Test]
        public void SendsCorrectUri()
        {
            var http = new ScriptedHttpFake();

            http.Script.Add(() => http.NextResponse = new HttpResponse(HttpStatusCode.OK, "<xml><Status>Suspended</Status></xml>"));

            var api = new AzureManagementLowLevelApi(http);
            api.CheckDeploymentStatus(TestDeploymentUri);

            Assert.That(http.LastGetUri, Is.EqualTo(TestDeploymentUri.ToString()));
        }
    }
}
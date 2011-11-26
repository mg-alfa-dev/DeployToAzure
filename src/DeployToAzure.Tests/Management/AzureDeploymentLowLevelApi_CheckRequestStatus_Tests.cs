using System.Net;
using DeployToAzure.Management;
using NUnit.Framework;

namespace DeployToAzure.Tests.Management
{
    [TestFixture]
    public class AzureDeploymentLowLevelApi_CheckRequestStatus_Tests
    {
        readonly RequestUri TestUrl = new RequestUri(subscriptionId: "subId", requestId: "requestId");

        [Test]
        public void CheckRequestStatus_InProgress()
        {
            var http = new ScriptedHttpFake();
            http.Script.Add(() => http.NextResponse = new HttpResponse(HttpStatusCode.Accepted, "<Status>InProgress</Status>"));
            var api = new AzureManagementLowLevelApi(http);

            var requestStatus = api.CheckRequestStatus(TestUrl);
            
            Assert.That(requestStatus, Is.EqualTo(AzureRequestStatus.InProgress), "request status");
            Assert.That(http.LastGetUri, Is.EqualTo("https://management.core.windows.net/subId/operations/requestId"), "operation URI");
        }

        [Test]
        public void CheckRequestStatus_Succeeded()
        {
            var http = new ScriptedHttpFake();
            http.Script.Add(() => http.NextResponse = new HttpResponse(HttpStatusCode.Accepted, "<Status>Succeeded</Status>"));
            var api = new AzureManagementLowLevelApi(http);

            var requestStatus = api.CheckRequestStatus(TestUrl);

            Assert.That(requestStatus, Is.EqualTo(AzureRequestStatus.Succeeded), "request status");
            Assert.That(http.LastGetUri, Is.EqualTo("https://management.core.windows.net/subId/operations/requestId"), "operation URI");
        }

        [Test]
        public void CheckRequestStatus_Failed()
        {
            var http = new ScriptedHttpFake();
            http.Script.Add(() => http.NextResponse = new HttpResponse(HttpStatusCode.Accepted, "<Status>Failed</Status>"));
            var api = new AzureManagementLowLevelApi(http);

            var requestStatus = api.CheckRequestStatus(TestUrl);

            Assert.That(requestStatus, Is.EqualTo(AzureRequestStatus.Failed), "request status");
            Assert.That(http.LastGetUri, Is.EqualTo("https://management.core.windows.net/subId/operations/requestId"), "operation URI");
        }
    }
}
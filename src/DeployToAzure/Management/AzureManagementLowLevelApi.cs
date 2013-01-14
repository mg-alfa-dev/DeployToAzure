using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using DeployToAzure.Utility;
using JetBrains.Annotations;

namespace DeployToAzure.Management
{
    public enum AzureRequestStatus
    {
        InProgress = 1,
        Succeeded,
        Failed,
    }

    public class AzureManagementLowLevelApi : IAzureManagementLowLevelApi
    {
        private readonly IHttp _http;

        public AzureManagementLowLevelApi(IHttp http)
        {
            _http = http;
        }

        public AzureDeploymentCheckOutcome CheckDeploymentStatus(DeploymentSlotUri deploymentUri)
        {
            try
            {
                var response = _http.Get(deploymentUri.ToString());

                if (response.StatusCode == HttpStatusCode.NotFound)
                    return AzureDeploymentCheckOutcome.NotFound;
                if (response.StatusCode != HttpStatusCode.OK)
                    return AzureDeploymentCheckOutcome.Failed;
                var statusText = CrackStatusTextFromResponse(response);
                OurTrace.TraceVerbose("CheckDeploymentStatus: " + statusText);
                return TranslateStatusText(statusText);
            }
            catch(UnhandledHttpException)
            {
                return AzureDeploymentCheckOutcome.Failed;
            }
        }

        public AzureRequestStatus CheckRequestStatus(RequestUri requestUri)
        {
            var response = _http.Get(requestUri.ToString());
            var match = Regex.Match(response.Content, "<Status>(.*?)</Status>");
            FailFast.Unless(match.Success, "Expected regex match in response content: " + match + ".  Request URI:" + requestUri + ", Response content:" + response.Content);

            switch (match.Groups[1].Value)
            {
                case "InProgress":
                    return AzureRequestStatus.InProgress;
                case "Succeeded":
                    return AzureRequestStatus.Succeeded;
                case "Failed":
                    OurTrace.TraceError("CheckRequestStatus gave us a failure: " + response.Content);
                    var is400BadRequest = Regex.Match(response.Content, "<HttpStatusCode>400</HttpStatusCode><Error><Code>BadRequest</Code>");
                    if (is400BadRequest.Success)
                        throw new BadRequestException(requestUri, response.Content);
                    return AzureRequestStatus.Failed;
                default:
                    FailFast.WithMessage("Unexpected operation status: " + match.Groups[1].Value + ", for operation: " + requestUri);
                    // ReSharper disable HeuristicUnreachableCode
                    throw new InvalidOperationException("Shouldn't ever get here!");
                    // ReSharper restore HeuristicUnreachableCode
            }
        }

        public RequestUri BeginUpgrade(DeploymentSlotUri deploymentUri, DeploymentConfiguration configuration)
        {
            OurTrace.TraceVerbose("BeginUpgrade");
            var xml = configuration.MakeUpgradeDeploymentMessage();
            OurTrace.TraceInfo(xml);

            var response = _http.Post(deploymentUri + "/?comp=upgrade", xml);
            var statusCode = response.StatusCode;

            if (statusCode.IsAccepted())
                return deploymentUri.ToRequestUri(response.AzureRequestIdHeader);

            if (statusCode.IsConflict())
                return null;

            ThrowUnexpectedHttpResponse(response);
            return null; // can't be reached.
        }

        public RequestUri BeginSuspend(DeploymentSlotUri deploymentUri)
        {
            OurTrace.TraceVerbose("BeginSuspend");
            return ChangeStatus(deploymentUri, "Suspended");
        }

        public RequestUri BeginDelete(DeploymentSlotUri deploymentUri)
        {
            OurTrace.TraceVerbose("BeginDelete");
            var response = _http.Delete(deploymentUri.ToString());
            var statusCode = response.StatusCode;

            if (statusCode.IsAccepted())
                return deploymentUri.ToRequestUri(response.AzureRequestIdHeader);

            if (statusCode.IsConflict())
                return null;

            ThrowUnexpectedHttpResponse(response);
            return null; // can't be reached
        }

        public RequestUri BeginCreate(DeploymentSlotUri deploymentUri, IDeploymentConfiguration configuration)
        {
            OurTrace.TraceVerbose("BeginCreate");
            var xml = configuration.MakeCreateDeploymentMessage();
            OurTrace.TraceInfo(xml);

            var response = _http.Post(deploymentUri.ToString(), xml);
            var statusCode = response.StatusCode;

            if (statusCode.IsAccepted())
                return deploymentUri.ToRequestUri(response.AzureRequestIdHeader);

            if (statusCode.IsConflict())
                return null;

            ThrowUnexpectedHttpResponse(response);
            return null; // can't be reached.
        }

        public IEnumerable<string> ListStorageAccounts(string subscriptionId)
        {
            var uri = string.Format("https://management.core.windows.net/{0}/services/storageservices", subscriptionId);
            var response = _http.Get(uri);
            if(!response.StatusCode.IsOK())
                ThrowUnexpectedHttpResponse(response);

            XNamespace ns = "http://schemas.microsoft.com/windowsazure";
            var elName = ns + "ServiceName";
            var result = XDocument.Parse(response.Content);
            
            if(result.Root == null)
                ThrowUnexpectedHttpResponse(response);

            var services = result.Root.Descendants(elName);
            return services.Select(x => x.Value).ToArray();
        }

        public IEnumerable<string> GetStorageAccountKeys(string subscriptionId, string storageAccountName)
        {
            var uri = string.Format("https://management.core.windows.net/{0}/services/storageservices/{1}/keys",
                subscriptionId,
                storageAccountName);
            var response = _http.Get(uri);
            if(!response.StatusCode.IsOK())
                ThrowUnexpectedHttpResponse(response);

            XNamespace ns = "http://schemas.microsoft.com/windowsazure";
            var elName = ns + "StorageServiceKeys";
            var result = XDocument.Parse(response.Content);

            if(result.Root == null)
                ThrowUnexpectedHttpResponse(response);

            var keysParent = result.Root.Descendants(elName).First();
            return keysParent.Elements().Select(x => x.Value).ToArray();
        }

        private RequestUri ChangeStatus(DeploymentSlotUri deploymentUri, string newStatus)
        {
            OurTrace.TraceVerbose("Changing status to:" + newStatus);
            FailFast.Unless(newStatus == "Suspended" || newStatus == "Running",
                            "status must be 'Running' or 'Suspended'");

            var updateDeploymentUri = string.Format("{0}/?comp=status", deploymentUri);
            var suspendXml = BuildUpdateDeploymentStatusXml(newStatus);

            var response = _http.Post(updateDeploymentUri, suspendXml);
            var statusCode = response.StatusCode;

            if (statusCode.IsAccepted())
                return deploymentUri.ToRequestUri(response.AzureRequestIdHeader);

            if (statusCode.IsConflict())
                return null;
            
            ThrowUnexpectedHttpResponse(response);
            return null; // can't be reached.
        }
        
        [TerminatesProgram]
        private static void ThrowUnexpectedHttpResponse(HttpResponse response)
        {
            OurTrace.TraceVerbose("Throwing UnhandledHttpException: " + response.StatusCode + ", with content: " + response.Content);
            throw new UnhandledHttpException(
                string.Format("Unexpected Http Response! HttpStatusCode = {0}, Response = {1}", 
                              response.StatusCode, 
                              response.Content));
        }

        private static string BuildUpdateDeploymentStatusXml(string status)
        {
            var updateXml = string.Format(
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                        <UpdateDeploymentStatus xmlns=""http://schemas.microsoft.com/windowsazure"">
                            <Status>{0}</Status>
                        </UpdateDeploymentStatus>",
                status);
            return updateXml;
        }

        private static AzureDeploymentCheckOutcome TranslateStatusText(string statusText)
        {
            switch(statusText)
            {
                case "Running":
                    return AzureDeploymentCheckOutcome.Running;
                case "Suspending":
                    return AzureDeploymentCheckOutcome.Suspending;
                case "Starting":
                    return AzureDeploymentCheckOutcome.Starting;
                case "Suspended":
                    return AzureDeploymentCheckOutcome.Suspended;
                case "Deleting":
                    return AzureDeploymentCheckOutcome.Suspended;
                case "Deploying":
                    return AzureDeploymentCheckOutcome.Deploying;
                case "RunningTransitioning":
                    return AzureDeploymentCheckOutcome.RunningTransitioning;
                default:
                    throw new InvalidOperationException("Unexpected status text: " + statusText);
            }
        }

        private static string CrackStatusTextFromResponse(HttpResponse response)
        {
            var match = Regex.Match(response.Content, "<Status>(.*?)</Status>");
            return match.Groups[1].Value;
        }
    }
}
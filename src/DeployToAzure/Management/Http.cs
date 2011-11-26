using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Security.Cryptography.X509Certificates;
using DeployToAzure.Utility;

namespace DeployToAzure.Management
{
    public interface IHttp
    {
        HttpResponse Delete(string uri);
        HttpResponse Post(string uri, string content);
        HttpResponse Get(string uri);
    }

    [Serializable]
    public class UnhandledHttpException : Exception
    {
        public UnhandledHttpException()
        {
        }

        public UnhandledHttpException(string message) : base(message)
        {
        }

        public UnhandledHttpException(string message, Exception inner) : base(message, inner)
        {
        }

        protected UnhandledHttpException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }

    public class Http : IHttp
    {
        private readonly X509Certificate2 _certificate;

        public Http(X509Certificate2 certificate)
        {
            _certificate = certificate;
        }

        public HttpResponse Delete(string uri)
        {
            return Internal("DELETE", uri, null);
        }

        public HttpResponse Post(string uri, string content)
        {
            return Internal("POST", uri, content);
        }

        public HttpResponse Get(string uri)
        {
            return Internal("GET", uri, null);
        }

        private HttpResponse Internal(string method, string uri, string requestBody)
        {
            try
            {
                OurTrace.TraceInfo("method:" + method);
                OurTrace.TraceInfo("uri:" + uri);
                OurTrace.TraceInfo("requestBody:" + requestBody);
                var request = Request(uri, method);
                if(requestBody != null)
                {
                    using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                        streamWriter.Write(requestBody);
                }

                var response = (HttpWebResponse)request.GetResponse();
                var requestId = response.Headers["x-ms-request-id"];
                if (method == "POST" && requestBody != null && !string.IsNullOrEmpty(requestId))
                    OurTrace.TraceInfo("request-id: " + requestId + ", for post: " + requestBody);
                using (var str = response.GetResponseStream())
                {
                    FailFast.IfNull(str, "response stream");
                    var stringReader = new StreamReader(str);
                    return new HttpResponse(response.StatusCode, stringReader.ReadToEnd(), requestId);
                }
            }
            catch (WebException exception)
            {
                OurTrace.TraceError(exception.ToString());
                OurTrace.TraceError("message: " + exception.Message);

                var responseContent = GetResponseContent(exception);
                OurTrace.TraceError("response: " + responseContent);

                var httpWebResponse = exception.Response as HttpWebResponse;
                if(httpWebResponse != null)
                {
                    OurTrace.TraceError("response.StatusCode: " + httpWebResponse.StatusCode);
                    OurTrace.TraceError("response.StatusDescription: " + httpWebResponse.StatusDescription);
                    return new HttpResponse(httpWebResponse.StatusCode, responseContent);
                }
                throw new UnhandledHttpException("Unhandled Http exception.", exception);
            }
            catch (SocketException exception)
            {
                throw new UnhandledHttpException("Unhandled socket exception.", exception);
            }
        }

        private static string GetResponseContent(WebException exception)
        {
            var responseContent = "";
            if (exception.Response != null)
                using (var responseStream = exception.Response.GetResponseStream())
                {
                    FailFast.IfNull(responseStream, "response stream");
                    responseContent = new StreamReader(responseStream).ReadToEnd();
                }
            return responseContent;
        }

        private HttpWebRequest Request(string uri, string method)
        {
            var request = (HttpWebRequest)WebRequest.Create(uri);
            request.ClientCertificates.Add(_certificate);
            request.Headers.Add("x-ms-version", "2010-10-28");
            request.ContentType = "application/xml";
            request.Method = method;
            return request;
        }
    }

    public class HttpResponse
    {
        public readonly HttpStatusCode StatusCode;
        public readonly string Content;
        public readonly string AzureRequestIdHeader;

        public HttpResponse(HttpStatusCode statusCode, string content, string azureRequestIdHeader = null)
        {
            StatusCode = statusCode;
            Content = content;
            AzureRequestIdHeader = azureRequestIdHeader;
        }
    }

    public static class HttpStatusCodeExtensions
    {
        public static bool IsAccepted(this HttpStatusCode statusCode)
        {
            return statusCode == HttpStatusCode.Accepted;
        }

        public static bool IsConflict(this HttpStatusCode statusCode)
        {
            return statusCode == HttpStatusCode.Conflict;
        }
    }
}
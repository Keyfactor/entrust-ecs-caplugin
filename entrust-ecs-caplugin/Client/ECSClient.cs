using Keyfactor.Extensions.CAPlugin.Entrust.API;
using Keyfactor.Extensions.CAPlugin.Entrust.Models;
using Keyfactor.Logging;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;

using static Keyfactor.Extensions.CAPlugin.Entrust.API.VersionRequest;

using Certificate = Keyfactor.Extensions.CAPlugin.Entrust.API.Certificate;

namespace Keyfactor.Extensions.CAPlugin.Entrust.Client
{
    public class ECSClient
    {
        private static ILogger Logger => LogHandler.GetClassLogger<ECSClient>();

        private string UserName { get; set; }
        private string Password { get; set; }
        private string BaseUrl { get; set; }
        private X509Certificate2 AuthCert { get; set; }

        public ECSClient(string username, string password, X509Certificate2 authCert, string baseUrl)
        {
            UserName = username;
            Password = password;
            AuthCert = authCert;
            BaseUrl = baseUrl;
        }

        public ECSClient(string username, string password, X509Certificate2 authCert)
            : this(username, password, authCert, "https://api.entrust.net/enterprise/v2/")
        {
        }

        private class ECSResponse
        {
            public ECSResponse()
            {
                Success = true;
                Response = "";
            }

            public bool Success { get; set; }
            public string Response { get; set; }
        }

        private ECSResponse Request(ECSBaseRequest request)
        {
            return Request(request, "");
        }

        private ECSResponse Request(ECSBaseRequest request, string parameters)
        {
            ECSResponse response = new ECSResponse();
            bool rateLimited = true;
            int retryAfter = 0;

            while (rateLimited)
            {
                System.Threading.Thread.Sleep(retryAfter * 1000);
                try
                {
                    string targetUri;
                    if (request.Method == "POST" || request.Method == "PUT" || request.Method == "PATCH")
                    {
                        targetUri = BaseUrl + request.Resource;
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(parameters))
                        {
                            targetUri = BaseUrl + request.Resource;
                        }
                        else
                        {
                            targetUri = BaseUrl + request.Resource + "?" + parameters;
                        }
                    }
                    Logger.LogTrace($"Entered Entrust request method: {request.Method} - URL: {targetUri}");

                    HttpWebRequest objRequest = (HttpWebRequest)WebRequest.Create(targetUri);
                    objRequest.Method = request.Method;
                    objRequest.ContentType = "application/json";
                    objRequest.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(UserName + ":" + Password));
                    if (AuthCert != null)
                    {
                        objRequest.ClientCertificates.Add(AuthCert);
                    }


                    if (!String.IsNullOrEmpty(parameters) && (objRequest.Method == "POST" || objRequest.Method == "PUT" || objRequest.Method == "PATCH"))
                    {
                        byte[] postBytes = Encoding.UTF8.GetBytes(parameters);
                        objRequest.ContentLength = postBytes.Length;
                        Stream requestStream = objRequest.GetRequestStream();
                        requestStream.Write(postBytes, 0, postBytes.Length);
                        requestStream.Close();
                    }

                    Stopwatch watch = new Stopwatch();
                    watch.Start();

                    using (HttpWebResponse objResponse = (HttpWebResponse)objRequest.GetResponse())
                    {
                        response.Response = new StreamReader(objResponse.GetResponseStream()).ReadToEnd();

                        Logger.LogTrace($"Entrust API returned response {objResponse.StatusCode} ({response.Response.Length} characters) in {watch.ElapsedMilliseconds}ms");
                    }

                    Logger.LogTrace("Full Response Body: " + response.Response);
                    rateLimited = false;
                }
                catch (WebException wex)
                {
                    if (wex.Response != null)
                    {
                        using (HttpWebResponse errorResponse = (HttpWebResponse)wex.Response)
                        {
                            using (StreamReader reader = new StreamReader(errorResponse.GetResponseStream()))
                            {
                                response.Response = reader.ReadToEnd();
                                string retrySeconds = errorResponse.Headers["Retry-After"];

                                if (!Int32.TryParse(retrySeconds, out retryAfter))
                                {
                                    rateLimited = false;
                                }
                                else
                                {
                                    retryAfter += 1; // Add one second to ensure we're not losing a decimal place.
                                    Logger.LogTrace("Rate Limit exceeded. Resubmitting request after {0} seconds.", retryAfter);
                                }
                            }
                        }
                    }
                    else
                    {
                        Logger.LogError($"Entrust Response Error: {wex.Message}");
                        throw new Exception($"Unable to establish connection to Entrust web service: {wex.Message}", wex);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Entrust Response Error: {ex.Message}");
                    throw new Exception($"Unable to establish connection to Entrust web service: {ex.Message}", ex);
                }
            }

            return response;
        }

        private bool IsError(string response)
        {
            return response.Contains("errors");
        }

        public VersionResponse GetApplicationVersion()
        {
            VersionRequest oRequest = new VersionRequest();
            ECSResponse oResponse = Request(oRequest, oRequest.BuildParameters());
            VersionResponse response;

            if (IsError(oResponse.Response))
            {
                ErrorResponse e = JsonConvert.DeserializeObject<ErrorResponse>(oResponse.Response);
                Logger.LogError($"Error occurred requesting application version from the Entrust REST API - {e.Errors.First().Message}");
                throw new Exception(e.Errors.First<Error>().Message);
            }
            else
            {
                response = JsonConvert.DeserializeObject<VersionResponse>(oResponse.Response);
            }

            return response;
        }

        public List<Organization> GetOrganizations()
        {
            GetOrganizationsRequest request = new GetOrganizationsRequest();
            ECSResponse apiResponse = Request(request, string.Empty);

            if (IsError(apiResponse.Response))
            {
                ErrorResponse e = JsonConvert.DeserializeObject<ErrorResponse>(apiResponse.Response);

                Logger.LogError($"Error occurred requesting organizations from the Entrust REST API: {e.Errors.First().Message}");

                throw new Exception(e.Errors.First().Message);
            }
            else
            {
                GetOrganizationsResponse response = JsonConvert.DeserializeObject<GetOrganizationsResponse>(apiResponse.Response);
                return response.Organizations;
            }
        }

        public List<ClientInfo> GetClients()
        {
            GetClientsRequest oRequest = new GetClientsRequest();
            ECSResponse oResponse = Request(oRequest, oRequest.BuildParameters());
            GetClientsResponse response;

            if (IsError(oResponse.Response))
            {
                ErrorResponse e = JsonConvert.DeserializeObject<ErrorResponse>(oResponse.Response);

                Logger.LogError($"Error occurred requesting client list from the Entrust REST API - {e.Errors.First().Message}");

                throw new Exception(e.Errors.First().Message);
            }
            else
            {
                response = JsonConvert.DeserializeObject<GetClientsResponse>(oResponse.Response);
            }

            return response.Clients;
        }

        public List<Certificate> GetAllCertificates()
        {
            List<Certificate> result = new List<Certificate>();
            int limit = 1000;
            int received = 0;
            int? total = 0;
            bool requestStarted = false;

            while (!requestStarted || received != total)
            {
                GetCertificatesRequest oRequest = new GetCertificatesRequest(limit, received);
                ECSResponse oResponse = Request(oRequest, oRequest.BuildParameters());
                GetCertificatesResponse response;

                if (IsError(oResponse.Response))
                {
                    ErrorResponse e = JsonConvert.DeserializeObject<ErrorResponse>(oResponse.Response);
                    Logger.LogError($"Error occurred requesting certificate list from Entrust REST API - {e.Errors.First().Message}");
                    throw new Exception(e.Errors.First().Message);
                }
                else
                {
                    response = JsonConvert.DeserializeObject<GetCertificatesResponse>(oResponse.Response);
                    total = response.summary.Total;
                    received += response.certificates.Count;
                    result.AddRange(response.certificates);
                }
                requestStarted = true;
            }

            return result;
        }

        public CertificateExt GetCertificateByTrackingId(int trackingId)
        {
            GetCertificateByTrackingIdRequest oRequest = new GetCertificateByTrackingIdRequest(trackingId);
            ECSResponse oResponse = Request(oRequest, oRequest.BuildParameters());
            CertificateExt response;

            if (IsError(oResponse.Response))
            {
                ErrorResponse e = JsonConvert.DeserializeObject<ErrorResponse>(oResponse.Response);
                Logger.LogError($"Error occurred requesting certificate for trackingId {trackingId} from the Entrust REST API - Error status code {e.Status} : {e.Errors.First().Message}");
                throw new Exception(e.Errors.First().Message);
            }
            else
            {
                response = JsonConvert.DeserializeObject<CertificateExt>(oResponse.Response);
            }

            return response;
        }

        public CertificateExt GetCertificateByThumbprint(string thumbprint)
        {
            GetCertificateByThumbprintRequest oRequest = new GetCertificateByThumbprintRequest(thumbprint);
            ECSResponse oResponse = Request(oRequest, oRequest.BuildParameters());
            CertificateExt response;

            if (IsError(oResponse.Response))
            {
                ErrorResponse e = JsonConvert.DeserializeObject<ErrorResponse>(oResponse.Response);
                Logger.LogError("Error occurred requesting certificate for thumbprint {0} from the Entrust REST API - Error status code {1} : {2}", thumbprint, e.Status, e.Errors.First().Message);
                throw new Exception(e.Errors.First().Message);
            }
            else
            {
                response = JsonConvert.DeserializeObject<CertificateExt>(oResponse.Response);
            }

            return response;
        }

        public CertificateResponse RequestNewCertificate(NewCertificateRequest request)
        {
            NewCertificateCall call = new NewCertificateCall();
            ECSResponse oResponse = Request(call, JsonConvert.SerializeObject(request, Newtonsoft.Json.Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            CertificateResponse response;

            if (IsError(oResponse.Response))
            {
                ErrorResponse e = JsonConvert.DeserializeObject<ErrorResponse>(oResponse.Response);
                Logger.LogError($"Error occurred requesting new certificate from Entrust REST API - {e.Errors.First().Message}");
                throw new Exception(e.Errors.First().Message);
            }
            else
            {
                response = JsonConvert.DeserializeObject<CertificateResponse>(oResponse.Response);
            }

            return response;
        }

        public CertificateResponse ReissueCertificate(ReissueCertificateRequestBody request, int trackingId)
        {
            ECSResponse oResponse = Request(new ReissueCertificateRequest(trackingId), JsonConvert.SerializeObject(request, Newtonsoft.Json.Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            CertificateResponse response;

            if (IsError(oResponse.Response))
            {
                ErrorResponse e = JsonConvert.DeserializeObject<ErrorResponse>(oResponse.Response);
                Logger.LogError($"Error occurred reissuing certificate with trackingId {trackingId} from Entrust REST API - {e.Errors.First().Message}");
                throw new Exception(e.Errors.First().Message);
            }
            else
            {
                response = JsonConvert.DeserializeObject<CertificateResponse>(oResponse.Response);
            }

            return response;
        }

        public CertificateResponse RenewCertificate(RenewCertificateRequestBody request, int trackingId)
        {
            ECSResponse oResponse = Request(new RenewCertificateRequest(trackingId), JsonConvert.SerializeObject(request, Newtonsoft.Json.Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            CertificateResponse response;

            if (IsError(oResponse.Response))
            {
                ErrorResponse e = JsonConvert.DeserializeObject<ErrorResponse>(oResponse.Response);
                Logger.LogError($"Error occurred renewing certificate with trackingId {trackingId} from Entrust REST API - {e.Errors.First().Message}");
                throw new Exception(e.Errors.First().Message);
            }
            else
            {
                response = JsonConvert.DeserializeObject<CertificateResponse>(oResponse.Response);
            }

            return response;
        }

        public ValueTuple<bool, string> ValidateRequestNewCertificate(NewCertificateRequest request)
        {
            // We switch the value here so that callers don't have to create a new request.
            bool? originalValue = request.ValidateOnly;
            request.ValidateOnly = true;
            ECSResponse oResponse = Request(new NewCertificateCall(), JsonConvert.SerializeObject(request, Newtonsoft.Json.Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            request.ValidateOnly = originalValue;

            if (IsError(oResponse.Response))
            {
                ErrorResponse response = JsonConvert.DeserializeObject<ErrorResponse>(oResponse.Response);
                Error requestError = response.Errors[0];
                return (false, requestError.Message);
            }
            return (true, oResponse.Response);
        }

        public Certificate GetCertificateBySerialNumber(string serialNumber)
        {
            string trimmedSerialNumber = serialNumber.TrimStart('0');
            List<Certificate> result = new List<Certificate>();

            Dictionary<string, string> qParams = new Dictionary<string, string>();
            qParams.Add("serialNumber", trimmedSerialNumber);
            GetCertificatesRequest oRequest = new GetCertificatesRequest(1, 0, qParams);
            ECSResponse oResponse = Request(oRequest, oRequest.BuildParameters());
            GetCertificatesResponse response;

            if (IsError(oResponse.Response))
            {
                ErrorResponse e = JsonConvert.DeserializeObject<ErrorResponse>(oResponse.Response);
                if (e.Status == 404)
                {
                    return null;
                }
                Logger.LogError($"Error occurred requesting certificate with serial number {trimmedSerialNumber} from Entrust REST API - {e.Errors.First().Message}");
                throw new Exception(e.Errors.First().Message);
            }
            else
            {
                response = JsonConvert.DeserializeObject<GetCertificatesResponse>(oResponse.Response);
            }

            if (response.certificates.Count > 0)
            {
                return response.certificates[0];
            }
            else
            {
                return null;
            }
        }

        public void RevokeCertificate(int trackingId, string reason, string comment)
        {
            RevokeCertificateCall oRequest = new RevokeCertificateCall(trackingId);
            RevokeCertificateRequest request = new RevokeCertificateRequest()
            {
                CrlReason = reason,
                RevocationComment = comment
            };

            ECSResponse oResponse = Request(oRequest, JsonConvert.SerializeObject(request, Newtonsoft.Json.Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));

            if (IsError(oResponse.Response))
            {
                ErrorResponse e = JsonConvert.DeserializeObject<ErrorResponse>(oResponse.Response);
                Logger.LogError($"Error occurred revoking certificate with trackingId {trackingId} from Entrust REST API - {e.Errors.First().Message}");
                throw new Exception(e.Errors.First().Message);
            }
        }

        public List<InventoryItem> GetInventories()
        {
            GetInventoryRequest oRequest = new GetInventoryRequest();
            ECSResponse oResponse = Request(oRequest, oRequest.BuildParameters());
            GetInventoryResponse response;

            if (IsError(oResponse.Response))
            {
                ErrorResponse e = JsonConvert.DeserializeObject<ErrorResponse>(oResponse.Response);
                Logger.LogError($"Error occurred requesting inventory from Entrust REST API - {e.Errors.First().Message}");
                throw new Exception(e.Errors.First().Message);
            }
            else
            {
                response = JsonConvert.DeserializeObject<GetInventoryResponse>(oResponse.Response);
            }

            return response.Inventories;
        }

        public CertificateResponse ApproveCertificate(int trackingId)
        {
            PatchCertificateRequest oRequest = new PatchCertificateRequest(trackingId);
            PatchCertificateRequestBody body = new PatchCertificateRequestBody()
            {
                Operation = CertificateOperation.APPROVE
            };
            ECSResponse oResponse = Request(oRequest, JsonConvert.SerializeObject(body, Newtonsoft.Json.Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            CertificateResponse response;

            if (IsError(oResponse.Response))
            {
                ErrorResponse e = JsonConvert.DeserializeObject<ErrorResponse>(oResponse.Response);
                Logger.LogError($"Error occurred approving certificate with trackingId {trackingId} from Entrust REST API - {e.Errors.First().Message}");
                throw new Exception(e.Errors.First().Message);
            }
            else
            {
                response = JsonConvert.DeserializeObject<CertificateResponse>(oResponse.Response);
            }

            return response;
        }

        public static ECSClient InitializeClient(ECSConfig config)
        {
            Logger.MethodEntry(LogLevel.Debug);
            X509Certificate2 clientCert = null;
            if (!string.IsNullOrEmpty(config.ClientCertificate.Thumbprint))
            {
                //Cert auth, cert in Windows store
                StoreName sn;
                StoreLocation sl;
                string thumbprint = config.ClientCertificate.Thumbprint;

                if (string.IsNullOrEmpty(thumbprint) ||
                    !Enum.TryParse(config.ClientCertificate.StoreName, out sn) ||
                    !Enum.TryParse(config.ClientCertificate.StoreLocation, out sl))
                {
                    throw new Exception("Unable to find client authentication certificate");
                }

                X509Certificate2Collection foundCerts;
                using (X509Store currentStore = new X509Store(sn, sl))
                {
                    Logger.LogTrace($"Search for client auth certificates with Thumprint {thumbprint} in the {sn}{sl} certificate store");

                    currentStore.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
                    foundCerts = currentStore.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, true);
                    Logger.LogTrace($"Found {foundCerts.Count} certificates in the {currentStore.Name} store");
                    currentStore.Close();
                }
                if (foundCerts.Count > 1)
                {
                    throw new Exception($"Multiple certificates with Thumprint {thumbprint} found in the {sn}{sl} certificate store");
                }
                if (foundCerts.Count > 0)
                    clientCert = foundCerts[0];
            }
            else if (!string.IsNullOrEmpty(config.ClientCertificate.CertificatePath))
            {
                //Cert auth, cert in pfx file
                try
                {
                    X509Certificate2 cert = new X509Certificate2(config.ClientCertificate.CertificatePath, config.ClientCertificate.CertificatePassword);
                    clientCert = cert;
                }
                catch (Exception ex)
                {
                    throw new Exception($"Unable to open the client certificate file with the given password. Error: {ex.Message}");
                }
            }

            return new ECSClient(config.AuthUsername, config.AuthPassword, clientCert);
        }
    }
}

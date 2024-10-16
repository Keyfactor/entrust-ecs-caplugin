using Keyfactor.AnyGateway.Extensions;
using Keyfactor.Extensions.CAPlugin.Entrust.API;
using Keyfactor.Extensions.CAPlugin.Entrust.Client;
using Keyfactor.Extensions.CAPlugin.Entrust.Models;
using Keyfactor.Logging;
using Keyfactor.PKI.Enums.EJBCA;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using Org.BouncyCastle.Asn1.X509;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Keyfactor.PKI.PKIConstants.Microsoft;

namespace Keyfactor.Extensions.CAPlugin.Entrust
{
    public class ECSCAPlugin : IAnyCAPlugin
    {
        private ECSConfig _config;
        private readonly ILogger _logger;
        private ICertificateDataReader _certificateDataReader;

        public ECSCAPlugin()
        {
            _logger = LogHandler.GetClassLogger<ECSCAPlugin>();
        }

        public void Initialize(IAnyCAPluginConfigProvider configProvider, ICertificateDataReader certificateDataReader)
        {
            _certificateDataReader = certificateDataReader;
            string rawConfig = JsonConvert.SerializeObject(configProvider.CAConnectionData);
            _config = JsonConvert.DeserializeObject<ECSConfig>(rawConfig);
        }

        /// <summary>
        /// Enroll for a certificate
        /// </summary>
        /// <param name="csr">The CSR for the certificate request</param>
        /// <param name="subject">The subject string</param>
        /// <param name="san">The list of SANs</param>
        /// <param name="productInfo">Collection of product information and options. Includes both product-level config options as well as custom enrollment fields.</param>
        /// <param name="requestFormat">The format of the request</param>
        /// <param name="enrollmentType">The type of enrollment (new, renew, reissue)</param>
        /// <returns>The result of the enrollment</returns>
        /// <exception cref="Exception"></exception>
        public async Task<EnrollmentResult> Enroll(string csr, string subject, Dictionary<string, string[]> san, EnrollmentProductInfo productInfo, RequestFormat requestFormat, EnrollmentType enrollmentType)
        {
            ECSClient client = ECSClient.InitializeClient(_config);
            _logger.LogTrace("Entrust Client Created");
            X509Name subjectParsed = new X509Name(subject);
            _logger.LogTrace($"Parsed Subject with {subject}");

            string underscoreErrorMessage = "Underscore is not allowed in DNSName.";
            string requestEmail;
            string requestNumber;
            string requestName;
            string commonName = "";
            string organization = "";
            string checkingSanVariable = "";
            int trackingId = 0;
            int clientId = -1;

            // Check tracking ID if we're doing a renewal or reissuance.
            if (enrollmentType == EnrollmentType.Reissue || enrollmentType == EnrollmentType.Renew || enrollmentType == EnrollmentType.RenewOrReissue)
            {
                _logger.LogTrace("This is a renew or reissue");
                trackingId = GetTrackingId(productInfo);
                _logger.LogTrace($"With trackingId {trackingId}");
                // Check now if the trackingId is 0 to fail early.
                if (trackingId == 0)
                {
                    throw new Exception("The tracking ID of the certificate requested for renewal or reissue is 0. This certificate must be renewed or reissued through the Entrust portal.");
                }
            }

            try
            {
                checkingSanVariable = "Common Name";
                string cn = subjectParsed.GetValueList(X509Name.CN).Cast<string>().LastOrDefault();
                if (!string.IsNullOrEmpty(cn))
                {
                    if (cn.Contains("_"))
                    {
                        throw new Exception(underscoreErrorMessage);
                    }
                    commonName = cn;
                }

                _logger.LogTrace($"Common Name of {commonName}");

                checkingSanVariable = "Organization";
                string org = subjectParsed.GetValueList(X509Name.O).Cast<string>().LastOrDefault();
                if (productInfo.ProductParameters.ContainsKey(Constants.Config.ORGANIZATION) && !string.IsNullOrEmpty(productInfo.ProductParameters[Constants.Config.ORGANIZATION]))
                {
                    organization = productInfo.ProductParameters[Constants.Config.ORGANIZATION];
                }
                else if (!string.IsNullOrEmpty(org))
                {
                    organization = org;
                }

                _logger.LogTrace($"Organization of {organization}");

                checkingSanVariable = "Email";
                string subjectEmail = subjectParsed.GetValueList(X509Name.EmailAddress).Cast<string>().LastOrDefault();
                if (productInfo.ProductParameters.ContainsKey(Constants.Config.EMAIL) && !string.IsNullOrEmpty(productInfo.ProductParameters[Constants.Config.EMAIL]))
                {
                    requestEmail = productInfo.ProductParameters[Constants.Config.EMAIL];
                }
                else if (!string.IsNullOrEmpty(subjectEmail))
                {
                    requestEmail = subjectEmail;
                }
                else if (!string.IsNullOrEmpty(_config.Email))
                {
                    requestEmail = _config.Email;
                }
                else
                {
                    requestEmail = "email@email.invalid";
                }

                _logger.LogTrace($"Email of {requestEmail}");

                checkingSanVariable = "Telephone Number";
                if (productInfo.ProductParameters.ContainsKey(Constants.Config.PHONE) && !string.IsNullOrEmpty(productInfo.ProductParameters[Constants.Config.PHONE]))
                {
                    requestNumber = productInfo.ProductParameters[Constants.Config.PHONE];
                }
                else if (!string.IsNullOrEmpty(_config.PhoneNumber))
                {
                    requestNumber = _config.PhoneNumber;
                }
                else
                {
                    requestNumber = "0000000000";
                }

                _logger.LogTrace($"Telephone Number of {requestNumber}");

                checkingSanVariable = "Name";
                if (productInfo.ProductParameters.ContainsKey(Constants.Config.NAME) && !string.IsNullOrEmpty(productInfo.ProductParameters[Constants.Config.NAME]))
                {
                    requestName = productInfo.ProductParameters[Constants.Config.NAME];
                }
                else if (!string.IsNullOrEmpty(_config.Name))
                {
                    requestName = _config.Name;
                }
                else
                {
                    requestName = "TestUser";
                }

                _logger.LogTrace($"Name of {requestName}");
            }
            catch (Exception ex)
            {
                if (ex.Message == underscoreErrorMessage)
                {
                    _logger.LogError($"Error occurred trying to validate the SAN information. {ex.Message}");
                    throw new Exception(ex.Message);
                }
                else
                {
                    _logger.LogError($"Error occurred trying to validate the request information. Required attributes {checkingSanVariable} may be missing.");
                    throw new Exception("Error occurred trying to validate the request information. Required attributes " + checkingSanVariable + " may be missing.");
                }
            }

            List<string> dnsNames = new List<string>();
            if (san.ContainsKey("Dns"))
            {
                dnsNames = new List<string>(san["Dns"]);
                _logger.LogTrace($"First DNS SAN: {dnsNames[0]}");
            }

            if (!commonName.Contains('.'))
            {
                throw new Exception($"Domain cannot be determined from Common Name.");
            }

            IEnumerable<Organization> approvedOrgs = client.GetOrganizations().Where(x => x.VerificationStatus.Equals("APPROVED", StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrEmpty(organization)) // If the organization is empty, use the default client.
            {
                clientId = 1;
            }
            else
            {
                Organization org = approvedOrgs.FirstOrDefault(x => x.Name.Equals(organization, StringComparison.OrdinalIgnoreCase));
                if (org != null)
                {
                    clientId = org.ClientId;
                }
            }

            _logger.LogTrace($"ClientId of {clientId}");

            if (clientId == -1)
            {
                throw new Exception($"Organization {organization} is not a valid Entrust organization for this account. The following organizations are approved: {string.Join(", ", approvedOrgs.Select(x => x.Name))}.");
            }

            string usageType = (productInfo.ProductParameters.ContainsKey("CertificateUsage")) ? productInfo.ProductParameters["CertificateUsage"] : "";
            _logger.LogTrace($"usageType of {usageType}");
            string eku = "";
            if (usageType.Equals("SERVERCLIENT", StringComparison.OrdinalIgnoreCase))
            {
                eku = "SERVER_AND_CLIENT_AUTH";
            }
            else if (usageType.Equals("SERVER", StringComparison.OrdinalIgnoreCase))
            {
                eku = "SERVER_AUTH";
            }
            else if (usageType.Equals("CLIENT", StringComparison.OrdinalIgnoreCase))
            {
                eku = "CLIENT_AUTH";
            }
            else
            {
                eku = "";
            }

            _logger.LogTrace($"Getting Tracking Info");
            Tracking trackingInfo = new Tracking()
            {
                TrackingInfo = "",
                RequesterEmail = requestEmail,
                RequesterName = requestName,
                RequesterPhone = requestNumber,
                Deactivated = false
            };
            _logger.LogTrace($"Got Tracking Info");

            if (!EntrustCertType.InventoryExists(client, productInfo.ProductID))
            {
                _logger.LogError($"Inventory for certificate type '{productInfo.ProductID}' has been used up. To perform the operation, revoke existing certificates or contact Entrust to acquire new inventory.");
                throw new Exception($"Inventory for certificate type '{productInfo.ProductID}' has been used up. To perform the operation, revoke existing certificates or contact Entrust to acquire new inventory.");
            }

            var months = (productInfo.ProductParameters.ContainsKey("Lifetime")) ? int.Parse(productInfo.ProductParameters["Lifetime"]) : 12;

            _logger.LogTrace($"Months of {months}");
            if (enrollmentType == EnrollmentType.RenewOrReissue)
            {
                _logger.LogTrace($"Determining if request is a renew or a reissue");
                var priorCertSnString = productInfo.ProductParameters["PriorCertSN"];
                int renewalWindowDays = productInfo.ProductParameters.ContainsKey("RenewalWindowDays") ? int.Parse(productInfo.ProductParameters["RenewalWindowDays"]) : 90;
                var reqId = _certificateDataReader.GetRequestIDBySerialNumber(priorCertSnString).Result;
                if (string.IsNullOrEmpty(reqId))
                {
                    throw new Exception($"No certificate with serial number '{priorCertSnString}' could be found.");
                }
                var expDate = _certificateDataReader.GetExpirationDateByRequestId(reqId);

                var renewCutoff = DateTime.Now.AddDays(renewalWindowDays * -1);

                if (expDate > renewCutoff)
                {
                    _logger.LogTrace($"Certificate with serial number {priorCertSnString} is within renewal window");
                    enrollmentType = EnrollmentType.Renew;
                }
                else
                {
                    _logger.LogTrace($"Certificate with serial number {priorCertSnString} is not within renewal window. Reissuing...");
                    enrollmentType = EnrollmentType.Reissue;
                }
            }

            _logger.LogTrace($"Switch Statement for Enrollment Type of {enrollmentType}");
            CertificateResponse response;
            switch (enrollmentType)
            {
                case EnrollmentType.New:

                    _logger.LogTrace($"Csr is {csr}");
                    _logger.LogTrace($"ClientId is {clientId}");
                    _logger.LogTrace($"Org is {organization}");
                    _logger.LogTrace($"CertType is {productInfo.ProductID.ToUpper()}");
                    _logger.LogTrace($"CertExpiryDate is {DateTime.Now.AddMonths(months)}");
                    _logger.LogTrace($"CertLifetime is {"P" + Math.Round(months / 12.0).ToString() + "Y"}");
                    _logger.LogTrace($"Tracking is {trackingInfo}");
                    _logger.LogTrace($"QueueForApproval is false");
                    _logger.LogTrace($"CertEmail is {requestEmail}");
                    _logger.LogTrace($"SubjectAltName is {(dnsNames.Count > 0 ? dnsNames[0] : "empty")}");
                    _logger.LogTrace($"Password is ''");
                    _logger.LogTrace($"SigningAlg is SHA-2");
                    _logger.LogTrace($"Eku is {eku}");
                    _logger.LogTrace($"Cn is {commonName}");
                    _logger.LogTrace($"Upn is {requestEmail}");
                    _logger.LogTrace($"Ou is empty string list");
                    _logger.LogTrace($"EndUserKeyStorageAgreement is true");
                    _logger.LogTrace($"ValidateOnly is false");

                    NewCertificateRequest request = new NewCertificateRequest()
                    {
                        Csr = csr,
                        ClientId = clientId,
                        Org = organization,
                        CertType = productInfo.ProductID.ToUpper(),
                        CertExpiryDate = DateTime.Now.AddMonths(months),
                        CertLifetime = "P" + Math.Round(months / 12.0).ToString() + "Y",
                        Tracking = trackingInfo,
                        QueueForApproval = false,
                        CertEmail = requestEmail,
                        SubjectAltName = dnsNames,
                        Password = "",
                        SigningAlg = "SHA-2",
                        Eku = eku,
                        Cn = commonName,
                        //email from userInfo
                        Upn = requestEmail,
                        Ou = new List<string>(),
                        EndUserKeyStorageAgreement = true,
                        //When true, this causes the api to only validate the submitted info and not actually register a cert.
                        ValidateOnly = false
                    };
                    _logger.LogTrace($"Before Validation Request: {JsonConvert.SerializeObject(request)}");
                    (bool validResponse, string messageResponse) = client.ValidateRequestNewCertificate(request);
                    _logger.LogTrace($"ValidResponse?: {validResponse}");
                    _logger.LogTrace($"messageResponse: {messageResponse}");

                    if (!validResponse)
                    {
                        _logger.LogError($"Request validation failed. {messageResponse}");
                        throw new Exception($"Request validation failed. {messageResponse}");
                    }

                    response = client.RequestNewCertificate(request);
                    _logger.LogTrace($"New Cert Request Response: {JsonConvert.SerializeObject(response)}");
                    break;

                case EnrollmentType.Reissue:

                    _logger.LogTrace($"Csr is {csr}");
                    _logger.LogTrace($"ClientId is {clientId}");
                    _logger.LogTrace($"Org is {organization}");
                    _logger.LogTrace($"Tracking is {trackingInfo}");
                    _logger.LogTrace($"CertEmail is {requestEmail}");
                    _logger.LogTrace($"SubjectAltName is {(dnsNames.Count > 0 ? dnsNames[0] : "empty")}");
                    _logger.LogTrace($"Password is ''");
                    _logger.LogTrace($"SigningAlg is SHA-2");
                    _logger.LogTrace($"Eku is {eku}");
                    _logger.LogTrace($"Cn is {commonName}");
                    _logger.LogTrace($"Upn is {requestEmail}");
                    _logger.LogTrace($"Ou is empty string list");
                    _logger.LogTrace($"EndUserKeyStorageAgreement is true");

                    ReissueCertificateRequestBody reissueRequest = new ReissueCertificateRequestBody()
                    {
                        Csr = csr,
                        ClientId = clientId,
                        Org = string.Empty,
                        Tracking = trackingInfo,
                        CertEmail = requestEmail,
                        SubjectAltName = dnsNames,
                        Password = string.Empty,
                        SigningAlg = "SHA-2",
                        Eku = eku,
                        Cn = commonName,
                        //email from userInfo
                        Upn = requestEmail,
                        Ou = new List<string>(),
                        EndUserKeyStorageAgreement = true,
                    };
                    _logger.LogTrace($"reissueRequest:  {JsonConvert.SerializeObject(reissueRequest)}");
                    response = client.ReissueCertificate(reissueRequest, trackingId);
                    _logger.LogTrace($"reissueResponse:  {JsonConvert.SerializeObject(response)}");
                    break;

                case EnrollmentType.Renew:

                    _logger.LogTrace($"Csr is {csr}");
                    _logger.LogTrace($"ClientId is {clientId}");
                    _logger.LogTrace($"Org is {organization}");
                    _logger.LogTrace($"CertExpiryDate is {DateTime.Now.AddMonths(months)}");
                    _logger.LogTrace($"CertLifetime is {"P" + Math.Round(months / 12.0).ToString() + "Y"}");
                    _logger.LogTrace($"Tracking is {trackingInfo}");
                    _logger.LogTrace($"CertEmail is {requestEmail}");
                    _logger.LogTrace($"SubjectAltName is {(dnsNames.Count > 0 ? dnsNames[0] : "empty")}");
                    _logger.LogTrace($"Password is ''");
                    _logger.LogTrace($"SigningAlg is SHA-2");
                    _logger.LogTrace($"Eku is {eku}");
                    _logger.LogTrace($"Cn is {commonName}");
                    _logger.LogTrace($"Upn is {requestEmail}");
                    _logger.LogTrace($"Ou is empty string list");
                    _logger.LogTrace($"EndUserKeyStorageAgreement is true");

                    RenewCertificateRequestBody renewRequest = new RenewCertificateRequestBody()
                    {
                        Csr = csr,
                        ClientId = clientId,
                        Org = "",
                        CertExpiryDate = DateTime.Now.AddMonths(months),
                        CertLifetime = "P" + Math.Round(months / 12.0).ToString() + "Y",
                        Tracking = trackingInfo,
                        CertEmail = requestEmail,
                        SubjectAltName = dnsNames,
                        Password = "",
                        SigningAlg = "SHA-2",
                        Eku = eku,
                        Cn = commonName,
                        //email from userInfo
                        Upn = requestEmail,
                        Ou = new List<string>(),
                        EndUserKeyStorageAgreement = true,
                    };
                    _logger.LogTrace($"reissueRequest:  {JsonConvert.SerializeObject(renewRequest)}");
                    //Validation is not supported for Renewals so validateOnly flag does not apply
                    response = client.RenewCertificate(renewRequest, trackingId);
                    _logger.LogTrace($"renewResponse:  {JsonConvert.SerializeObject(response)}");
                    break;

                default:
                    throw new Exception($"The enrollment type {enrollmentType} is not recognized.");
            }
            _logger.LogTrace($"Getting Cert By Tracking Id {response.TrackingId}");
            CertificateExt enrolledCert = client.GetCertificateByTrackingId(response.TrackingId);
            _logger.LogTrace($"Got Cert By Tracking Id {response.TrackingId} with status of {enrolledCert.Status}");
            int status = ConvertStatus(enrolledCert.Status, response.TrackingId.ToString());
            string statusMessage;
            switch (status)
            {
                case (int)EndEntityStatus.GENERATED:
                    statusMessage = $"Certificate with trackingId {enrolledCert.TrackingId} issued successfully";
                    break;

                case (int)EndEntityStatus.EXTERNALVALIDATION:
                    // Attempt to approve the cert. If still pending, return External validation
                    (int statusPending, string statusPendingMessage) statusTuple = ApproveCert(response.TrackingId);
                    status = statusTuple.statusPending;
                    statusMessage = statusTuple.statusPendingMessage;
                    break;

                case (int)EndEntityStatus.FAILED:
                    statusMessage = $"Certificate with trackingId {enrolledCert.TrackingId} is denied";
                    break;

                default:
                    statusMessage = $"Certificate with trackingId {enrolledCert.TrackingId} has an unknown status";
                    break;
            }

            _logger.LogTrace($"Returning Result of CARequestId={response.TrackingId}, Certificate={response.EndEntityCert}, Status={status}, StatusMessage={statusMessage}");
            return new EnrollmentResult
            {
                CARequestID = response.TrackingId.ToString(),
                Certificate = response.EndEntityCert,
                Status = status,
                StatusMessage = statusMessage
            };

        }

        /// <summary>
        /// Gets the annotations for the CA Connector-level configuration fields
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, PropertyConfigInfo> GetCAConnectorAnnotations()
        {
            return new Dictionary<string, PropertyConfigInfo>()
            {
                [Constants.Config.USERNAME] = new PropertyConfigInfo()
                {
                    Comments = "Username for the gateway to authenticate with Entrust",
                    Hidden = false,
                    DefaultValue = "",
                    Type = "String"
                },
                [Constants.Config.PASSWORD] = new PropertyConfigInfo()
                {
                    Comments = "Password for the account used to authenticate with Entrust",
                    Hidden = true,
                    DefaultValue = "",
                    Type = "String"
                },
                [Constants.Config.CLIENTCERT] = new PropertyConfigInfo()
                {
                    Comments = "The client certificate information used to authenticate with Entrust (if configured to use certificate authentication). This can be either a Windows cert store location and thumbprint, or a PFX file and password.",
                    Hidden = false,
                    DefaultValue = "",
                    Type = "ClientCertificate"
                },
                [Constants.Config.NAME] = new PropertyConfigInfo()
                {
                    Comments = "The default requester name",
                    Hidden = false,
                    DefaultValue = "TestUser",
                    Type = "String"
                },
                [Constants.Config.EMAIL] = new PropertyConfigInfo()
                {
                    Comments = "The default requester email address",
                    Hidden = false,
                    DefaultValue = "email@email.invalid",
                    Type = "String"
                },
                [Constants.Config.PHONE] = new PropertyConfigInfo()
                {
                    Comments = "The default requester phone number",
                    Hidden = false,
                    DefaultValue = "0000000000",
                    Type = "String"
                },
                [Constants.Config.IGNOREEXPIRED] = new PropertyConfigInfo()
                {
                    Comments = "If set to true, will not sync expired certs from Entrust",
                    Hidden = false,
                    DefaultValue = false,
                    Type = "Boolean"
                },
                [Constants.Config.ENABLED] = new PropertyConfigInfo()
                {
                    Comments = "Flag to Enable or Disable gateway functionality. Disabling is primarily used to allow creation of the CA prior to configuration information being available.",
                    Hidden = false,
                    DefaultValue = true,
                    Type = "Boolean"
                }
            };
        }

        public List<string> GetProductIds()
        {
            try
            {
                ECSClient client = ECSClient.InitializeClient(_config);
                var certTypes = EntrustCertType.GetCustomerAccountTypes(client);
                List<string> productIds = new List<string>();
                productIds.AddRange(certTypes.Select(c => c.ProductCode));
                return productIds;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unable to retrieve cert types from Entrust: {ex.Message}");
                return new List<string>();
            }
        }

        public async Task<AnyCAPluginCertificate> GetSingleRecord(string caRequestID)
        {
            // Get status of cert and the cert itself from Digicert
            ECSClient client = ECSClient.InitializeClient(_config);

            // Split string to see what kind of ID we have.
            string[] parts = caRequestID.Split('-');

            // Get the cert by tracking ID or thumbprint.
            CertificateExt entrustCert = parts.Length == 1 ? client.GetCertificateByTrackingId(Int32.Parse(caRequestID)) : client.GetCertificateByThumbprint(parts[1]);
            int status = ConvertStatus(entrustCert.Status, caRequestID);
            if (status == (int)RequestDisposition.PENDING)
            {
                status = (int)EndEntityStatus.EXTERNALVALIDATION;
            }
            return new AnyCAPluginCertificate
            {
                CARequestID = caRequestID,
                Certificate = !string.IsNullOrEmpty(entrustCert.EndEntityCert) ? entrustCert.EndEntityCert : null,
                Status = status,
                ProductID = entrustCert.CertType
            };
        }

        public Dictionary<string, PropertyConfigInfo> GetTemplateParameterAnnotations()
        {
            return new Dictionary<string, PropertyConfigInfo>()
            {
                [Constants.Config.LIFETIME] = new PropertyConfigInfo()
                {
                    Comments = "OPTIONAL: The number of months of validity to use when requesting certs. If not provided, default is 12.",
                    Hidden = false,
                    DefaultValue = 12,
                    Type = "Number"
                },
                [Constants.Config.ORGANIZATION] = new PropertyConfigInfo()
                {
                    Comments = "OPTIONAL: For requests that will not have a subject (such as ACME) you can use this field to provide an organization name. Value supplied here will override any CSR values, so do not include this field if you want the organization from the CSR to be used.",
                    Hidden = false,
                    DefaultValue = "",
                    Type = "String"
                },
                [Constants.Config.CERTUSAGE] = new PropertyConfigInfo()
                {
                    Comments = "Required for public SSL certificate types. Represents the key usage for the certificates enrolled against this template. Valid values are 'server', 'client', or 'serverclient'. Do not provide a value for cert types that are not public SSL.",
                    Hidden = false,
                    DefaultValue = "server",
                    Type = "String"
                },
                [Constants.Config.RENEWAL_WINDOW] = new PropertyConfigInfo()
                {
                    Comments = "OPTIONAL: The number of days from certificate expiration that the gateway should do a renewal rather than a reissue. If not provided, default is 90.",
                    Hidden = false,
                    DefaultValue = 90,
                    Type = "Number"
                }
            };
        }

        public async Task Ping()
        {
            try
            {
                ECSClient client = ECSClient.InitializeClient(_config);

                _logger.LogDebug("Attempting to ping Entrust API.");

                _ = client.GetClients();

                _logger.LogDebug("Successfully pinged Entrust API.");
            }
            catch (Exception e)
            {
                _logger.LogError($"There was an error contacting Entrust: {e.Message}.");
                throw new Exception($"Error attempting to ping Entrust: {e.Message}.", e);
            }
        }

        public async Task<int> Revoke(string caRequestID, string hexSerialNumber, uint revocationReason)
        {
            _logger.LogTrace("Entered Entrust Revoke method");

            ECSClient client = ECSClient.InitializeClient(_config);
            string reason = RevokeReasonToString(revocationReason);
            string comment = $"Revoked by Entrust Gateway for the following reason: {reason}";
            var cert = await GetSingleRecord(caRequestID);

            if (!string.Equals(reason, "keyCompromise"))
            {
                // Entrust no longer accepts any reason codes other than keyCompromise and unspecified.
                reason = "unspecified";
            }

            if (!(cert.Status == (int)EndEntityStatus.GENERATED))
            {
                string errorMessage = String.Format("Request {0} was not found in Entrust database or is not in a valid state to perform a revocation", caRequestID);
                _logger.LogError(errorMessage);
                throw new Exception(errorMessage);
            }
            client.RevokeCertificate(Int32.Parse(caRequestID), reason, comment);

            return (int)EndEntityStatus.REVOKED;
        }

        public async Task Synchronize(BlockingCollection<AnyCAPluginCertificate> blockingBuffer, DateTime? lastSync, bool fullSync, CancellationToken cancelToken)
        {
            int deniedCerts = 0;
            int totalSkipped = 0;
            ECSClient client = ECSClient.InitializeClient(_config);
            List<Certificate> allCerts = client.GetAllCertificates();
            bool ignoreExpired = false;
            if (_config.IgnoreExpired.HasValue)
            {
                ignoreExpired = _config.IgnoreExpired.Value;
            }
            foreach (Certificate entrustCert in allCerts)
            {
                cancelToken.ThrowIfCancellationRequested();

                if (entrustCert.ExpiresAfter.GetValueOrDefault() <= DateTime.UtcNow && ignoreExpired)
                {
                    _logger.LogTrace($"The certificate with serial number '{entrustCert.SerialNumber}' is expired and IgnoreExpired is true. Skipping.");
                    continue;
                }

                // Set up request ID.
                string caRequestId = entrustCert.TrackingId.ToString();

                // If the tracking ID is 0, log it and modify the request ID.
                if (entrustCert.TrackingId == 0)
                {
                    _logger.LogWarning($"The certificate with serial number '{entrustCert.SerialNumber}' has a tracking ID of 0. Will attempt to sync using thumbprint.");

                    string thumbprint = GetThumbprint(entrustCert);
                    if (string.IsNullOrEmpty(thumbprint))
                    {
                        _logger.LogWarning("The thumbprint could not be found. Skipping certificate.");
                        ++totalSkipped;
                        continue;
                    }

                    caRequestId = $"0-{thumbprint}";
                }

                try
                {
                    // Find cert within the database
                    int dbCertStatus;
                    try
                    {
                        dbCertStatus = await _certificateDataReader.GetStatusByRequestID(caRequestId);
                    }
                    catch
                    {
                        //Record not found in database
                        dbCertStatus = -1;
                    }

                    // Get status and check to see if we need to skip it.
                    int entrustStatus = ConvertStatus(entrustCert.Status, caRequestId);
                    if (entrustStatus == (int)EndEntityStatus.FAILED)
                    {
                        _logger.LogWarning($"Certificate with tracking ID '{entrustCert.TrackingId}' has a status of FAILED and will be skipped, as it has no certificate record.");
                        ++deniedCerts;
                        continue;
                    }

                    // If the cert exists, check the status and see if it's different from the cert from Entrust
                    // If doing a full sync, update the record anyway (in case other fields have changed)
                    if (dbCertStatus >= 0)
                    {
                        if (dbCertStatus != entrustStatus || fullSync)
                        {
                            AnyCAPluginCertificate newCert = entrustCert.TrackingId != 0 ? GetRecordByTrackingId(entrustCert.TrackingId) : GetRecordByThumbprint(GetThumbprint(entrustCert));
                            blockingBuffer.Add(newCert);
                        }
                    }
                    else
                    {
                        AnyCAPluginCertificate newCert = entrustCert.TrackingId != 0 ? GetRecordByTrackingId(entrustCert.TrackingId) : GetRecordByThumbprint(GetThumbprint(entrustCert));
                        blockingBuffer.Add(newCert);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError($"An error occurred while processing certificate with tracking ID '{entrustCert.TrackingId}', skipping.", e);
                    ++totalSkipped;
                }
            }

            _logger.LogDebug($"Synchronization skipped a total of {deniedCerts} certificates with the 'DECLINED' status.");
        }

        public async Task ValidateCAConnectionInfo(Dictionary<string, object> connectionInfo)
        {
            _logger.MethodEntry(LogLevel.Trace);

            try
            {
                if (!(bool)connectionInfo[Constants.Config.ENABLED])
                {
                    _logger.LogWarning($"The CA is currently in the Disabled state. It must be Enabled to perform operations. Skipping validation...");
                    _logger.MethodExit(LogLevel.Trace);
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception: {LogHandler.FlattenException(ex)}");
            }

            List<string> errors = new List<string>();

            _logger.LogTrace("Checking the Username");
            string username = connectionInfo.ContainsKey(Constants.Config.USERNAME) ? (string)connectionInfo[Constants.Config.USERNAME] : string.Empty;
            if (string.IsNullOrWhiteSpace(username))
            {
                errors.Add("The username is required");
            }

            _logger.LogTrace("Checking the Password");
            string password = connectionInfo.ContainsKey(Constants.Config.PASSWORD) ? (string)connectionInfo[Constants.Config.PASSWORD] : string.Empty;
            if (string.IsNullOrWhiteSpace(password))
            {
                errors.Add("The password is required");
            }

            _logger.LogTrace("Checking the user information");
            string name = connectionInfo.ContainsKey(Constants.Config.NAME) ? (string)connectionInfo[Constants.Config.NAME] : string.Empty;
            if (string.IsNullOrWhiteSpace(name))
            {
                errors.Add("The name is required");
            }

            string email = connectionInfo.ContainsKey(Constants.Config.EMAIL) ? (string)connectionInfo[Constants.Config.EMAIL] : string.Empty;
            if (string.IsNullOrWhiteSpace(email))
            {
                errors.Add("The email is required");
            }

            string number = connectionInfo.ContainsKey(Constants.Config.PHONE) ? (string)connectionInfo[Constants.Config.PHONE] : string.Empty;
            if (string.IsNullOrWhiteSpace(number))
            {
                errors.Add("The phone number is required");
            }

            ECSConfig tempConfig = JsonConvert.DeserializeObject<ECSConfig>(JsonConvert.SerializeObject(connectionInfo));

            ECSClient client = ECSClient.InitializeClient(tempConfig);
            try
            {
                List<ClientInfo> clients = client.GetClients();
                if (clients.Count <= 0)
                {
                    errors.Add($"Checking clients to determine Entrust connection failed.");
                }
            }
            catch (Exception e)
            {
                errors.Add($"An error occured when trying to connect to Entrust. {e.Message}");
            }
            _logger.LogTrace("Leaving 'ValidateCAConnectionInfo' method.");

            // We cannot proceed if there are any errors.
            if (errors.Any())
            {
                ThrowValidationException(errors);
            }
        }

        private void ThrowValidationException(List<string> errors)
        {
            throw new AnyCAValidationException(string.Join("\n", errors));
        }

        public async Task ValidateProductInfo(EnrollmentProductInfo productInfo, Dictionary<string, object> connectionInfo)
        {
            string productId = productInfo.ProductID;
            ECSConfig tempConfig = JsonConvert.DeserializeObject<ECSConfig>(JsonConvert.SerializeObject(connectionInfo));

            ECSClient client = ECSClient.InitializeClient(tempConfig);
            _logger.LogTrace("Checking inventory");

            bool inventory = EntrustCertType.ProductIDValid(client, productId);
            if (!inventory)
            {
                throw new Exception($"The product ID '{productId}' could not be validated.");
            }
            else
            {
                _logger.LogTrace($"Validation for product ID '{productId}' successful");
            }
        }

        private int ConvertStatus(string status, string certId)
        {
            switch (status.ToLower())
            {
                case "active":
                case "ready":
                case "reissued":
                case "renewed":
                case "expired":
                    return (int)EndEntityStatus.GENERATED;

                case "pending":
                    return (int)EndEntityStatus.EXTERNALVALIDATION;

                case "deactivated":
                case "suspended":
                case "revoked":
                    return (int)EndEntityStatus.REVOKED;

                case "declined":
                    return (int)EndEntityStatus.FAILED;

                default:
                    _logger.LogError($"Order {certId} has unexpected status {status}");
                    throw new Exception($"Order {certId} has unknown status {status}");
            }
        }

        public static string RevokeReasonToString(UInt32 revokeType)
        {
            switch (revokeType)
            {
                case 1:
                case 2:  // Entrust doesn't accept CA Compromised, since they get to decide that, not us
                    return "keyCompromise";
                case 3:
                    return "affiliationChanged";
                case 4:
                    return "superseded";
                case 5:
                case 6: // Entrust doesn't accept Certificate Hold
                    return "cessationOfOperation";
                default:
                    return "affiliationChanged";
            }
        }

        private string GetThumbprint(Certificate entrustCert)
        {
            // It seems as if this URL is the only place we can actually get the thumbprint.
            if (entrustCert.URI.Contains("/thumbprints/"))
            {
                string[] parts = entrustCert.URI.Split(new string[] { "/thumbprints/" }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 1)
                {
                    // Trim just in case some URIs come back with trailing slash.
                    return parts.Last().Trim('/').ToUpper();
                }
            }

            // If the URL doesn't contain thumbprint, we return nothing.
            return null;
        }

        /// <summary>
        /// Gets a single record by its tracking ID.
        /// </summary>
        /// <param name="client">The Entrust REST API client.</param>
        /// <param name="trackingId">The tracking ID of the cert we want.</param>
        /// <returns></returns>
        private AnyCAPluginCertificate GetRecordByTrackingId(int trackingId)
        {
            ECSClient client = ECSClient.InitializeClient(_config);
            CertificateExt entrustCertDetail = client.GetCertificateByTrackingId(trackingId);
            string cert = !string.IsNullOrEmpty(entrustCertDetail.EndEntityCert) ? entrustCertDetail.EndEntityCert : null;
            int statusCode = ConvertStatus(entrustCertDetail.Status, trackingId.ToString());

            AnyCAPluginCertificate newCert = new AnyCAPluginCertificate
            {
                CARequestID = trackingId.ToString(),
                Certificate = cert,
                Status = statusCode,
                CSR = !string.IsNullOrEmpty(entrustCertDetail.Csr) ? entrustCertDetail.Csr : null,
                RevocationDate = entrustCertDetail.Tracking.Deactivated ? entrustCertDetail.Tracking.DeactivatedOn ?? DateTime.UtcNow : (DateTime?)null,
                ProductID = entrustCertDetail.CertType
            };
            return newCert;
        }

        /// <summary>
        /// Gets a single record by its thumbprint.
        /// </summary>
        /// <param name="client">The Entrust REST API client.</param>
        /// <param name="thumbprint">The thumbprint of the cert we want.</param>
        /// <returns></returns>
        private AnyCAPluginCertificate GetRecordByThumbprint(string thumbprint)
        {
            ECSClient client = ECSClient.InitializeClient(_config);
            CertificateExt entrustCertDetail = client.GetCertificateByThumbprint(thumbprint);
            string cert = !string.IsNullOrEmpty(entrustCertDetail.EndEntityCert) ? entrustCertDetail.EndEntityCert : null;
            int statusCode = entrustCertDetail.Status.Equals("UNKNOWN", StringComparison.OrdinalIgnoreCase) ? (int)EndEntityStatus.EXTERNALVALIDATION : ConvertStatus(entrustCertDetail.Status, $"0-{thumbprint}");
            AnyCAPluginCertificate newCert = new AnyCAPluginCertificate
            {
                CARequestID = $"0-{thumbprint}",
                Certificate = cert,
                Status = statusCode,
                CSR = !string.IsNullOrEmpty(entrustCertDetail.Csr) ? entrustCertDetail.Csr : null,
                RevocationDate = entrustCertDetail.Tracking.Deactivated ? entrustCertDetail.Tracking.DeactivatedOn ?? DateTime.UtcNow : (DateTime?)null,
                ProductID = entrustCertDetail.CertType
            };
            return newCert;
        }

        private int GetTrackingId(EnrollmentProductInfo enrollmentProductInfo)
        {
            ECSClient client = ECSClient.InitializeClient(_config);
            if (enrollmentProductInfo.ProductParameters.ContainsKey("PriorCertSN"))
            {
                //get prior cert serial number
                string attrPriorCertSN = enrollmentProductInfo.ProductParameters["PriorCertSN"];

                //requesting certificate by serial number
                Certificate priorCertTemp = client.GetCertificateBySerialNumber(attrPriorCertSN);
                if (priorCertTemp != null)
                {
                    return priorCertTemp.TrackingId;
                }
                else
                {
                    _logger.LogTrace($"No certificate found with serial number {enrollmentProductInfo.ProductParameters["PriorCertSN"]}.");
                }
            }

            throw new Exception($"Reissue requested, but certificate with serial number {enrollmentProductInfo.ProductParameters["PriorCertSN"]} not found.");
        }

        private ValueTuple<int, string> ApproveCert(int trackingId)
        {
            var client = ECSClient.InitializeClient(_config);
            CertificateResponse approveResult = client.ApproveCertificate(trackingId);
            CertificateExt changedCert = client.GetCertificateByTrackingId(trackingId);
            int newStatus = ConvertStatus(changedCert.Status, trackingId.ToString());

            if (newStatus == (int)EndEntityStatus.EXTERNALVALIDATION)
            {
                return ((int)EndEntityStatus.EXTERNALVALIDATION, $"Certificate with trackingId {trackingId} is still pending after approval attempt. External validation is required.");
            }
            else if (newStatus == (int)EndEntityStatus.GENERATED)
            {
                return (newStatus, $"Certificate with trackingId {trackingId} has been issued after Entrust returned it with a pending status.");
            }

            throw new Exception($"Unable to approve certificate with trackingId {trackingId}. Status is neither issued or pending. ");
        }
    }
}

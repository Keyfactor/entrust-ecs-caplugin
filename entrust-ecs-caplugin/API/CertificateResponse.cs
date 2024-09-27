using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Keyfactor.Extensions.CAPlugin.Entrust.API
{
    public class CertificateResponse
    {

        /// <summary>
        /// Gets or Sets TrackingId
        /// </summary>
        [JsonProperty("trackingId")]
        public int TrackingId { get; set; }

        /// <summary>
        /// PEM-encoded certificate 
        /// </summary>
        /// <value>PEM-encoded certificate </value>
        [JsonProperty("endEntityCert")]
        public string EndEntityCert { get; set; }

        /// <summary>
        /// Gets or Sets ChainCerts
        /// </summary>
        [JsonProperty("chainCerts")]
        public List<string> ChainCerts { get; set; }

        /// <summary>
        /// Serial number in hexadecimal format 
        /// </summary>
        /// <value>Serial number in hexadecimal format </value>
        [JsonProperty("serialNumber")]
        public string SerialNumber { get; set; }

        /// <summary>
        /// The date and time, in RFC3339 format, after which the certificate is no longer valid.
        /// </summary>
        /// <value>The date and time, in RFC3339 format, after which the certificate is no longer valid.</value>
        [JsonProperty("expiresAfter")]
        public DateTime? ExpiresAfter { get; set; }

        /// <summary>
        /// Gets or Sets PickupUrl
        /// </summary>
        [JsonProperty("pickupUrl")]
        public string PickupUrl { get; set; }

        /// <summary>
        /// S/MIME certificate and private key in PKCS12 format protected by the provided password. Only returned for SMIME_ENT certtype and only if no CSR is supplied. 
        /// </summary>
        /// <value>S/MIME certificate and private key in PKCS12 format protected by the provided password. Only returned for SMIME_ENT certtype and only if no CSR is supplied. </value>
        [JsonProperty("pkcs12")]
        public string Pkcs12 { get; set; }


    }
}

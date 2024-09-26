using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Keyfactor.Extensions.CAPlugin.Entrust.API
{
	public class RenewCertificateRequest : ECSBaseRequest
	{
		public RenewCertificateRequest(int trackingId)
		{
			this.Resource = $"certificates/{trackingId}/renewals";
			this.Method = "POST";
		}
	}
	public class RenewCertificateRequestBody : ReissueCertificateRequestBody
	{
		/// <summary>
		/// If the validateOnly flag is set to true, the request contents will be validated for correctness but will not otherwise be processed. No inventory will be consumed and no certificate will be generated. 
		/// </summary>
		/// <value>If the validateOnly flag is set to true, the request contents will be validated for correctness but will not otherwise be processed. No inventory will be consumed and no certificate will be generated. </value>
		[JsonProperty("validateOnly")]
		public bool? ValidateOnly { get; set; }

		/// <summary>
		/// The date the certificate is set to expire (pooling accounts only). An RFC3339 compliant date, for example&amp;#58; YYYY-MM-DD Note that only the date (day, month, year) is supported for specifying expiry date. If you choose to specify an expiry time with the expiry date, the time will be adjusted to Eastern Standard Time (EST). This could have the unintended effect of moving your expiry date to the previous day. 
		/// </summary>
		/// <value>The date the certificate is set to expire (pooling accounts only). An RFC3339 compliant date, for example&amp;#58; YYYY-MM-DD Note that only the date (day, month, year) is supported for specifying expiry date. If you choose to specify an expiry time with the expiry date, the time will be adjusted to Eastern Standard Time (EST). This could have the unintended effect of moving your expiry date to the previous day. </value>
		[JsonProperty("certExpiryDate")]
		public DateTime? CertExpiryDate { get; set; }

		/// <summary>
		/// The lifetime of the certificate. Applies to all non-pooling accounts and to CDS_INDIVIDUAL, CDS_GROUP, CDS_ENT_LITE, CDS_ENT_PRO, and SMIME_ENT certificates, regardless of account type.  This value is specified as an ISO 8601 duration.  Allowed values are: &#39;P1Y&#39;, &#39;P2Y&#39;, and &#39;P3Y&#39;. 
		/// </summary>
		/// <value>The lifetime of the certificate. Applies to all non-pooling accounts and to CDS_INDIVIDUAL, CDS_GROUP, CDS_ENT_LITE, CDS_ENT_PRO, and SMIME_ENT certificates, regardless of account type.  This value is specified as an ISO 8601 duration.  Allowed values are: &#39;P1Y&#39;, &#39;P2Y&#39;, and &#39;P3Y&#39;. </value>
		[JsonProperty("certLifetime")]
		public string CertLifetime { get; set; }
	}
}

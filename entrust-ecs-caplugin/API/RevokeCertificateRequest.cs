using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Keyfactor.Extensions.CAPlugin.Entrust.API
{
	public partial class RevokeCertificateRequest
	{
		/// <summary>
		/// Gets or Sets CrlReason
		/// </summary>
		[JsonProperty("crlReason")]
		public string CrlReason { get; set; }

		/// <summary>
		/// Comment field to explain the reason for revocation 
		/// </summary>
		/// <value>Comment field to explain the reason for revocation </value>
		[JsonProperty("revocationComment")]
		public string RevocationComment { get; set; }

	}

	public class RevokeCertificateCall : ECSBaseRequest
	{


		public RevokeCertificateCall(int trackingId)
		{
			this.Resource = "certificates/" + trackingId.ToString() + "/revocations";
			this.Method = "POST";
		}
	}
}

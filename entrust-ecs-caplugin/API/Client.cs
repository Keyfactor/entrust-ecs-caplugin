using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Keyfactor.Extensions.CAPlugin.Entrust.API
{
	public class GetClientsRequest : ECSBaseRequest
	{
		public GetClientsRequest()
		{
			this.Resource = "clients";
			this.Method = "GET";
		}
	}

	public class GetClientsResponse
	{
		[JsonProperty("clients")]
		public List<ClientInfo> Clients { get; set; }
	}

	public class ClientInfo
	{
		/// <summary>
		/// Gets or Sets VerificationStatus
		/// </summary>
		[JsonProperty("evVerificationStatus")]
		public string EVVerificationStatus { get; set; }

		/// <summary>
		/// Gets or Sets VerificationStatus
		/// </summary>
		[JsonProperty("verificationStatus")]
		public string VerificationStatus { get; set; }

		/// <summary>
		/// Client ID of client. For the primary client, this is 1. 
		/// </summary>
		/// <value>Client ID of client. For the primary client, this is 1. </value>
		[JsonProperty("clientId")]
		public int ClientId { get; set; }

		/// <summary>
		/// The company name of the client
		/// </summary>
		/// <value>The company name of the client</value>
		[JsonProperty("clientName")]
		public string ClientName { get; set; }

		/// <summary>
		/// Gets or Sets FriendlyClientName
		/// </summary>
		[JsonProperty("friendlyClientName")]
		public string FriendlyClientName { get; set; }

		/// <summary>
		/// OV information expiry date - - only present if client has been APPROVED
		/// </summary>
		/// <value>OV information expiry date - - only present if client has been APPROVED</value>
		[JsonProperty("ovExpiryDate")]
		public DateTime? OvExpiryDate { get; set; }

		[JsonProperty("evExpiryDate")]
		public DateTime? EvExpiryDate { get; set; }
	}
}

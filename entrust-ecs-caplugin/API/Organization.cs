using Newtonsoft.Json;

using Org.BouncyCastle.Asn1.Cmp;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Keyfactor.Extensions.CAPlugin.Entrust.API
{
	public class GetOrganizationsRequest : ECSBaseRequest
	{
		/// <summary>
		/// Default constructor.
		/// </summary>
		public GetOrganizationsRequest()
		{
			Resource = "organizations";
			Method = "GET";
		}
	}

	public class GetOrganizationsResponse : ECSBaseResponse
	{
		/// <summary>
		/// The collection of organizations returned by Entrust.
		/// </summary>
		[JsonProperty("organizations")]
		public List<Organization> Organizations { get; set; }
	}

	public class Organization
	{
		/// <summary>
		/// The organization's name.
		/// </summary>
		[JsonProperty("name")]
		public string Name { get; set; }

		/// <summary>
		/// The status of the organization.
		/// </summary>
		[JsonProperty("verificationStatus")]
		public string VerificationStatus { get; set; }

		/// <summary>
		/// The ID of the client associated with this organization.
		/// </summary>
		[JsonProperty("clientId")]
		public int ClientId { get; set; }
	}
}

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Keyfactor.Extensions.CAPlugin.Entrust.Models
{
	public class Tracking
	{
		[JsonProperty("trackingInfo")]
		public string TrackingInfo { get; set; }

		[JsonProperty("requesterName")]
		public string RequesterName { get; set; }

		[JsonProperty("requesterEmail")]
		public string RequesterEmail { get; set; }

		[JsonProperty("requesterPhone")]
		public string RequesterPhone { get; set; }

		[JsonProperty("deactivated")]
		public bool Deactivated { get; set; }

		[JsonProperty("deactivatedOn")]
		public DateTime? DeactivatedOn { get; set; }
	}
}

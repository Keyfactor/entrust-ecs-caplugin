using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Keyfactor.Extensions.CAPlugin.Entrust.API
{
	public class VersionRequest : ECSBaseRequest
	{
		public VersionRequest()
		{
			this.Resource = "application/version";
			this.Method = "GET";
		}

		public class VersionResponse
		{
			/// <summary>
			/// Gets or Sets Status
			/// </summary>
			[JsonProperty("version")]
			public string Version { get; set; }
		}
	}
}

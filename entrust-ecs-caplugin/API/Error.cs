using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Keyfactor.Extensions.CAPlugin.Entrust.API
{
	public class Error
	{
		[JsonProperty("message")]
		public string Message { get; set; }
	}

	public class ErrorResponse
	{
		[JsonProperty("errors")]
		public List<Error> Errors { get; set; }

		[JsonProperty("status")]
		public int Status { get; set; }
	}
}

// Copyright 2024 Keyfactor
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions
// and limitations under the License.

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

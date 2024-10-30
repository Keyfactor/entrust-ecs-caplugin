// Copyright 2024 Keyfactor
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions
// and limitations under the License.

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Keyfactor.Extensions.CAPlugin.Entrust.Models
{
	public class CertificateRequest
	{
		[JsonProperty("csr")]
		public string CSR { get; set; }

		[JsonProperty("subjectAltName")]
		public List<string> SubjectAltName { get; set; }

		[JsonProperty("signingAlg")]
		public string SigningAlg { get; set; } = "SHA-2";

		[JsonProperty("eku")]
		public string EKU { get; set; }

		[JsonProperty("ctLog")]
		public bool? CTLog { get; set; }

		[JsonProperty("cn")]
		public string CN { get; set; }

		[JsonProperty("certEmail")]
		public string CertEmail { get; set; }

		[JsonProperty("upn")]
		public string UPN { get; set; }

		[JsonProperty("clientId")]
		public int? ClientId { get; set; }

		[JsonProperty("org")]
		public string Org { get; set; }

		[JsonProperty("ou")]
		public List<string> OU { get; set; }

		[JsonProperty("password")]
		public string Password { get; set; }

		[JsonProperty("tracking")]
		public Tracking Tracking { get; set; }

		[JsonProperty("endUserKeyStorageAgreement")]
		public bool? EndUserKeyStorageAgreement { get; set; }

		[JsonProperty("queueForApproval")]
		public bool? QueueForApproval { get; set; }

		[JsonProperty("certExpiryDate")]
		public DateTime? CertExpiryDate { get; set; }

		[JsonProperty("certLifetime")]
		public string CertLifetime { get; set; }

		[JsonProperty("validateOnly")]
		public bool? ValidateOnly { get; set; }

		[JsonProperty("certType")]
		public string CertType { get; set; }
	}
}

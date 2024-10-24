// Copyright 2024 Keyfactor
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions
// and limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace Keyfactor.Extensions.CAPlugin.Entrust
{
	public class ECSConfig
	{
		public string AuthUsername { get; set; }
		public string AuthPassword { get; set; }
		public AuthCert ClientCertificate { get; set; }
		public string Name { get; set; }
		public string Email { get; set; }
		public string PhoneNumber { get; set; }
		public bool? IgnoreExpired { get; set; }
		public bool Enabled { get; set; } = true;
	}

	public class AuthCert
	{
		public string StoreName { get; set; }
		public string StoreLocation { get; set; }
		public string Thumbprint { get; set; }
		public string CertificatePath { get; set; }
		public string CertificatePassword { get; set; }
	}
}

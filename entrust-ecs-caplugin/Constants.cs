﻿// Copyright 2024 Keyfactor
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions
// and limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Keyfactor.Extensions.CAPlugin.Entrust
{
	public class Constants
	{
		public class Config
		{
			public const string USERNAME = "AuthUsername";
			public const string PASSWORD = "AuthPassword";
			public const string CLIENTCERT = "ClientCertificate";
			public const string NAME = "Name";
			public const string EMAIL = "Email";
			public const string PHONE = "PhoneNumber";
			public const string IGNOREEXPIRED = "IgnoreExpired";
			public const string ENABLED = "Enabled";

			public const string LIFETIME = "LifetimeMonths";
			public const string ORGANIZATION = "Organization";
			public const string CERTUSAGE = "CertificateUsage";
			public const string RENEWAL_WINDOW = "RenewalWindowDays";

			public class ClientCert
			{
				public const string STORE_NAME = "StoreName";
				public const string STORE_LOC = "StoreLocation";
				public const string THUMBPRINT = "Thumbprint";
				public const string CERT_PATH = "CertificatePath";
				public const string CERT_PASS = "CertificatePassword";
			}
		}
	}
}

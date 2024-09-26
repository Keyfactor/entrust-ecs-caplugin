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

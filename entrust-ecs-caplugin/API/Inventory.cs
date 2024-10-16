using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Keyfactor.Extensions.CAPlugin.Entrust.API
{
	public class GetInventoryRequest : ECSBaseRequest
	{
		public GetInventoryRequest()
		{
			this.Resource = "inventories";
			this.Method = "GET";
		}
	}

	public class InventoryItem
	{

		/// <summary>
		/// Gets or Sets ProductType
		/// </summary>
		[JsonProperty("productType")]
		public string ProductType { get; set; }

		/// <summary>
		/// Total inventory for this product type ever added to the account
		/// </summary>
		/// <value>Total inventory for this product type ever added to the account</value>
		[JsonProperty("totalCount")]
		public int? TotalCount { get; set; }

		/// <summary>
		/// Inventory for this product type that has not been used, and has not expired
		/// </summary>
		/// <value>Inventory for this product type that has not been used, and has not expired</value>
		[JsonProperty("remainingCount")]
		public int? RemainingCount { get; set; }

		/// <summary>
		/// Count of consumed inventory for this product type. This count does not include expired inventory
		/// </summary>
		/// <value>Count of consumed inventory for this product type. This count does not include expired inventory</value>
		[JsonProperty("usedCount")]
		public int? UsedCount { get; set; }
	}

	public class GetInventoryResponse
	{
		[JsonProperty("inventories")]
		public List<InventoryItem> Inventories { get; set; }
	}
}

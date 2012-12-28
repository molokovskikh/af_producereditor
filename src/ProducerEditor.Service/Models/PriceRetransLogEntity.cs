using System;
using NHibernate.Mapping.Attributes;

namespace ProducerEditor.Service
{
	[Class(Table = "logs.PricesRetrans")]
	public class PriceRetransLogEntity
	{
		public PriceRetransLogEntity()
		{
		}

		public PriceRetransLogEntity(string operatorName, string operatorHost, uint priceCode)
		{
			OperatorName = operatorName;
			OperatorHost = operatorHost;
			PriceItemId = priceCode;
			LogTime = DateTime.Now;
		}

		[Id(0, Name = "Id"), Generator(1, Class = "native")]
		public virtual uint Id { get; set; }

		[Property]
		public virtual DateTime LogTime { get; set; }

		[Property]
		public virtual string OperatorName { get; set; }

		[Property]
		public virtual string OperatorHost { get; set; }

		[Property]
		public virtual uint PriceItemId { get; set; }

		[Property]
		public virtual uint RetransType { get; set; }
	}
}
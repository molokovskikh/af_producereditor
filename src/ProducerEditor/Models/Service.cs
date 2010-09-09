using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ProducerEditor.Models
{
	[DataContract(Namespace = "http://schemas.datacontract.org/2004/07/ProducerEditor.Service")]
	public class OfferView
	{
		[DataMember]
		public string ProductSynonym { get; set; }
		[DataMember]
		public string ProducerSynonym { get; set; }
		[DataMember]
		public string Supplier { get; set; }
		[DataMember]
		public byte Segment { get; set; }

		public string SegmentAsString()
		{
			return Segment == 0 ? "Опт" : "Розница";
		}
	}

	[DataContract(Namespace = "http://schemas.datacontract.org/2004/07/ProducerEditor.Service")]
	public class ProducerDto
	{
		[DataMember]
		public virtual uint Id { get; set; }
		[DataMember]
		public virtual string Name { get; set; }
		[DataMember]
		public virtual bool Checked { get; set; }
		[DataMember]
		public virtual bool HasOffers { get; set;}
	}

	[DataContract(Namespace = "http://schemas.datacontract.org/2004/07/ProducerEditor.Service")]
	public class ProductAndProducer
	{
		[DataMember]
		public bool Selected { get; set; }
		[DataMember]
		public long ExistsInRls { get; set; }
		[DataMember]
		public uint ProducerId { get; set; }
		[DataMember]
		public string Producer { get; set; }
		[DataMember]
		public uint CatalogId { get; set; }
		[DataMember]
		public string Product { get; set; }
		[DataMember]
		public long OrdersCount { get; set; }
		[DataMember]
		public long OffersCount { get; set; }
	}

	[DataContract(Namespace = "http://schemas.datacontract.org/2004/07/ProducerEditor.Service")]
	public class SynonymReportItem
	{
		[DataMember]
		public uint Id { get; set; }
		[DataMember]
		public string User { get; set; }
		[DataMember]
		public string Price { get; set; }
		[DataMember]
		public string Region { get; set; }
		[DataMember]
		public string Synonym { get; set; }
		[DataMember]
		public string Producer { get; set; }
		[DataMember]
		public string Products { get; set; }
		[DataMember]
		public int IsSuspicious { get; set; }
		[DataMember]
		public uint SupplierId { get; set; }
	}

	public interface IPager
	{
		uint Page { get; set; }
		uint TotalPages { get; set; }
	}

	[DataContract(Namespace = "http://schemas.datacontract.org/2004/07/ProducerEditor.Service")]
	public class Pager<T> : IPager
	{
		[DataMember]
		public uint Page { get; set; }
		[DataMember]
		public uint TotalPages { get; set; }
		[DataMember]
		public IList<T> Content { get; set; }
	}

	[DataContract(Namespace = "http://schemas.datacontract.org/2004/07/ProducerEditor.Service")]
	public class OffersQuery
	{
		public OffersQuery(string field, uint value)
		{
			Field = field;
			Value = value;
		}

		[DataMember]
		public string Field { get; set; }
		[DataMember]
		public object Value { get; set; }
	}
}
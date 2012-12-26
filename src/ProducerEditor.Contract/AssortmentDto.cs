using System.Runtime.Serialization;

namespace ProducerEditor.Contract
{
	[DataContract(Namespace = "http://schemas.datacontract.org/2004/07/ProducerEditor.Service", Name = "Assortment")]
	public class AssortmentDto
	{
		[DataMember]
		public uint Id { get; set; }

		[DataMember]
		public string Product { get; set; }

		[DataMember]
		public string Producer { get; set; }

		[DataMember]
		public uint ProducerId { get; set; }

		[DataMember]
		public uint CatalogId { get; set; }

		[DataMember]
		public bool Checked { get; set; }
	}

	[DataContract(Namespace = "http://schemas.datacontract.org/2004/07/ProducerEditor.Service", Name = "Exclude")]
	public class ExcludeDto
	{
		[DataMember]
		public uint Id { get; set; }

		[DataMember]
		public string Supplier { get; set; }

		[DataMember]
		public string Region { get; set; }

		[DataMember]
		public string Catalog { get; set; }

		[DataMember]
		public string ProducerSynonym { get; set; }

		[DataMember]
		public string OriginalSynonym { get; set; }

		[DataMember]
		public uint OriginalSynonymId { get; set; }

		[DataMember]
		public string Operator { get; set; }
	}
}
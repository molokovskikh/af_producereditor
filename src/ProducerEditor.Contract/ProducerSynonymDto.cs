using System.Runtime.Serialization;

namespace ProducerEditor.Contract
{
	[DataContract(Name = "ProducerSynonym", Namespace = "http://schemas.datacontract.org/2004/07/ProducerEditor.Service")]
	public class ProducerSynonymDto
	{
		[DataMember]
		public virtual uint Id { get; set; }
		[DataMember]
		public virtual string Name { get; set; }
		[DataMember]
		public virtual string Producer { get; set; }
		[DataMember]
		public string Supplier { get; set; }
		[DataMember]
		public string Region { get; set; }
		[DataMember]
		public bool HaveOffers { get; set; }

		public bool SameAsCurrent { get; set; }
	}
}
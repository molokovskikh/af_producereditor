using System.Runtime.Serialization;

namespace ProducerEditor.Contract
{
	[DataContract(Namespace = "http://schemas.datacontract.org/2004/07/ProducerEditor.Service")]
	public class OffersQueryParams
	{
		public OffersQueryParams(string field, uint value)
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
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace ProducerEditor.Models
{
	[DataContract(Namespace = "http://schemas.datacontract.org/2004/07/ProducerEditor.Service")]
	public class Offer
	{
		[DataMember]
		public string Product { get; set; }
		[DataMember]
		public string Producer { get; set; }
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
	}

	[DataContract(Namespace = "http://schemas.datacontract.org/2004/07/ProducerEditor.Service")]
	public class Assortment
	{
		[DataMember]
		public uint Id { get; set; }
		[DataMember]
		public string Product { get; set; }
		[DataMember]
		public string Producer { get; set; }
	}

	[DataContract(Namespace = "http://schemas.datacontract.org/2004/07/ProducerEditor.Service")]
	public class Pager<T>
	{
		[DataMember]
		public uint Page { get; set; }
		[DataMember]
		public uint TotalPages { get; set; }
		[DataMember]
		public T Content { get; set; }
	}

	[ServiceContract]
	public interface ProducerService
	{
		[OperationContract]
		IList<Offer> GetOffers(uint producerSynonymId);

		[OperationContract]
		IList<SynonymReportItem> GetSynonymReport(DateTime begin, DateTime end);

		[OperationContract]
		IList<string> GetEquivalents(uint producerId);

		[OperationContract]
		IList<SynonymReportItem> ShowSuspiciousSynonyms();


		[OperationContract]
		Pager<IList<Assortment>> ShowAssortment(uint assortimentId);

		[OperationContract]
		Pager<IList<Assortment>> GetAssortmentPage(uint page);

		[OperationContract]
		Pager<IList<Assortment>> SearchAssortment(string text);


		[OperationContract]
		void Suspicious(uint producerSynonymId);

		[OperationContract]
		void DeleteSuspicious(uint producerSynonymId);

		[OperationContract]
		void DeleteProducerSynonym(uint producerSynonymId);

		[OperationContract]
		void DeleteProducer(uint producerId);
	}
}
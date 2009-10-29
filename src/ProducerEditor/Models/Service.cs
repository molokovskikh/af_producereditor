using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace ProducerEditor.Models
{
	[DataContract(Namespace = "http://schemas.datacontract.org/2004/07/ProducerEditor.Service")]
	public class Exclude
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
		public string Producer { get; set; }
		[DataMember]
		public string ProducerSynonym { get; set; }
	}

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
		public IList<T> Content { get; set; }
	}

	[ServiceContract]
	public interface ProducerService
	{
		[OperationContract]
		IList<Offer> ShowOffersBySynonym(uint producerSynonymId);

		[OperationContract]
		IList<SynonymReportItem> ShowSynonymReport(DateTime begin, DateTime end);

		[OperationContract]
		IList<string> GetEquivalents(uint producerId);

		[OperationContract]
		IList<SynonymReportItem> ShowSuspiciousSynonyms();

		[OperationContract]
		Pager<Assortment> ShowAssortment(uint assortimentId);

		[OperationContract]
		Pager<Assortment> GetAssortmentPage(uint page);

		[OperationContract]
		Pager<Assortment> SearchAssortment(string text);

		[OperationContract]
		void DeleteAssortment(uint assortmentId);

		[OperationContract]
		void Suspicious(uint producerSynonymId);

		[OperationContract]
		void DeleteSuspicious(uint producerSynonymId);

		[OperationContract]
		void DeleteProducerSynonym(uint producerSynonymId);

		[OperationContract]
		void DeleteProducer(uint producerId);

		[OperationContract]
		Pager<Exclude> ShowExcludes(uint page);

		[OperationContract]
		void DoNotShow(uint excludeId);

		[OperationContract]
		void AddToAssotrment(uint excludeId);
	}
}
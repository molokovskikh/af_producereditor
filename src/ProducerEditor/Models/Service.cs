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
		[DataMember]
		public uint ProducerSynonymId { get; set; }
		[DataMember]
		public string OriginalSynonym { get; set; }
		[DataMember]
		public uint OriginalSynonymId { get; set; }
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
	public class ProducerSynonym
	{
		[DataMember]
		public virtual uint Id { get; set; }
		[DataMember]
		public virtual string Name { get; set; }
		[DataMember]
		public string Supplier { get; set; }
		[DataMember]
		public string Region { get; set; }
		[DataMember]
		public Int64 HaveOffers { get; set; }
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

	[DataContract(Namespace = "http://schemas.datacontract.org/2004/07/ProducerEditor.Service")]
	public class Assortment
	{
		[DataMember]
		public uint Id { get; set; }
		[DataMember]
		public string Product { get; set; }
		[DataMember]
		public string Producer { get; set; }
		[DataMember]
		public bool Checked { get; set; }
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
		void DoJoin(uint[] sourceProduceIds, uint targetProducerId);

		[OperationContract]
		IList<SynonymReportItem> ShowSynonymReport(DateTime begin, DateTime end);

		[OperationContract]
		IList<string> GetEquivalents(uint producerId);

		[OperationContract]
		IList<ProducerSynonym> GetSynonyms(uint producerId);

		[OperationContract]
		IList<SynonymReportItem> ShowSuspiciousSynonyms();

		[OperationContract]
		Pager<Assortment> ShowAssortment(uint assortimentId);

		[OperationContract]
		Pager<Assortment> GetAssortmentPage(uint page);

		[OperationContract]
		Pager<Assortment> SearchAssortment(string text, uint page);

		[OperationContract]
		void DeleteAssortment(uint assortmentId);

		[OperationContract]
		void SetAssortmentChecked(uint assortmentId, bool @checked);

		[OperationContract]
		void Suspicious(uint producerSynonymId);

		[OperationContract]
		void DeleteSuspicious(uint producerSynonymId);

		[OperationContract]
		void DeleteProducerSynonym(uint producerSynonymId);

		[OperationContract]
		void DeleteProducer(uint producerId);

		[OperationContract]
		Pager<Exclude> ShowExcludes(uint page, bool isRefresh);

		[OperationContract]
		Pager<Exclude> SearchExcludes(string text, uint page, bool isRefresh);

		[OperationContract]
		void DoNotShow(uint excludeId);

		[OperationContract]
		IList<uint> AddToAssotrment(uint excludeId);

		[OperationContract]
		Pager<Assortment> ShowAssortmentForProducer(uint producerId, uint page);

		[OperationContract]
		void CreateEquivalentForProducer(uint producerId, string equivalentName);

		[OperationContract]
		string GetSupplierEmails(uint supplierId);

		[OperationContract]
		void DeleteSynonym(uint synonymId);
	}
}
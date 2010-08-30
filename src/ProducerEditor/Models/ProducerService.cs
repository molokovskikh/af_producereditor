using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace ProducerEditor.Models
{
	[ServiceContract]
	public interface ProducerService
	{
		[OperationContract]
		List<OfferView> ShowOffers(OffersQuery query);

		[OperationContract]
		IList<ProducerDto> GetProducers();

		[OperationContract]
		List<ProductAndProducer> ShowProductsAndProducers(uint producerId);

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
		void AddToAssotrment(uint excludeId, uint producerId, string equivalent);

		[OperationContract]
		Pager<Assortment> ShowAssortmentForProducer(uint producerId, uint page);

		[OperationContract]
		void CreateEquivalentForProducer(uint producerId, string equivalentName);

		[OperationContract]
		string GetSupplierEmails(uint supplierId);

		[OperationContract]
		void DeleteSynonym(uint synonymId);

		[OperationContract]
		void UpdateProducer(ProducerDto item);

		[OperationContract]
		void UpdateAssortment(Assortment item);
	}
}
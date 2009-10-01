using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Windows.Forms;
using NHibernate.Transform;
using ProducerEditor.Models;
using ProducerEditor.Service;
using ProducerEditor.Views;

namespace ProducerEditor
{
	public class MainController
	{
		public List<Producer> Producers { get; private set;}

		public IList<Producer> GetAllProducers()
		{
			Producers = With.Session(s =>s.CreateSQLQuery(@"
select p.Id,
p.Name,
p.Checked,
c.Id != 0 as HasOffers
from Catalogs.Producers p
	left join farm.core0 c on c.CodeFirmCr = p.Id
group by p.Id
order by p.Name")
						.SetResultTransformer(Transformers.AliasToBean(typeof (Producer)))
						.List<Producer>()).ToList();
			return Producers;
		}

		public void OfferForProducerId(uint producerId)
		{
			var offers = FindOffers(0, producerId);
			new OffersView(offers).ShowDialog();
		}

		public void OffersForCatalogId(uint catalogId)
		{
			var offers = FindOffers(catalogId, 0);
			new OffersView(offers).ShowDialog();
		}

		public void Update(Producer producer)
		{
			producer.Name = producer.Name.ToUpper();
			With.Master(producer.Update);
		}

		public void Join(Producer producer, Action update)
		{
			if (producer == null)
				return;
			var rename = new JoinView(this, producer);
			if (rename.ShowDialog() != DialogResult.Cancel)
				update();
		}

		public void DoJoin(Producer[] sources, Producer target)
		{
			With.Master(() => With.Session(session => {
				using (var transaction = session.BeginTransaction())
				{
					foreach (var source in sources)
					{

						session.CreateSQLQuery(
							@"
update farm.SynonymFirmCr
set CodeFirmCr = :TargetId
where CodeFirmCr = :SourceId
;

update farm.core0
set CodeFirmCr = :TargetId
where CodeFirmCr = :SourceId
;

update orders.orderslist
set CodeFirmCr = :TargetId
where CodeFirmCr = :SourceId
;")
							.SetParameter("SourceId", source.Id)
							.SetParameter("TargetId", target.Id)
							.ExecuteUpdate();
						session.Save(new ProducerEquivalent
						{
							Name = source.Name,
							Producer = target,
						});
						source.Delete();
					}
					transaction.Commit();
				}
				foreach (var source in sources)
					Producers.Remove(source);
			}));
		}

		public IList<SynonymView> Synonyms(Producer producer)
		{
			return With.Session(
				session => session.CreateSQLQuery(@"
select sfc.Synonym as Name,
sfc.SynonymFirmCrCode as Id,
cd.ShortName as Supplier,
cd.FirmSegment as Segment,
r.Region,
c.Id is not null as HaveOffers
from farm.SynonymFirmCr sfc
  join usersettings.PricesData pd on sfc.PriceCode = pd.PriceCode
    join usersettings.clientsdata cd on cd.FirmCode = pd.FirmCode
      join farm.Regions r on cd.RegionCode = r.RegionCode
  left join farm.Core0 c on c.SynonymFirmCrCode = sfc.SynonymFirmCrCode
where sfc.CodeFirmCr = :ProducerId and cd.BillingCode <> 921
group by sfc.SynonymFirmCrCode")
				           	.SetParameter("ProducerId", producer.Id)
				           	.SetResultTransformer(Transformers.AliasToBean(typeof (SynonymView)))
				           	.List<SynonymView>().ToList());
		}

		public List<Producer> SearchProducer(string text)
		{
			return Producers.Where(p => p.Name.Contains((text ?? "").ToUpper())).ToList();
		}

		public List<OrderView> FindOrders(Producer producer)
		{
			return With.Session(s => s.CreateSQLQuery(@"
select oh.WriteTime,
drugstore.ShortName as Drugstore,
supplier.ShortName as Supplier,
sa.Synonym as ProductSynonym,
sfc.Synonym as ProducerSynonym
from orders.orderslist ol
  join orders.ordershead oh on ol.OrderId = oh.RowId
    join farm.SynonymArchive sa on sa.SynonymCode = ol.SynonymCode
    join farm.SynonymFirmCr sfc on sfc.SynonymFirmCrCode = ol.SynonymFirmCrCode
  join usersettings.clientsdata drugstore on drugstore.FirmCode = oh.ClientCode
  join usersettings.pricesdata pd on pd.PriceCode = oh.PriceCode
    join usersettings.clientsdata supplier on supplier.FirmCode = pd.FirmCode
where ol.CodeFirmCr = :ProducerId and oh.Deleted = 0
order by oh.WriteTime desc
limit 20")
					.SetParameter("ProducerId", producer.Id)
					.SetResultTransformer(Transformers.AliasToBean(typeof (OrderView)))
					.List<OrderView>()).ToList();
		}

		public List<OfferView> FindOffers(uint catalogId, uint producerId)
		{
			return With.Session(s => {
				string filter;
				if (catalogId != 0)
					filter = "p.CatalogId = :CatalogId";
				else
					filter = "c.CodeFirmCr = :ProducerId";
				var query = s.CreateSQLQuery(
					String.Format(
						@"
select cd.ShortName as Supplier, 
cd.FirmSegment as Segment,
s.Synonym as ProductSynonym, 
sfc.Synonym as ProducerSynonym
from farm.core0 c
  join catalogs.Products p on p.Id = c.ProductId
  join farm.SynonymArchive s on s.SynonymCode = c.SynonymCode
  join farm.SynonymFirmCr sfc on sfc.SynonymFirmCrCode = c.SynonymFirmCrCode
  join usersettings.PricesData pd on pd.PriceCode = c.PriceCode
    join usersettings.ClientsData cd on cd.FirmCode = pd.FirmCode
where {0}
group by c.Id
order by cd.FirmCode",
						filter))
					.SetResultTransformer(Transformers.AliasToBean(typeof (OfferView)));
				if (catalogId != 0)
					query.SetParameter("CatalogId", catalogId);
				else
					query.SetParameter("ProducerId", producerId);
				return query.List<OfferView>();
			}).ToList();
		}

		public void Delete(object instance)
		{
			With.Master(() => {
				if (instance is Producer)
					Producer.Find(((Producer) instance).Id).Delete();
			});
		}

		public void DeleteSynonym(SynonymView view, Producer producer)
		{
			WithService(s => s.DeleteProducerSynonym(view.Id));
		}

		public void ShowSynonymReport()
		{
			var items = SynonymReportItem.Load(DateTime.Today.AddDays(-1), DateTime.Today);
			ShowDialog<SynonymReport>(items, DateTime.Today.AddDays(-1), DateTime.Today);
		}

		public void ShowOffersBySynonym(ProducerSynonym synonym)
		{
			if (synonym == null)
				return;

			IList<Offer> offers = null;
			WithService(s => {
				offers = s.GetOffers(synonym.Id);
			});
		
			ShowDialog<OffersBySynonym>(offers);
		}

		public void ShowProductsAndProducers(Producer producer)
		{
			if (producer == null)
				return;

			ShowDialog<ProductsAndProducersView>(this, producer, ProductAndProducer.FindRelativeProductsAndProducers(producer));
		}

		private void ShowDialog<T>(params object[] args)
		{
			var form = (Form) Activator.CreateInstance(typeof (T), args);
			form.ShowDialog();
		}

		private void WithService(Action<ProducerService> action)
		{
			var binding = new BasicHttpBinding
			{
				MaxBufferSize = int.MaxValue,
				MaxReceivedMessageSize = int.MaxValue,
				SendTimeout = TimeSpan.FromMinutes(10),
				ReaderQuotas = {MaxArrayLength = int.MaxValue},
			};
			var endpoint = new EndpointAddress(Settings.Default.EndpointAddress + "ProducerService.svc");
			var factory = new ChannelFactory<ProducerService>(binding, endpoint);
			action(factory.CreateChannel());
		}
	}
}
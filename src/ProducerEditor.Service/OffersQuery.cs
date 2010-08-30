using System;
using System.Runtime.Serialization;
using Common.Models.Helpers;
using NHibernate;

namespace ProducerEditor.Service
{
	[DataContract(Namespace = "http://schemas.datacontract.org/2004/07/ProducerEditor.Service")]
	public class Query
	{
		public Query()
		{}

		public Query(string field, object value)
		{
			Field = field;
			Value = value;
		}

		[DataMember]
		public string Field { get; set; }
		[DataMember]
		public object Value { get; set; }
	}

	[DataContract(Namespace = "http://schemas.datacontract.org/2004/07/ProducerEditor.Service")]
	public class OffersQuery : Query
	{
		public IQuery Apply(ISession session)
		{
			var filter = "";
			var sort = "cd.FirmCode";
			if (Field == "CatalogId")
				filter = "p.CatalogId = :CatalogId";
			else if (Field == "ProducerId")
				filter = "c.CodeFirmCr = :ProducerId";
			else if (Field == "ProducerSynonymId")
			{
				filter = "c.SynonymFirmCrCode = :ProducerSynonymId";
				sort = "s.Synonym, sfc.Synonym";
			}

			var query = session.CreateSQLQuery(String.Format(@"
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
order by {1}", filter, sort))
				.SetResultTransformer(new AliasToPropertyTransformer(typeof (OfferView)));

			if (String.IsNullOrEmpty(filter))
				throw new Exception(String.Format("Не знаю как фильтровать по полю, {0}", Field));
			query.SetParameter(Field, Value);
			return query;
		}
	}
}
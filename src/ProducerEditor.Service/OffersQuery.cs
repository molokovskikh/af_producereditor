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

		public static ISQLQuery GetQuery(ISession session, Query query, string sql)
		{
			var where = "";
			if (query != null)
			{
				var tableField = "";
				if (query.Field == "CatalogName")
					tableField = "c.Name";
				else if (query.Field == "ProducerId")
					tableField = "a.ProducerId";
				else if (query.Field == "CatalogId")
					tableField = "a.CatalogId";
				var compare = "=";
				if (query.Value is string)
					compare = " like ";
				if (String.IsNullOrEmpty(tableField))
					throw new Exception(String.Format("Не знаю как фильтровать по полю {0}", query.Field));

				where = String.Format("where {0} {1} :{2}", tableField, compare, query.Field);
			}

			var sqlQuery = session.CreateSQLQuery(String.Format(sql, where));
			if (query != null)
				sqlQuery.SetParameter(query.Field, query.Value);
			return sqlQuery;
		}
	}

	[DataContract(Namespace = "http://schemas.datacontract.org/2004/07/ProducerEditor.Service")]
	public class OffersQuery : Query
	{
		public IQuery Apply(ISession session)
		{
			var filter = "";
			var sort = "s.Id";
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
select s.Name as Supplier,
sa.Synonym as ProductSynonym, 
sfc.Synonym as ProducerSynonym
from farm.core0 c
  join catalogs.Products p on p.Id = c.ProductId
  join farm.SynonymArchive sa on sa.SynonymCode = c.SynonymCode
  join farm.SynonymFirmCr sfc on sfc.SynonymFirmCrCode = c.SynonymFirmCrCode
  join usersettings.PricesData pd on pd.PriceCode = c.PriceCode
    join Customers.Suppliers s on s.Id = pd.FirmCode
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
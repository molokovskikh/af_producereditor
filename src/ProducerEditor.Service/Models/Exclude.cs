using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Common.Models.Helpers;
using NHibernate;
using NHibernate.Mapping.Attributes;

namespace ProducerEditor.Service
{
	public class UniuqAttribute : Attribute
	{}

	[Class(Table = "Farm.Excludes")]
	public class Exclude
	{
		[Id(0, Name = "Id")]
		[Generator(1, Class = "native")]
		public virtual uint Id { get; set; }

		[Property]
		public virtual string ProducerSynonym { get; set; }

		[ManyToOne(ClassType = typeof (CatalogProduct), Column = "CatalogId")]
		public virtual CatalogProduct CatalogProduct { get; set; }

		[ManyToOne(ClassType = typeof (Price), Column = "PriceCode")]
		public virtual Price Price { get; set; }

		[Property]
		public virtual bool DoNotShow { get; set; }
	}

	public class TypedQuery<T>
	{
		public Common.MySql.Query Query { get; set; }
		public ISession Session { get; set; }

		public TypedQuery(ISession session, Common.MySql.Query query)
		{
			Session = session;
			Query = query;
		}

		public TypedQuery<T> Filter(string filter, object parameters)
		{
			Query.Where(filter, parameters);
			return this;
		}
	}

	public static class QueryExtension
	{
		public static TypedQuery<T> Query<T>(this ISession session)
		{
			return new TypedQuery<T>(session, new Common.MySql.Query()
				.Select(@"
e.Id,
c.Name as Catalog,
e.ProducerSynonym,
r.Region,
cd.ShortName as Supplier,
ifnull(syn.Synonym, synarch.Synonym) as OriginalSynonym,
e.OriginalSynonymId,
e.Operator")
				.From(@"
farm.Excludes e
	join Catalogs.Catalog c on c.Id = e.CatalogId
	left join farm.Synonym syn on syn.SynonymCode = e.OriginalSynonymId
	left join farm.SynonymArchive synarch on synarch.SynonymCode = e.OriginalSynonymId
	join usersettings.PricesData pd on pd.PriceCode = e.PriceCode
		join usersettings.ClientsData cd on cd.FirmCode = pd.FirmCode
		join farm.Regions r on r.RegionCode = cd.RegionCode")
				.Where("e.DoNotShow = 0 and cd.FirmSegment = 0")
				.OrderBy("e.CreatedOn"));
		}

		public static Pager<T> Page<T>(this TypedQuery<T> query, uint page)
		{
			query.Query.Params(new {begin = page*1000});
			query.Query.Limit(":begin, 100");

			var sqlQuery = query.Session.CreateSQLQuery(query.Query.ToSql());
			foreach (var parameters in query.Query.GetParameters())
				sqlQuery.SetParameter(parameters.Key, parameters.Value);

			var items = sqlQuery
				.SetResultTransformer(new AliasToPropertyTransformer(typeof (T)))
				.List<T>();

			query.Query.SelectParts.Clear();
			query.Query.SelectParts.Add("count(*)");
			query.Query.Limit(null);

			sqlQuery = query.Session.CreateSQLQuery(query.Query.ToSql());
			foreach (var parameters in query.Query.GetParameters().Where(p => p.Key != "begin"))
				sqlQuery.SetParameter(parameters.Key, parameters.Value);

			var total = sqlQuery
				.UniqueResult<long>();
			return new Pager<T>(page, (uint) total, items);
		}
	}
}
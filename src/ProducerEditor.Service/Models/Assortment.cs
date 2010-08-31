using System;
using System.Linq;
using System.Runtime.Serialization;
using Common.Models.Helpers;
using MySql.Data.MySqlClient;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Mapping.Attributes;

namespace ProducerEditor.Service
{
	[DataContract(Namespace = "http://schemas.datacontract.org/2004/07/ProducerEditor.Service", Name = "Assortment")]
	public class AssortmentDto
	{
		[DataMember]
		public uint Id { get; set; }
		[DataMember]
		public string Product { get; set; }
		[DataMember]
		public string Producer { get; set; }
		[DataMember]
		public string ProducerId { get; set; }
		[DataMember]
		public bool Checked { get; set; }
	}

	[Class(Table = "Catalogs.Assortment")]
	public class Assortment
	{
		public Assortment()
		{}

		public Assortment(CatalogProduct product, Producer producer)
		{
			CatalogProduct = product;
			Producer = producer;
		}

		[Id(0, Name = "Id")]
		[Generator(1, Class = "native")]
		public virtual uint Id { get; set; }

		[ManyToOne(ClassType = typeof (CatalogProduct), Column = "CatalogId")]
		public virtual CatalogProduct CatalogProduct { get; set; }

		[ManyToOne(ClassType = typeof (Producer), Column = "ProducerId")]
		public virtual Producer Producer { get; set; }

		[Property]
		public virtual bool Checked { get; set; }

		public static Pager<AssortmentDto> Search(ISession session, uint page, Query query)
		{
			var assortments = GetQuery(session, query, @"
select a.Id,
		pr.Name as Producer,
		c.Name as Product,
		a.Checked,
		a.ProducerId
from catalogs.Assortment a
	join catalogs.Producers pr on pr.Id = a.ProducerId
	join Catalogs.Catalog c on a.CatalogId = c.id
{0}
order by c.Name
limit :begin, 100")
				.SetParameter("begin", page * 100)
				.SetResultTransformer(new AliasToPropertyTransformer(typeof(AssortmentDto)))
				.List<AssortmentDto>();
			var count = GetQuery(session, query, @"
select count(*)
from catalogs.Assortment a 
	join Catalogs.Catalog c on a.CatalogId = c.id
	join catalogs.Producers pr on pr.Id = a.ProducerId
{0}").UniqueResult<long>();

			return new Pager<AssortmentDto>(page, (uint)count, assortments);
		}

		private static ISQLQuery GetQuery(ISession session, Query query, string sql)
		{
			var where = "";
			if (query != null)
			{
				var tableField = "";
				if (query.Field == "CatalogName")
					tableField = "c.Name";
				else if (query.Field == "ProducerId")
					tableField = "a.ProducerId";
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

		public static uint GetPage(ISession session, uint assortimentId)
		{
			//nhibernate воспринимает : как начало параметра по этому, ado
			var connection = (MySqlConnection) session.Connection;
			var command =new MySqlCommand(@"
set @i = 0;

select assortmentIndex
from (
select @i := @i + 1 as assortmentIndex, a.id
from catalogs.Assortment a
join Catalogs.Catalog as c on c.Id = a.CatalogId
order by c.name
) as c
where c.Id = ?id", connection);
			command.Parameters.AddWithValue("?id", assortimentId);
			var value = command.ExecuteScalar();

			if (value == DBNull.Value)
				return 0;
			return (Convert.ToUInt32(value) / 100);
		}

		public virtual bool Exist(ISession session)
		{
			return (from assortment in session.Linq<Assortment>()
				where assortment.Producer == Producer
					&& assortment.CatalogProduct == CatalogProduct
				select assortment).FirstOrDefault() != null;
		}
	}
}
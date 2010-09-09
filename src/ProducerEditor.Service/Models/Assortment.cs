using System;
using System.Linq;
using Common.Models.Helpers;
using MySql.Data.MySqlClient;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Mapping.Attributes;
using ProducerEditor.Contract;

namespace ProducerEditor.Service.Models
{
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
			var assortments = Query.GetQuery(session, query, @"
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
			var count = Query.GetQuery(session, query, @"
select count(*)
from catalogs.Assortment a 
	join Catalogs.Catalog c on a.CatalogId = c.id
	join catalogs.Producers pr on pr.Id = a.ProducerId
{0}").UniqueResult<long>();

			return new Pager<AssortmentDto>(page, (uint)count, assortments);
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
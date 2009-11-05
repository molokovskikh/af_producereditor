using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using MySql.Data.MySqlClient;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Mapping.Attributes;
using NHibernate.Transform;

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

		public static IList<AssortmentDto> Load(ISession session, uint page)
		{
			//cсортировка должна производить по наименованию каталожного продукта и по производителю 
			//но если добавить двойную сортировку то mysql делает full table scan
			//по этому как то так
			return session.CreateSQLQuery(@"
select	a.Id, 
		pr.Name as Producer,
		c.Name as Product,
		a.Checked
from catalogs.Assortment a
	join catalogs.Producers pr on pr.Id = a.ProducerId
	join Catalogs.Catalog as c on c.Id = a.CatalogId
order by Product
limit :begin, 100")
				.SetParameter("begin", page * 100)
				.SetResultTransformer(Transformers.AliasToBean<AssortmentDto>())
				.List<AssortmentDto>();
		}

		public static uint TotalPages(ISession session)
		{
			return (uint) session.CreateSQLQuery(@"select count(*) from catalogs.Assortment").UniqueResult<long>() / 100;
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
			return Convert.ToUInt32(value) / 100;
		}

		public static int Find(ISession session, string text)
		{
			var assortiment = session.CreateSQLQuery(@"
select	a.Id,
		pr.Name as Producer,
		c.Name as Product,
		a.Checked
from catalogs.Assortment a
	join catalogs.Producers pr on pr.Id = a.ProducerId
	join Catalogs.Catalog as c on a.CatalogId = c.id
where c.Name like :text
limit 1")
				.SetParameter("text", text + "%")
				.SetResultTransformer(Transformers.AliasToBean<AssortmentDto>())
				.UniqueResult<AssortmentDto>();
			if (assortiment == null)
				return -1;
			return (int) GetPage(session, assortiment.Id);
		}

		public virtual bool Exist(ISession session)
		{
			return (
				from assortment in session.Linq<Assortment>()
				where assortment.Producer == Producer
					&& assortment.CatalogProduct == CatalogProduct
				select assortment).FirstOrDefault() != null;
		}
	}
}
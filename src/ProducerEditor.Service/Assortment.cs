using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
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

		public static IList<AssortmentDto> Load(ISession session, uint page)
		{
			//cсортировка должна производить по наименованию каталожного продукта и по производителю 
			//но если добавить двойную сортировку то mysql делает full table scan
			//по этому как то так
			return session.CreateSQLQuery(@"
select	a.Id, 
		pr.Name as Producer,
		c.Name as Product
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
			var count = session
				.CreateSQLQuery(@"
select count(*) 
from catalogs.Assortment a
	join Catalogs.Catalog as c on c.Id = a.CatalogId
where a.id < :id
order by c.name")
				.SetParameter("id", assortimentId)
				.UniqueResult<long>();
			return (uint) (count/100);
		}

		public static uint Find(ISession session, string text)
		{
			var assortiment = session.CreateSQLQuery(@"
select	a.Id,
		pr.Name as Producer,
		c.Name as Product
from catalogs.Assortment a
	join catalogs.Producers pr on pr.Id = a.ProducerId
	join Catalogs.Catalog as c on a.CatalogId = c.id
where c.Name like :text
limit 1")
				.SetParameter("text", text + "%")
				.SetResultTransformer(Transformers.AliasToBean<AssortmentDto>())
				.UniqueResult<AssortmentDto>();
			return GetPage(session, assortiment.Id);
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
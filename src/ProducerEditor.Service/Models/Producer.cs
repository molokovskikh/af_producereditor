using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Common.Tools;
using NHibernate;
using NHibernate.Mapping.Attributes;

namespace ProducerEditor.Service.Models
{
	[DataContract(Namespace = "http://schemas.datacontract.org/2004/07/ProducerEditor.Service"/*, Name = "Producer"*/)]
	public class ProducerDto
	{
		[DataMember]
		public virtual uint Id { get; set; }

		[DataMember]
		public virtual string Name { get; set; }

		[DataMember]
		public virtual bool Checked { get; set; }

		[DataMember]
		public virtual bool HasOffers { get; set;}
	}

	[Class(Table = "Catalogs.Producers")]
	public class Producer
	{
		[Id(0, Name = "Id")]
		[Generator(1, Class = "native")]
		public virtual uint Id { get; set; }

		[Property]
		public virtual string Name { get; set; }

		[Property]
		public virtual bool Checked { get; set; }

		[Bag(0, Lazy = true, Inverse = true, Cascade = "all")]
		[Key(1, Column = "ProducerId")]
		[OneToMany(2, ClassType = typeof (Assortment))]
		public virtual IList<Assortment> Assortments { get; set; }

		[Bag(0, Lazy = true, Inverse = true, Cascade = "all")]
		[Key(1, Column = "CodeFirmCr")]
		[OneToMany(2, ClassType = typeof (ProducerSynonym))]
		public virtual IList<ProducerSynonym> Synonyms { get; set; }

		[Bag(0, Lazy = true, Inverse = true, Cascade = "all")]
		[Key(1, Column = "ProducerId")]
		[OneToMany(2, ClassType = typeof (ProducerEquivalent))]
		public virtual IList<ProducerEquivalent> Equivalents { get; set; }

		public virtual void MergeToEquivalent(Producer producer, ISession session)
		{
			AddEquivalent(producer.Name);

			producer.Assortments
				.Where(a => Assortments.All(x => x.CatalogProduct.Id != a.CatalogProduct.Id))
				.Select(a => new Assortment(a.CatalogProduct, this){Checked =  a.Checked})
				.Each(a => Assortments.Add(a));

			producer.Equivalents
				.Each(e => AddEquivalent(e.Name));
		}

		public virtual void AddEquivalent(string name)
		{
			if (Equivalents.Any(e => e.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase)))
				return;

			Equivalents.Add(new ProducerEquivalent(this, name));
		}
	}
}
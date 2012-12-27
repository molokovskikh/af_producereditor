using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate.Mapping.Attributes;

namespace ProducerEditor.Service.Models
{
	[Class(Table = "Farm.Core0")]
	public class Core
	{
		[Id(0, Name = "Id"), Generator(1, Class = "native")]
		public virtual ulong Id { get; set; }

		[ManyToOne(ClassType = typeof(Price), Column = "PriceCode")]
		public virtual Price Price { get; set; }

		[ManyToOne(ClassType = typeof(ProducerSynonym), Column = "SynonymFirmCrCode")]
		public virtual ProducerSynonym ProducerSynonym { get; set; }
	}
}
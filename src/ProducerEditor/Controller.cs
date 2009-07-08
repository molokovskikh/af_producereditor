using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NHibernate;

namespace ProducerEditor
{
	public class Controller
	{
		public IList<Producer> FindProducers(string name)
		{
			return (from producer in Producer.Queryable
			        orderby producer.Name
			        select producer).ToList();
		}

		public void Join(Producer source, Producer target)
		{
			ISession session = null;
			var producerEquivalent = new ProducerEquivalent
			                         	{
			                         		Name = source.Name,
			                         		Producer = target,
			                         	};

			using (session.BeginTransaction())
			{
				session.Save(producerEquivalent);
				session.Delete(source);
			}
		}
	}
}

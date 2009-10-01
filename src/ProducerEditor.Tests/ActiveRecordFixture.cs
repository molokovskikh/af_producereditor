using System;
using System.Linq;
using Castle.ActiveRecord;
using NUnit.Framework;
using ProducerEditor.Service;
using ProducerEditor.Views;
using Producer = ProducerEditor.Models.Producer;

namespace ProducerEditor.Tests
{
	[TestFixture]
	public class ActiveRecordFixture
	{
		[SetUp]
		public void Setup()
		{
			Initializer.Initialize();
		}

		[Test]
		public void Check_active_record_configuration()
		{
			var p = Producer.Find(2575u);
			Console.Write(p);
		}

		[Test]
		public void Set_checked_property()
		{
			Producer producer;
			using(new SessionScope())
			{
				producer = Producer.Queryable.Where(p => !p.Checked).First();
				producer.Checked = true;
				producer.Update();
			}

			using (new SessionScope())
			{
				var reloadedProducer = Producer.Find(producer.Id);
				Assert.That(reloadedProducer.Checked);
			}
		}

		[Test]
		public void Load_synonym_report()
		{
			With.Session(s => {
				SynonymReportItem.Load(s, DateTime.Today.AddDays(-1), DateTime.Today);
			});
		}
	}
}

using System.Linq;
using System.Reflection;
using log4net.Config;
using MySql.Data.MySqlClient;
using NHibernate;
using NHibernate.Linq;
using NUnit.Framework;
using ProducerEditor.Service;
using System;
using Castle.Windsor.Installer;
using ProducerEditor.Service.Helpers;
using ProducerEditor.Service.Models;
using Rhino.Mocks;

namespace ProducerEditor.Tests
{
	[TestFixture]
	public class AssortmentFixture : BaseFixture
	{
		[Test]
		public void Total()
		{
			var assortments = Assortment.Search(session, 0, null);
			Assert.That(assortments.TotalPages, Is.GreaterThan(0));
		}

		[Test]
		public void Get_page()
		{
			var assortments = Assortment.Search(session, 3, null);
			Assert.That(Assortment.GetPage(session, assortments.Content[3].Id), Is.GreaterThan(3));
		}

		[Test]
		public void Search_text()
		{
			var assortments = Assortment.Search(session, 5, null);
			var findedAssortments = Assortment.Search(session, 0, new Query("ProducerId", assortments.Content[1].ProducerId));
			Assert.That(findedAssortments.Content.Count, Is.GreaterThan(1));
			Assert.That(findedAssortments.Page, Is.EqualTo(0));
			Assert.That(findedAssortments.TotalPages, Is.EqualTo(1));
		}

		[Test]
		public void Get_assortment_for_producer()
		{
			var testProducer = session.Query<Producer>().FirstOrDefault(p => p.Name == "test-producer");
			if (testProducer != null)
				session.Delete(testProducer);
			session.Flush();

			var producer = new Producer();
			producer.Name = "test-producer";
			session.Save(producer);

			var assortment = new Assortment(session.Query<CatalogProduct>().First(), producer);
			session.Save(assortment);
			session.Flush();

			var producerId = producer.Id;
			session.Clear();

			var assortments = Assortment.Search(session, 0, new Query("ProducerId", producerId));
			Assert.That(assortments.Content.Count, Is.EqualTo(1));
			var assortment1 = assortments.Content.Single();
			Assert.That(assortment1.Producer, Is.EqualTo("test-producer"));
		}

		private Producer CreateTestProducer(ISession session)
		{
			const string ProducerName = "test-producer";

			var testProducer = session.Query<Producer>().FirstOrDefault(p => p.Name == ProducerName);
			if (testProducer != null)
				session.Delete(testProducer);
			session.Flush();

			var producer = new Producer();
			producer.Name = ProducerName;
			session.Save(producer);
			return producer;
		}
	}
}
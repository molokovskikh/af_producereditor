using System;
using NUnit.Framework;
using ProducerEditor.Models;

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
		public void Load_synonym_report()
		{
			SynonymReportItem.Load(DateTime.Today.AddDays(-1), DateTime.Today);
		}
	}
}

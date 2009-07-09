using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net.Config;
using NUnit.Framework;
using ProducerEditor.Models;

namespace ProducerEditor.Tests
{
	[TestFixture]
	public class ActiveRecordFixture
	{
		[Test]
		public void Check_active_record_configuration()
		{
			Initialezer.Initialize();
			var p = Producer.Find(2575u);
			Console.Write(p);
		}

		[Test]
		public void Check_speed()
		{
			XmlConfigurator.Configure();
			Initialezer.Initialize();
			var controller = new Controller();
			Test(controller);
			Test(controller);
		}

		private void Test(Controller controller)
		{
			var begin = DateTime.Now;
			//var producers = controller.GetAllProducers("те");
			//Console.WriteLine(producers.Count);
			Console.WriteLine((DateTime.Now - begin).TotalMilliseconds);
		}
	}
}

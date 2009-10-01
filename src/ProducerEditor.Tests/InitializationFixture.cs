using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ProducerEditor.Service;

namespace ProducerEditor.Tests
{
	[TestFixture]
	public class InitializationFixture
	{
		[Test]
		public void Try_to_initialize()
		{
			Global.InitializeNHibernate();
		}
	}
}

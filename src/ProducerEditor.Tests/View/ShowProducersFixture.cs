using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Forms;
using NUnit.Framework;
using ProducerEditor.Contract;
using ProducerEditor.Infrastructure;
using ProducerEditor.Presenters;
using ProducerEditor.Views;
using Subway.VirtualTable;

namespace ProducerEditor.Tests.View
{
	[TestFixture]
	public class ShowProducersFixture
	{
		private ShowProducers view;
		private ShowProducersPresenter presenter;

		[SetUp]
		public void Setup()
		{
			view = new ShowProducers();
			presenter = (ShowProducersPresenter)view.Presenter;
			presenter.UnderTest = true;
		}

		[Test]
		public void Update_producer_on_search()
		{
			presenter.Search("фарм");
			Assert.That(presenter.Producers.Count, Is.Not.EqualTo(0));
			Assert.That(presenter.Producers.Count, Is.LessThan(ShowProducers.producers.Count));
			var host = view.Children().OfType<TableHost>().First(h => h.Name == "Producers");
			Assert.That(host.Table.TemplateManager.Source, Is.EqualTo(presenter.Producers));
		}

		[Test]
		public void Delete_equivalent()
		{
			presenter.Producers = new ObservableCollection<ProducerDto>(ShowProducers.producers);
			presenter.CurrentChanged(presenter.Producers[1]);
			var equivalent = presenter.ProducerEquivalents[0];

			presenter.Delete(equivalent);
		}

		[Test]
		public void Rename_equivalent()
		{
			presenter.Producers = new ObservableCollection<ProducerDto>(ShowProducers.producers);
			presenter.CurrentChanged(presenter.Producers[1]);
			var equivalent = presenter.ProducerEquivalents[0];
			presenter.Dialog += form => {
				((RenameView)form).Value = "test";
				return DialogResult.OK;
			};
			presenter.Rename(equivalent);
		}
	}
}
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ProducerEditor.Contract;
using ProducerEditor.Models;
using ProducerEditor.Views;

namespace ProducerEditor.Presenters
{
	public class ShowProducersPresenter : Presenter
	{
		private List<ProducerDto> producers;
		private List<string> equivalents;
		private List<ProducerSynonymDto> producerSynonyms;

		public ShowProducersPresenter()
		{
			producers = new List<ProducerDto>();
			equivalents = new List<string>();
			producerSynonyms = new List<ProducerSynonymDto>();
		}

		public List<ProducerDto> Producers
		{
			get
			{
				return producers;
			}
			set
			{
				producers = value;
				OnUpdate("Producers", value);
			}
		}

		protected List<ProducerSynonymDto> ProducerSynonyms
		{
			get
			{
				return producerSynonyms;
			}
			set
			{
				producerSynonyms = value;
				OnUpdate("ProducerSynonyms", value);
			}
		}

		protected List<string> Equivalents
		{
			get
			{
				return equivalents;
			}
			set
			{
				equivalents = value;
				OnUpdate("Equivalents", value);
			}
		}

		public void Search(string text)
		{
			text = text ?? "";
			var allProducers = ShowProducers.producers;
			if (string.IsNullOrEmpty(text))
				Producers = allProducers;
			else
				Producers = Request(r => r.GetProducers(text)).ToList();

			if (Producers.Count == 0) {
				MessageBox.Show("По вашему запросу ничеого не найдено", "Результаты поиска",
					MessageBoxButtons.OK,
					MessageBoxIcon.Warning);
			}
		}

		public void CurrentChanged(ProducerDto producer)
		{
			if (producer == null)
				return;
			Action(s => {
				ProducerSynonyms = s.GetSynonyms(producer.Id).ToList();
				Equivalents = s.GetEquivalents(producer.Id).ToList();
			});
		}
	}
}
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using ProducerEditor.Contract;
using ProducerEditor.Views;

namespace ProducerEditor.Presenters
{
	public class ShowProducersPresenter : Presenter
	{
		private ObservableCollection<ProducerDto> producers;
		private ObservableCollection<ProducerEquivalentDto> _producerEquivalents;
		private ObservableCollection<ProducerSynonymDto> producerSynonyms;

		public ShowProducersPresenter()
		{
			producers = new ObservableCollection<ProducerDto>();
			_producerEquivalents = new ObservableCollection<ProducerEquivalentDto>();
			producerSynonyms = new ObservableCollection<ProducerSynonymDto>();
		}

		public ObservableCollection<ProducerDto> Producers
		{
			get { return producers; }
			set
			{
				producers = value;
				OnUpdate("Producers", value);
			}
		}

		public ObservableCollection<ProducerSynonymDto> ProducerSynonyms
		{
			get { return producerSynonyms; }
			set
			{
				producerSynonyms = value;
				OnUpdate("ProducerSynonyms", value);
			}
		}

		public ObservableCollection<ProducerEquivalentDto> ProducerEquivalents
		{
			get { return _producerEquivalents; }
			set
			{
				_producerEquivalents = value;
				OnUpdate("ProducerEquivalents", value);
			}
		}

		public void Search(string text)
		{
			text = text ?? "";
			var allProducers = ShowProducers.producers;
			if (string.IsNullOrEmpty(text))
				Producers = new ObservableCollection<ProducerDto>(allProducers);
			else
				Producers = new ObservableCollection<ProducerDto>(Request(r => r.GetProducers(text)));

			if (Producers.Count == 0) {
				MessageBox.Show("По вашему запросу ничеого не найдено", "Результаты поиска",
					MessageBoxButtons.OK,
					MessageBoxIcon.Warning);
			}
		}

		public void CurrentChanged(ProducerDto producer)
		{
			Action(s => {
				ProducerSynonyms = new ObservableCollection<ProducerSynonymDto>(s.GetSynonyms(producer.Id));
				ProducerEquivalents = new ObservableCollection<ProducerEquivalentDto>(s.GetEquivalents(producer.Id));
			});
		}

		public void Delete(ProducerSynonymDto producerSynonym)
		{
			Action(s => s.DeleteProducerSynonym(producerSynonym.Id));
			ProducerSynonyms.Remove(producerSynonym);
		}

		public void Delete(ProducerDto producer)
		{
			var dialogResult = MessageBox.Show(String.Format("Удалить производителя \"{0}\"", producer.Name), "Удаление производителя",
				MessageBoxButtons.OKCancel,
				MessageBoxIcon.Question);

			if (dialogResult == DialogResult.Cancel)
				return;

			Action(s => {
				s.DeleteProducer(producer.Id);
				producers.Remove(producer);
				Producers.Remove(producer);
			});
		}

		public void Delete(ProducerEquivalentDto equivalent)
		{
			Action(s => {
				s.DeleteEquivalent(equivalent.Id);
				ProducerEquivalents.Remove(equivalent);
			});
		}

		public void Rename(ProducerDto producer)
		{
			var rename = new RenameView(producer.Name);
			rename.Text = "Переименование производителя";
			rename.CheckValidation += () => {
				if (String.IsNullOrEmpty(rename.Value))
					return "Название производителя не может быть пустым";

				var existsProducer = ShowProducers.producers.FirstOrDefault(p =>
					p.Name.Equals(rename.Value, StringComparison.CurrentCultureIgnoreCase)
						&& p.Id != producer.Id);
				if (existsProducer != null)
					return "Такой производитель уже существует";

				return null;
			};

			if (ShowDialog(rename) != DialogResult.Cancel) {
				Action(s => {
					producer.Name = rename.Value.ToUpper();
					s.UpdateProducer(producer);
				});
				RefreshView(Producers);
			}
		}

		public void Rename(ProducerEquivalentDto equivalent)
		{
			var rename = new RenameView(equivalent.Name);
			rename.Text = "Переименование эквивалента";
			rename.CheckValidation += () => {
				if (String.IsNullOrEmpty(rename.Value))
					return "Название эквивалента не может быть пустым";

				var existsProducer = ProducerEquivalents.FirstOrDefault(p =>
					p.Name.Equals(rename.Value, StringComparison.CurrentCultureIgnoreCase)
						&& p.Id != equivalent.Id);
				if (existsProducer != null)
					return "Такой эквивалент уже существует";

				return null;
			};
			if (ShowDialog(rename) != DialogResult.Cancel) {
				Action(s => {
					equivalent.Name = rename.Value.ToUpper();
					s.Update(equivalent);
				});
				RefreshView(ProducerEquivalents);
			}
		}
	}
}
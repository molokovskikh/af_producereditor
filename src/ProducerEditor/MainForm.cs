using System.Windows.Forms;

namespace ProducerEditor
{
	public class MainForm : Form
	{
		private Controller _controller = new Controller();
		public MainForm()
		{
			Text = "Редактор каталога производителей";
			var toolBar = new ToolStrip();
			toolBar.Items.Add(new ToolStripTextBox
			                  	{

			                  	});
			toolBar.Items.Add(new ToolStripButton
			                  	{
									Text = "Поиск"
			                  	});
			toolBar.Items.Add(new ToolStripSeparator());
			toolBar.Items.Add(new ToolStripButton
			                  	{
			                  		Text = "Переименовать"
			                  	});
			toolBar.Items.Add(new ToolStripButton
			                  	{
			                  		Text = "Объединить"
			                  	});
			var split = new SplitContainer
			            	{
			            		Dock = DockStyle.Fill
			            	};
			Controls.Add(toolBar);
			Controls.Add(split);
		}

		public void Search(string searchText)
		{

		}
	}

	public class SearchProducerForJion
	{
		
	}
}
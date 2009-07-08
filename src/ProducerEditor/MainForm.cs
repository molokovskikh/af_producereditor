using System.Windows.Forms;

namespace ProducerEditor
{
	public class MainForm : Form
	{
		private Controller _controller = new Controller();
		public MainForm()
		{
			Text = "�������� �������� ��������������";
			var toolBar = new ToolStrip();
			toolBar.Items.Add(new ToolStripTextBox
			                  	{

			                  	});
			toolBar.Items.Add(new ToolStripButton
			                  	{
									Text = "�����"
			                  	});
			toolBar.Items.Add(new ToolStripSeparator());
			toolBar.Items.Add(new ToolStripButton
			                  	{
			                  		Text = "�������������"
			                  	});
			toolBar.Items.Add(new ToolStripButton
			                  	{
			                  		Text = "����������"
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
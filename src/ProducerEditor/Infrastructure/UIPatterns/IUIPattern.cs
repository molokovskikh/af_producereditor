using System.Windows.Forms;

namespace ProducerEditor.Infrastructure.UIPatterns
{
	public interface IUIPattern
	{
		void Apply(Form view);
		bool IsApplicable(Form view);
	}
}
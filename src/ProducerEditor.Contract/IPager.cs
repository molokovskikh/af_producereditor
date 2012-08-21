namespace ProducerEditor.Contract
{
	public interface IPager
	{
		uint Page { get; set; }
		uint TotalPages { get; set; }
	}
}
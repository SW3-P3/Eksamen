public class Offer
{
	public int ID { get; set; }
	public ICollection<Item> GenericItems { get; set; }
	public string Heading { get; set; }
	public string Store_ID { get; set; }
	public string Store { get; set; }
	public DateTime Begin { get; set; }
	public DateTime End { get; set; }
	public decimal Price { get; set; }
	public string Unit { get; set; }
	public string Image_URL { get; set; }

	public Offer() { GenericItems = new List<Item>(); }
}
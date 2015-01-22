public class Recipe
{
	public int ID { get; set; }
	public string OriginalAuthorName { get; set; }
	public string AuthorName { get; set; }
	public string Title { get; set; }
	public ICollection<Item> Ingredients { get; set; }
	public int Minutes { get; set; }
	public string Instructions { get; set; }
	public ICollection<Rating> Ratings { get; set; }
	...
}
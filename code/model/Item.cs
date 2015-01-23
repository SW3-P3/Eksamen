public class Item
{
	public int ID { get; set; }
	public string Name { get; set; }
	public ICollection<Offer> Offers { get; set; }
	public ICollection<Recipe> OnRecipes { get; set; }
	public ICollection<ShoppingList> OnShoppinglists { get; set; }
	...
}
public class ShoppingList
{
	public int ID { get; set; }
	public string Title { get; set; }
	public ICollection<Item> Items { get; set;}
	public ICollection<User> Users { get; set;} 
	...
}

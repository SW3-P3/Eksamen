public ActionResult AddItem(int id, string name, double? amount, string unit)
{
	if(name.Trim() == string.Empty) {
		return RedirectToAction("Details/" + id);
	}
	if(amount == null) {
		amount = 0;
	}
	ShoppingList shoppingList = db.ShoppingLists.Include(s => s.Items).Where(x => x.ID == id).Single();
	Item tmpItem;

	//Search in GenericLItems for item
	Item knownItem = null;
	if(db.Items.Count() > 0)
		knownItem = db.Items.Where(i => i.Name.CompareTo(name) == 0).SingleOrDefault();

	if(knownItem != null) {
		tmpItem = knownItem;
	} else {
		tmpItem = new Item() { Name = name };
	}

	if(shoppingList.Items.Contains(tmpItem)) {
		db.ShoppingList_Item.Where(x => x.ItemID == tmpItem.ID && x.ShoppingListID == id).Single().Amount += (double)amount;
	} else {
		var shoppingListItem = new ShoppingList_Item { Item = tmpItem, ShoppingList = shoppingList, Amount = (double)amount, Unit = unit };

		db.ShoppingList_Item.Add(shoppingListItem);

		shoppingList.Items.Add(tmpItem);
	}

	db.SaveChanges();
	return RedirectToAction("Details/" + id);
}

public ActionResult RemoveItem(int id, int itemID)
{
	//Find relevant shoppingList and include the items
	var tmp = db.ShoppingLists.Include(s => s.Items).ToList();
	ShoppingList shoppingList = tmp.Find(x => x.ID == id);

	//Find the item to be deleted, and remove it from the shopping list
	var rmItem = shoppingList.Items.ToList().Find(x => x.ID == itemID);
	shoppingList.Items.Remove(rmItem);

	//Find the item in the ShoppingList_Item table
	var rmShoppingListItem = db.ShoppingList_Item
		.Where(x => x.ItemID == itemID
		&& x.ShoppingListID == id)
		.SingleOrDefault();
	//... and remove it
	if(rmShoppingListItem != null)
		db.ShoppingList_Item.Remove(rmShoppingListItem);

	//Save the changes in the database
	db.SaveChanges();

	//Update the users view of the shoppinglist
	return RedirectToAction("Details/" + id);
}
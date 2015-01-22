using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Web.Mvc;
using ProjectFood.Models;

namespace ProjectFood.Controllers
{
    public class ShoppingListsController : Controller
    {
        private IDataBaseContext _db = new DataBaseContext();

        public ShoppingListsController() { }

        public ShoppingListsController(IDataBaseContext context)
        {
            _db = context;
        }

        // GET: ShoppingLists
        public ActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                ViewBag.NumItems = _db.ShoppingLists.Include(s => s.Items);

                var userLists =
                    _db.Users.Include(u => u.ShoppingLists)
                        .First(u => u.Username == User.Identity.Name)
                        .ShoppingLists.Where(l => l.Users.Count == 1);

                var sharedLists =
                    _db.Users.Include(u => u.ShoppingLists)
                        .First(u => u.Username == User.Identity.Name)
                        .ShoppingLists.Where(l => l.Users.Count > 1);
                ViewBag.SharedLists = sharedLists;
               

                return View(userLists);
            }

            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action()});
        }

        // GET: ShoppingLists/Details/5
        public ActionResult Details(int? id)
        {           

            if(id == null) {
                return RedirectToAction("Index");
            }
            ShoppingList shoppingList = findShoppingListFromID(id);

            ViewBag.ShoppingList_Item = _db.ShoppingList_Item.Where(x => x.ShoppingListID == id).ToList();

            if(shoppingList == null) {
                return HttpNotFound();
            }

            foreach (var item in shoppingList.Items)
            {
                item.Offers = GetOffersForItem(_db, item, _db.Users.FirstOrDefault(u => u.Username == User.Identity.Name)).OrderBy(x=>x.Store).ToList();
            }

            _db.SaveChanges();

            if (User.Identity.IsAuthenticated &&
                _db.Users
                .Include(s => s.ShoppingLists)
                .First(u => u.Username == User.Identity.Name)
                .ShoppingLists.ToList().Exists(s => s.ID == id))
            {
                return View(shoppingList);
            }
                return RedirectToAction("Index");
        }

        // GET: ShoppingLists/Create
        public PartialViewResult Create()
        {
            return PartialView("_CreateShoppingList");
        }

        // POST: ShoppingLists/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ID,Title")] ShoppingList shoppingList, string from)
        {
            if(ModelState.IsValid) {              
                if (User.Identity.IsAuthenticated)
                {
                    _db.ShoppingLists.Add(shoppingList);
                    _db.Users.Include(u => u.ShoppingLists).First(u => u.Username == User.Identity.Name).ShoppingLists.Add(shoppingList);
                    _db.SaveChanges();
                }                               
            }

            string gotoUrl = from == "/ShoppingLists" ? "/ShoppingLists/Details/" + shoppingList.ID : from;

            return Redirect(gotoUrl);
        }

        // GET: ShoppingLists/Edit/5
        public ActionResult Edit(int? id)
        {
            if(id == null) {
                return RedirectToAction("Index");
            }
            ShoppingList shoppingList = _db.ShoppingLists.Find(id);
            if(shoppingList == null) {
                return HttpNotFound();
            }

            return RedirectToAction("Index");
        }

        // POST: ShoppingLists/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ID,Title")] ShoppingList shoppingList, string from)
        {
            if(ModelState.IsValid) {
                _db.MarkAsModified(shoppingList);
                _db.SaveChanges();

                return Redirect(from);
            }
            return RedirectToAction("Index");
        }

        // POST: ShoppingLists/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
           
            ShoppingList shoppingList = _db.ShoppingLists.Find(id);
            if (shoppingList.Users.Count == 1)
            {
                _db.ShoppingLists.Remove(shoppingList);
            }
            else
            {
                _db.Users.Include(x => x.ShoppingLists)
                    .First(u => u.Username == User.Identity.Name)
                    .ShoppingLists.Remove(shoppingList);

                var user = shoppingList.Users.First(u => u.Username == User.Identity.Name);
                shoppingList.Users.Remove(user);
            }
            _db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if(disposing) {
                _db.Dispose();
            }
            base.Dispose(disposing);
        }

        public ActionResult WatchList(int? itemID)
        {
            if(User.Identity.IsAuthenticated) {
                ViewBag.ShoppingLists =
                       _db.Users.Include(s => s.ShoppingLists)
                           .First(u => u.Username == User.Identity.Name)
                           .ShoppingLists.ToList();
                ViewBag.SelectedItem = itemID;
                var user = _db.Users.Include(w => w.WatchList.Items.Select(i => i.Offers)).First(u => u.Username == User.Identity.Name);
                if(user.WatchList == null) {    
                    user.WatchList = new ShoppingList { Title = "watchList"};

                    _db.SaveChanges();
                }

                foreach (var item in user.WatchList.Items)
                {
                    item.Offers = GetOffersForItem(_db, item, user).OrderBy(x => x.Store).ToList();
                }

                return View(user.WatchList);
            }

            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action() });
        }

        [HttpPost]
        public ActionResult ShareList(int? id, string email)
        {
            var shoppingList = _db.ShoppingLists.Include(x => x.Users).FirstOrDefault(x => x.ID == id);
            var user = _db.Users.Include(u => u.ShoppingLists).FirstOrDefault(u => u.Username == email);
            if (user == null)
            {
                return Json(new { Success = "false", Message = "Email ikke fundet" });
            }

            if (!user.ShoppingLists.Any(x => x.ID == shoppingList.ID))
            {
                user.ShoppingLists.Add(shoppingList);
                shoppingList.Users.Add(user);
                _db.SaveChanges();
                return Json(new { Success = "true", Message = "Delt med " + email });
            }

            return Json(new { Success = "false", Message = "Bruger er allerede med på Indkøbslisten" });
        }

        public ActionResult AddItem(int id, string name, double? amount, string unit, int? offerID, int? itemID)
        {
            
            ShoppingList shoppingList = findShoppingListFromID(id);
            Item tmpItem;

            if(name.Trim() == string.Empty || name.Length < 2) {
                return shoppingList.Title == "watchList" ? RedirectToAction("WatchList") : RedirectToAction("Details/" + id);
            }
            if(amount == null) {
                amount = 0;
            }

            //Search in GenericLItems for item
            Item knownItem = null;
            if(itemID != null) {
                knownItem = _db.Items.Find(itemID);
            }else if(_db.Items.Any()) { 
                knownItem = _db.Items.FirstOrDefault(i => i.Name.ToLower().CompareTo(name.ToLower()) == 0);
            }

            tmpItem = knownItem ?? new Item() { Name = name };

            if(shoppingList.Items.Contains(tmpItem)) {
                if(isOfferSelectedOnItem(id, tmpItem)) {
                    Item sameNameItem = new Item() { Name = name }; 
                    addToShoppingList_Item(sameNameItem, shoppingList, amount, unit);
                } else { 
                    _db.ShoppingList_Item.First(x => x.ItemID == tmpItem.ID && x.ShoppingListID == id).Amount += (double)amount;
                    _db.SaveChanges();
                }
            } else {
                addToShoppingList_Item(tmpItem, shoppingList, amount, unit);
            }

            if (offerID != null)
            {
                var derp = _db.ShoppingList_Item.Where(x => x.ShoppingList.ID == id && x.Item.Name == tmpItem.Name && x.selectedOffer != null).ToList();


                if(derp.Any())
                {

                    var shoppingListItems = _db.ShoppingList_Item.ToList();
                    var itemsForId = shoppingListItems.Where(x => x.ShoppingListID == id).ToList();
                    var item = itemsForId.Last(x => x.Item.Name == tmpItem.Name);
                    item.selectedOffer = _db.Offers.Find((int)offerID);
                }
                else
                {
                    var shoppingListItem = _db.ShoppingList_Item.FirstOrDefault(x => x.ShoppingList.ID == id && x.Item.ID == tmpItem.ID);
                if (shoppingListItem != null)
                    shoppingListItem.selectedOffer = _db.Offers.Find((int) offerID);
                }
                
                _db.SaveChanges();
            }

            return shoppingList.Title == "watchList" ? RedirectToAction("WatchList") : RedirectToAction("Details/" + id);
        }
        
        [HttpPost, ActionName("AddItem")]
        public ActionResult AddItemAjax(int id, string name, double? amount, string unit, int? offerID, int? itemID)
        {
            AddItem(id, name, amount, unit, offerID, itemID);

            return Json(new { shoppingListTitle = _db.ShoppingLists.Find(id).Title, offerID = offerID, itemID = itemID });
        }

        public ActionResult RemoveItem(int id, int itemID)
        {

            ShoppingList shoppingList = findShoppingListFromID(id);

            //Find the item to be deleted, and remove it from the shopping list
            var rmItem = shoppingList.Items.First(x => x.ID == itemID);
            shoppingList.Items.Remove(rmItem);

            //Find the item in the ShoppingList_Item table
            var rmShoppingListItem = _db.ShoppingList_Item.FirstOrDefault(x => x.ItemID == itemID && x.ShoppingListID == id);
            //... and remove it
            if(rmShoppingListItem != null)
                _db.ShoppingList_Item.Remove(rmShoppingListItem);

            //Save the changes in the database
            _db.SaveChanges();

            //Update the users view of the shoppinglist
            return shoppingList.Title == "watchList" ? RedirectToAction("WatchList") : RedirectToAction("Details/" + id);
        }

        public ActionResult ClearShoppingList(int id)
        {
            ShoppingList shoppingList = findShoppingListFromID(id);

            if(User.Identity.IsAuthenticated) {
                var user = _db.Users.Include(u => u.ShoppingLists).FirstOrDefault(u => u.Username == User.Identity.Name);

                if(user != null && user.ShoppingLists.FirstOrDefault(s => s.ID == id) != null) {
                    shoppingList.Items.Clear();
                    var itemRels = _db.ShoppingList_Item.Where(x => x.ShoppingListID == id);
                    foreach (var shoppingListItem in itemRels.ToList())
                    {
                        _db.ShoppingList_Item.Remove(shoppingListItem);

                    }
                    _db.SaveChanges();  
                }
            }

            return shoppingList.Title == "watchList" ? RedirectToAction("WatchList") : RedirectToAction("Details/" + id);
        }

        [HttpPost]
        public ActionResult ToggleItemBought(int id, int itemID)
        {
            var tmpItem = _db.ShoppingList_Item.First(i => i.ItemID == itemID && i.ShoppingListID == id);

            if (tmpItem == null)
                return Json(new
                {
                    Message = "DID NOT TWERK",
                    itemID = itemID,
                }, JsonRequestBehavior.AllowGet);

            tmpItem.Bought = tmpItem.Bought == false;

            _db.SaveChanges();
            return Json(new {
                Message = "Hajtroels",
                itemID = itemID,
            }, JsonRequestBehavior.AllowGet);
        }

        public static List<Offer> GetOffersForItem(IDataBaseContext db, Item item)
        {
            return db.OffersFiltered() 
                .Where(x => x.Heading.ToLower().Contains(item.Name.ToLower() + " ") || x.Heading.ToLower().Contains(" " + item.Name.ToLower()) ||
                            String.Equals(x.Heading.ToLower(), item.Name.ToLower())).ToList();
        }

        public static List<Offer> GetOffersForItem(IDataBaseContext db, Item item, User user)
        {
            return db.OffersFilteredByUserPrefs(user)
                .Where(x => x.Heading.ToLower().Contains(item.Name.ToLower() + " ") || x.Heading.ToLower().Contains(" " + item.Name.ToLower()) ||
                            String.Equals(x.Heading.ToLower(), item.Name.ToLower())).ToList();
        }

        [HttpPost]
        public ActionResult EditAmount(int shoppingListID, int itemID, string amount, string unit)
        {
            var tmpItemRel = _db.ShoppingList_Item.FirstOrDefault(r => r.ShoppingListID == shoppingListID && r.ItemID == itemID);

            double parsedAmount;
            bool parseRes = double.TryParse(amount.Replace(".", ","), out parsedAmount);
            tmpItemRel.Amount = parseRes ? parsedAmount : 0;
            if(unit.Trim() != string.Empty) {
                tmpItemRel.Unit = unit.Trim();
            }

            _db.SaveChanges();

            return Json(new {
                Message = "Hajtroels",
                ItemId = itemID,
            }, JsonRequestBehavior.AllowGet);
        }

        //Find relevant shoppingList and include the items
        private ShoppingList findShoppingListFromID(int? id)
        {
            return _db.ShoppingLists.Include(s => s.Items.Select(x => x.Offers)).ToList().Find(x => x.ID == id);
        } 

        public ActionResult ChooseOffer(int shoppingListId, int ItemId, int offerId)
        {

            ShoppingList list = findShoppingListFromID(shoppingListId);

            _db.ShoppingList_Item.First(x => x.ItemID == ItemId && x.ShoppingListID == shoppingListId).selectedOffer = _db.Offers.First(x => x.ID == offerId);
            _db.SaveChanges();

            return RedirectToAction("Details/" + shoppingListId);

        }

        private void addToShoppingList_Item(Item item, ShoppingList shoppingList, double? amount, string unit)
        {
            shoppingList.Items.Add(item);

            var shoppingListItem = new ShoppingList_Item { Item = item, ShoppingList = shoppingList, Amount = (double)amount, Unit = unit };
            _db.ShoppingList_Item.Add(shoppingListItem);

            _db.SaveChanges();
        }

        private bool isOfferSelectedOnItem(int id, Item item){
            return (_db.ShoppingList_Item.First(x => x.ItemID == item.ID && x.ShoppingListID == id).selectedOffer != null);
        }
        
    }
}

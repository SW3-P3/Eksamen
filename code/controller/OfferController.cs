public ActionResult Index(int? shoppingListID)
{
	if (User.Identity.IsAuthenticated)
	{
		var tmpUser = _db.Users
			.Include(s => s.ShoppingLists)
			.First(u => u.Username == User.Identity.Name);
		if (tmpUser.ShoppingLists.Count > 0)
		{

			int tmpShoppingListID = shoppingListID == null ? tmpUser.ShoppingLists.First().ID : (int)shoppingListID;
			ViewBag.SelectedShoppingListID = tmpShoppingListID;

			ViewBag.ShoppingLists = tmpUser.ShoppingLists.ToList();
			Stopwatch stopwatch = Stopwatch.StartNew();
			ViewBag.OffersOnListByID = _db.ShoppingList_Item
				.Where(s => s.ShoppingListID == tmpShoppingListID && s.selectedOffer != null)
				.Select<ShoppingList_Item, int>(o => o.selectedOffer.ID).ToArray();
			stopwatch.Stop();
			Debug.WriteLine("OffersOnListByID " + stopwatch.ElapsedMilliseconds);
		}

		ViewBag.Stores = _db.OffersFilteredByUserPrefs(_db.Users.First(u => u.Username == User.Identity.Name))
			.OrderBy(d => d.Store).Select(x => x.Store).Distinct();
		ViewBag.ShoppingLists = _db.Users.Include(s => s.ShoppingLists)
			.First(u => u.Username == User.Identity.Name).ShoppingLists.ToList();

		return View(_db.OffersFilteredByUserPrefs(tmpUser).ToList());
	}
	return RedirectToAction("Login", "Account", new { returnUrl = Url.Action() });
}

public ActionResult ImportOffers()
{
	string apikey, secret;
	/* Write Apikey and Secret to global variables */
	try
	{
		apikey = System.IO.File.ReadAllText(@"C:\apikey.secret");
		secret = System.IO.File.ReadAllText(@"C:\secret.secret");
	}
	catch (IOException ex)
	{
		Console.WriteLine("File not found. " + ex);
		throw;
	}

	/* Create a RestClient*/
	var client = new RestClient("https://api.etilbudsavis.dk");

	/* Initiate the first request to get a session */
	var sessionRequest = new RestRequest("v2/sessions", Method.POST);
	sessionRequest.AddParameter("api_key", apikey);

	/* Map the response to the class "Session" */
	IRestResponse<Session> response2 = client.Execute<Session>(sessionRequest);

	/* Save the response in an object */
	var sessionobj = response2.Data;

	/* Load aditional infomation into the object */
	sessionobj.Secret = secret;
	sessionobj.Apikey = apikey;
	sessionobj.Signature = Models.Api.Session.Sha256(sessionobj.Secret + sessionobj.Token);

	/* Copy the sessionobj to a globalSession */
	Global.Session = sessionobj;

	/* Offers request (Every offer from the catalog found above */
	var offersRequest = new RestRequest("v2/offers", Method.GET);
	offersRequest.AddParameter("r_lat", Global.Latitude);
	offersRequest.AddParameter("r_lng", Global.Longitude);
	offersRequest.AddParameter("r_radius", Global.Radius);
	offersRequest.AddHeader("X-Token", Global.Session.Token);
	offersRequest.AddHeader("X-Signature", Global.Session.Signature);
	//offersRequest.AddParameter("catalog_ids", string.Join(",", catalogsResult.Select(x => x.id)));
	offersRequest.AddParameter("limit", "100");
	offersRequest.AddParameter("dealer_ids", "9ba51,8c4da,bdf5A,11deC,c1edq,71c90,101cD,0b1e8,ecddz,8c4da,1e1eB"); //8c4da,bdf5A,11deC,c1edq,71c90,101cD,0b1e8,ecddz,8c4da,1e1eB,d311fg

	//TODO: This should be converted to an Async method for each store if we need a speed-up
	List<ApiOffer> offersResult = client.Execute<List<ApiOffer>>(offersRequest).Data;
	List<ApiOffer> listofApiOffers = offersResult;
	while (offersResult.Count == 100)
	{
		var nextOffersRequest = new RestRequest("v2/offers", Method.GET);
		nextOffersRequest.AddParameter("r_lat", Global.Latitude);
		nextOffersRequest.AddParameter("r_lng", Global.Longitude);
		nextOffersRequest.AddParameter("r_radius", Global.Radius);
		nextOffersRequest.AddHeader("X-Token", Global.Session.Token);
		nextOffersRequest.AddHeader("X-Signature", Global.Session.Signature);
		nextOffersRequest.AddParameter("limit", "100");
		nextOffersRequest.AddParameter("dealer_ids", "9ba51,8c4da,bdf5A,11deC,c1edq,71c90,101cD,0b1e8,ecddz,8c4da,1e1eB");
		nextOffersRequest.AddParameter("offset", listofApiOffers.Count);
		offersResult = client.Execute<List<ApiOffer>>(nextOffersRequest).Data;
		listofApiOffers.AddRange(offersResult);
	}

	foreach (var o in listofApiOffers)
	{
		var newOffer = new Offer
		{
			eTilbudsavisID = o.id,
			Heading = o.heading,
			Begin = o.run_from,
			End = o.run_till,
			Store = o.branding.name,
			Price = o.pricing.price,
			Unit = o.quantity.unit != null ? o.quantity.size.@from + " " + o.quantity.unit.symbol : " "
		};

		// If the offer doesn't already exist in the database add it.
		if (!_db.Offers.Any(x => x.eTilbudsavisID == newOffer.eTilbudsavisID))
		{
			_db.Offers.Add(newOffer);
		}
	}

	_db.SaveChanges();
	NotifyWatchers();

	return RedirectToAction("Index");
}

[HttpPost]
public ActionResult AddOfferToShoppingList(int offerId, int? shoppingListId)
{
	var tmpOffer = _db.Offers.Find(offerId);

	var tmpItem = new Item { Name = tmpOffer.Heading };
	tmpItem.Offers.Add(tmpOffer);

	var shoppingList = _db.ShoppingLists.First(l => l.ID == shoppingListId);

	double num;
	bool res = double.TryParse(tmpOffer.Unit.Split(' ').First(), out num);


	var shoppingListItem = new ShoppingList_Item
	{
		Item = tmpItem,
		ShoppingList = shoppingList,
		Amount = res ? num : 0,
		Unit = tmpOffer.Unit.Split(' ').Last(),
		selectedOffer = tmpOffer
	};

	_db.ShoppingList_Item.Add(shoppingListItem);

	shoppingList.Items.Add(tmpItem);

	_db.SaveChanges();

	return Json(new
	{
		Message = "Hajtroels",
		OfferId = offerId,
	}, JsonRequestBehavior.AllowGet);
}

internal List<Offer> GetOffersForItem(string str)
{
	return _db.OffersFilteredByUserPrefs(_db.Users.FirstOrDefault(x => x.Username.Equals(User.Identity.Name)))
		.Where(x => x.Heading.ToLower().Contains(str.ToLower() + " ") || x.Heading.ToLower().Contains(" " + str.ToLower()) ||
					String.Equals(x.Heading, str, StringComparison.CurrentCultureIgnoreCase))
		.ToList();
}


public void NotifyWatchers()
{
	// Get all the users who have a watchlist with items.
	var users = _db.Users.Include(u => u.RelevantOffers.Items).Include(w => w.WatchList.Items).Where(u => u.WatchList != null && u.WatchList.Items.Count > 0);

	foreach (var user in users.Include(x => x.SentOffers))
	{
		var dt = (DateTime)user.LastSentNotification;
		// Only sent notification if non have been sent for 1 day unless a preference is set.
		if (dt.AddDays(user.MaxSendEmailsEveryDays ?? 1) < DateTime.Now)
		{
			var relevantOffers = new List<Offer>();
			// Add every offer that maybe should be sent. 
			foreach (var item in user.WatchList.Items)
			{
				relevantOffers.AddRange(GetOffersForItem(item.Name));
			}

			var output = new List<Offer>();

			// Add offers not yet sent to the user to a list of item to send and to the list of offers sent.
			foreach (var offer in relevantOffers.Where(offer => !user.SentOffers.Contains(offer)))
			{
				output.Add(offer);
				user.SentOffers.Add(offer);
				offer.SentToUsers.Add(user);
			}

			// If there are new offers send them to the user.
			if (output.Count > 0)
			{
				SendEmailToUser(output, user);
				user.LastSentNotification = DateTime.Now;

			}
		}
	}
	_db.SaveChanges();
}

private static void SendEmailToUser(IEnumerable<Offer> offers, User user)
{
	try
	{
		/* Make a SMTP Client to send emails.*/
		var smtp = new SmtpClient
		{
			Port = 587,
			Host = "smtp.gmail.com",
			EnableSsl = true,
			UseDefaultCredentials = false,
			Credentials = new NetworkCredential("ProjectFoodHype@gmail.com", "PFHype!123"),
			DeliveryMethod = SmtpDeliveryMethod.Network
		};

		/* construct the message*/
		var message = new MailMessage();
		message.From = new MailAddress("ProjectFoodHype@gmail.com");
		message.To.Add(new MailAddress("ProjectFoodHype@gmail.com"));

		//message.To.Add(new MailAddress(user.Username));

		message.Subject = string.Format("Hej {0} \n ProjectFood har {1} tilbud på dine overvågede varer!",user.Name, offers.Count());

		var body = string.Empty;

		body += string.Format("Følgende tilbud er fundet til dig ");

		body = offers.Aggregate(body, (current, relevantOffer) => current + string.Format("\"{0}\" til {1} kr. i {2}\n", relevantOffer.Heading, relevantOffer.Price, relevantOffer.Store));

		body += "Se mere på: %foobar.com%";

		message.Body = body;

		smtp.Send(message);
	}
	catch (Exception ex)
	{
		Debug.WriteLine("err: " + ex.Message);

	}
}

internal List<Offer> GetOffersForItem(Item item)
{
	return _db.OffersFilteredByUserPrefs(_db.Users.FirstOrDefault(x => x.Username == User.Identity.Name))
		.Where(x => x.Heading.ToLower().Contains(item.Name.ToLower() + " ") 
			|| x.Heading.ToLower().Contains(" " + item.Name.ToLower())
			|| String.Equals(x.Heading, item.Name, StringComparison.CurrentCultureIgnoreCase))
		.ToList();
}

[HttpPost]
public ActionResult GetOffers(int page, string storename)
{
	int PerPage = 25;
	IQueryable<Offer> offers;
	if (storename == "all")
	{
		offers = _db.OffersFilteredByUserPrefs(_db.Users.FirstOrDefault(x => x.Username == User.Identity.Name)).AsQueryable()
			.OrderBy(o => o.Heading)
			.Skip((page - 1) * PerPage)
			.Take(PerPage);
	}
	else
	{
		offers = _db.OffersFilteredByUserPrefs(_db.Users.FirstOrDefault(x => x.Username == User.Identity.Name)).AsQueryable()
			.Where(o => o.Store.Replace("ø", string.Empty).Replace(" ", string.Empty) == storename).OrderBy(o => o.Heading)
			.Skip((page - 1) * PerPage)
			.Take(PerPage);
	}



	var jsonSerialiser = new JavaScriptSerializer();
	var jsonOffers = jsonSerialiser.Serialize(offers);

	return Json(new { page = page, store = storename.Replace(" ", string.Empty), jsonOffers }, JsonRequestBehavior.DenyGet);
}
}
}
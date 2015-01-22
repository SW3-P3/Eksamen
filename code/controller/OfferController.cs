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

	/* Load additional information into the object */
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

	foreach( var a in _db.Offers)
	{
		_db.Offers.Remove(a);
	}

	foreach (var o in listofApiOffers)
	{
		_db.Offers.Add(new Offer
		{
			Heading = o.heading,
			Begin = o.run_from,
			End = o.run_till,
			Store = o.branding.name,
			Price = o.pricing.price,
			Unit = o.quantity.unit != null ? o.quantity.size.@from + " " + o.quantity.unit.symbol : " "
		});
	}

	_db.SaveChanges();

	return RedirectToAction("Index");
}

[HttpPost]
public ActionResult AddOfferToShoppingList(int offerId, int? shoppingListId)
{
	var tmpOffer = _db.Offers.Find(offerId);

	var tmpItem = new Item();
	tmpItem.Name = tmpOffer.Heading;
	tmpItem.Offers.Add(tmpOffer);
	
	if (shoppingListId == null)
		shoppingListId = _db.ShoppingLists.First().ID;

	var shoppingList = _db.ShoppingLists.First(l => l.ID == shoppingListId);

	shoppingList.Items.Add(tmpItem);

	_db.SaveChanges();

	return Json(new
	{
		OfferId = offerId,
	}, JsonRequestBehavior.AllowGet);
}
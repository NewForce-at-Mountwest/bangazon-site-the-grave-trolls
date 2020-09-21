using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Bangazon.Data;
using Bangazon.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Bangazon.Models.OrderViewModels;

namespace Bangazon.Controllers
{
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;

        private readonly UserManager<ApplicationUser> _userManager;

        public OrdersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private Task<ApplicationUser> GetCurrentUserAsync() => _userManager.GetUserAsync(HttpContext.User);

        // GET: Orders
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Order.Include(o => o.PaymentType).Include(o => o.User);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Orders/Details/5
        //Authorize is a handy little tag that only allows the following method to be run if an authorized user is logged in. Otherwise, they're redirected to the login!
        [Authorize]
        public async Task<IActionResult> Details(int? id)
        {
            //Gets the current user
            ApplicationUser currentUser = await GetCurrentUserAsync();

            //Creates an empty order that will be filled in just a second
            Order order;

            //If we call the details view without passing in an id (which we'll do if we simply click the "View Cart" link), then we want to show the currently open order, or our Shopping Cart!
            if (id == null)
            {
                //This grabs the first order with the user's ID that also has no payment type (aka it's still open)
                order = await _context.Order
                    .Include(o => o.User)
                    .Include(o => o.OrderProducts)
                        .ThenInclude(op => op.Product)
                    .Where(o => o.UserId == currentUser.Id)
                    .Where(o => o.PaymentTypeId == null)
                    .FirstOrDefaultAsync();
            }
            //If this method is passed an id, it'll just grab the order with that id instead
            else
            {
                order = await _context.Order
               .Include(o => o.PaymentType)
               .Include(o => o.User)
               .Include(o => o.OrderProducts)
                        .ThenInclude(op => op.Product)
               .Where(o => o.UserId == currentUser.Id)
               .FirstOrDefaultAsync(m => m.OrderId == id);
            }

            //If there is no open order in the database, we'll create one
            if (order == null && id == null)
            {
                //Creates a new order...
                order = new Order
                {
                    DateCreated = DateTime.Now,
                    UserId = currentUser.Id,
                    OrderProducts = new List<OrderProduct>(),
                    User = currentUser
                };
                //...then adds it to the database! This object's id is also now its database id
                _context.Add(order);
                await _context.SaveChangesAsync();
            }
            //If we passed an id into the method and still found no order, this will give us a NotFound page
            else if (order == null && id != null)
            {
                return NotFound();
            }

            //Finally, if we make it here with an order (whether fetched or newly created), we'll show the view with that order
            return View(order);
        }

        // GET: Orders/Create
        public IActionResult Create()
        {
            ViewData["PaymentTypeId"] = new SelectList(_context.PaymentType, "PaymentTypeId", "AccountNumber");
            ViewData["UserId"] = new SelectList(_context.ApplicationUsers, "Id", "Id");
            return View();
        }

        // POST: Orders/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("OrderId,DateCreated,DateCompleted,UserId,PaymentTypeId")] Order order)
        {
            if (ModelState.IsValid)
            {
                _context.Add(order);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["PaymentTypeId"] = new SelectList(_context.PaymentType, "PaymentTypeId", "AccountNumber", order.PaymentTypeId);
            ViewData["UserId"] = new SelectList(_context.ApplicationUsers, "Id", "Id", order.UserId);
            return View(order);
        }

        // GET: Orders/Edit/5
        //This Edit page is actually our Finalize Checkout page, where the user adds a payment type!
        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            //If no id is passed in, returns a Not Found page
            if (id == null)
            {
                return NotFound();
            }

            //Gets the current user
            ApplicationUser currentUser = await GetCurrentUserAsync();

            //Finds the current open order, using the id that was passed in
            var order = await _context.Order
                                .Include(o => o.PaymentType)
                                .Include(o => o.User)
                                .Include(o => o.OrderProducts)
                                    .ThenInclude(op => op.Product)
                                .Where(o => o.UserId == currentUser.Id)
                                .Where(o => o.PaymentTypeId == null)
                                .FirstOrDefaultAsync(m => m.OrderId == id);

            //If the order doesn't exist, returns a Not Found page
            if (order == null)
            {
                return NotFound();
            }

            //Creates a ViewModel to hold our order and payment types
            OrderEditViewModel model = new OrderEditViewModel();
            model.Order = order;

            //fetches all active payment types attributed to the user
            List<PaymentType> paymentTypes = await _context.PaymentType
                                                .Where(pt => pt.UserId == currentUser.Id && pt.Active == true)
                                                .ToListAsync();

            //If payment types exist, convert them to select list items and add them to the view model
            if (paymentTypes != null)
            {
                model.PaymentTypes = new SelectList(paymentTypes, "PaymentTypeId", "Description", order.PaymentTypeId).ToList();
            }

            //Pulls up our view for this order. If you look at the cshtml for this view, you'll note that, if no payment types were found, the user is given a link to add a new payment type!
            return View(model);
        }

        // POST: Orders/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        //This method updates/edits our order with the new PaymentType Id
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, OrderEditViewModel model)
        {
            //If the passed-in id and the order id don't match, returns a not found page
            if (id != model.Order.OrderId)
            {
                return NotFound();
            }

            //We're removing all of this from ModelState so that the ModelState is valid. None of this data is included in our ViewModel parameter
            ModelState.Remove("Order.DateCompleted");
            ModelState.Remove("Order.OrderProducts");
            ModelState.Remove("Order.PaymentType");
            ModelState.Remove("Order.User");

            //Creates a new View Model for our next view. It will hold the order, select list items of payment types, and a list of out of stock products that were removed from the order
            OrderEditViewModel newModel = new OrderEditViewModel();

            //Fetches a list of all products in the order that we can loop through
            List<OrderProduct> productsInOrder = await _context.OrderProduct
                                                            .Include(op => op.Product)
                                                            .Where(op => op.OrderId == model.Order.OrderId)
                                                            .ToListAsync();
            //Fetches a second list that the first list will be compared to. This one can be edited as we loop through the first one
            List<OrderProduct> productsAfterRemoval = await _context.OrderProduct
                                                            .Include(op => op.Product)
                                                            .Where(op => op.OrderId == model.Order.OrderId)
                                                            .ToListAsync();

            //Gets the current user
            ApplicationUser currentUser = await GetCurrentUserAsync();

            //Loops through orders that START in our order. We're looking for duplicate items, and going to count them. This count will be subtracted from the total quantity available. If this puts the quanitity below 0, we're going to remove the product from our order.
            foreach (OrderProduct productInOrder in productsInOrder)
            {
                int count = 0;
                //Loops through our second, mirrored list to find products identical to the current product
                foreach (OrderProduct otherProduct in productsAfterRemoval)
                {
                    if (otherProduct.ProductId == productInOrder.ProductId)
                    {
                        //iterate our count if the product ids match
                        count++;
                    }
                }

                //Grabs the current number of products that are on closed orders
                List<OrderProduct> productsGone = await _context.OrderProduct
                                    .Include(op => op.Order)
                                    .Where(op => op.ProductId == productInOrder.ProductId)
                                    .Where(op => op.Order.PaymentTypeId != null)
                                    .ToListAsync();

                //Subtracts the number of product gone from the total quantity
                int currentQuantity = productInOrder.Product.Quantity - productsGone.Count();

                //If the number of products the user is buying will drop the current quantity below 0....
                if (currentQuantity - count < 0)
                {
                    //...add it to a list of removed products in our view model...
                    model.outOfStockProducts.Add(productInOrder);
                    //...remove the product from the order in the database...
                    _context.OrderProduct.Remove(productInOrder);
                    await _context.SaveChangesAsync();
                    //...and remove it from our second list, so we have a proper, up-to-date look of what our final cart will look like.
                    productsAfterRemoval.Remove(productInOrder);
                }
            }

            //By the time the above code runs, any items that would push the stock below 0 will be removed, and kept track of in the outOfStockProducts list on our view model. Our second list from before, productsAfterRemoval, is now an accurate view of what our cart looks like after those products are removed. productsInCart is an old, obsolete view -- it's what our cart looks like with those removed items still there. We couldn't remove them from the list WHILE we were iterating over it. C# doesn't like that.

            //Runs if our ModelState is valid, which it should be since we removed properties from it
            if (ModelState.IsValid)
            {
                try
                {
                    //Only runs if our finalized cart actually has items in it!
                    if (productsAfterRemoval.Count() > 0)
                    {
                        //If we still have items in our cart after removal, we're going to checkout. First, we set the date of completion to today
                        model.Order.DateCompleted = DateTime.Now;

                        //then we update the order in the database, which really just gives it a PaymentTypeId, making it a closed order
                        _context.Update(model.Order);
                        await _context.SaveChangesAsync();
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderExists(model.Order.OrderId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                //All of the upcoming code will refresh our ViewModel with the finalized order, post-removal

                //First we get the order again, with all its products...
                Order order = await _context.Order
                                .Include(o => o.PaymentType)
                                .Include(o => o.User)
                                .Include(o => o.OrderProducts)
                                    .ThenInclude(op => op.Product)
                                .Where(o => o.UserId == currentUser.Id)
                                .FirstOrDefaultAsync(m => m.OrderId == id);

                newModel.Order = order;

                //...Then we get all the PaymentTypes again, just to fill out the PaymentType...
                List<PaymentType> paymentTypes = await _context.PaymentType
                                                    .Where(pt => pt.UserId == currentUser.Id && pt.Active == true)
                                                    .ToListAsync();

                //...and turn them into SelectListItems
                if (paymentTypes != null)
                {
                    newModel.PaymentTypes = new SelectList(paymentTypes, "PaymentTypeId", "Description", newModel.Order.PaymentTypeId).ToList();
                }

                //Sends us to our CheckedOut view
                return View("CheckedOut", newModel);
            }

            //This all refreshes the ViewModel so we can refresh the page if checking out FAILS

            Order failedOrder = await _context.Order
                                .Include(o => o.PaymentType)
                                .Include(o => o.User)
                                .Include(o => o.OrderProducts)
                                    .ThenInclude(op => op.Product)
                                .Where(o => o.UserId == currentUser.Id)
                                .Where(o => o.PaymentTypeId == null)
                                .FirstOrDefaultAsync(m => m.OrderId == id);

            newModel.Order = failedOrder;

            List<PaymentType> failedPaymentTypes = await _context.PaymentType
                                                .Where(pt => pt.UserId == currentUser.Id && pt.Active == true)
                                                .ToListAsync();

            if (failedPaymentTypes != null)
            {
                newModel.PaymentTypes = new SelectList(failedPaymentTypes, "PaymentTypeId", "Description", newModel.Order.PaymentTypeId).ToList();
            }

            //Refreshes the page with the same ViewModel if checking out fails
            return View(newModel);
        }

        // GET: Orders/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Order
                .Include(o => o.PaymentType)
                .Include(o => o.User)
                .FirstOrDefaultAsync(m => m.OrderId == id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // POST: Orders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order = await _context.Order.FindAsync(id);
            _context.Order.Remove(order);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool OrderExists(int id)
        {
            return _context.Order.Any(e => e.OrderId == id);
        }
    }
}

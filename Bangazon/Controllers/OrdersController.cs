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
                    .FirstAsync();
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
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            //Gets the current user
            ApplicationUser currentUser = await GetCurrentUserAsync();

            var order = await _context.Order
                                .Include(o => o.PaymentType)
                                .Include(o => o.User)
                                .Include(o => o.OrderProducts)
                                    .ThenInclude(op => op.Product)
                                .Where(o => o.UserId == currentUser.Id)
                                .FirstOrDefaultAsync(m => m.OrderId == id);
            if (order == null)
            {
                return NotFound();
            }

            OrderEditViewModel model = new OrderEditViewModel();
            model.Order = order;

            List<PaymentType> paymentTypes = await _context.PaymentType
                                                .Where(pt => pt.UserId == currentUser.Id && pt.Active == true)
                                                .ToListAsync();

            model.PaymentTypes = new SelectList(paymentTypes, "Id", "Description", order.PaymentTypeId).ToList();

            return View(model);
        }

        // POST: Orders/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("OrderId,DateCreated,DateCompleted,UserId,PaymentTypeId")] Order order)
        {
            if (id != order.OrderId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(order);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderExists(order.OrderId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["PaymentTypeId"] = new SelectList(_context.PaymentType, "PaymentTypeId", "AccountNumber", order.PaymentTypeId);
            ViewData["UserId"] = new SelectList(_context.ApplicationUsers, "Id", "Id", order.UserId);
            return View(order);
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

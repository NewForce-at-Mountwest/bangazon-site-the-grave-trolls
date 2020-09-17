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

namespace Bangazon.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        private readonly UserManager<ApplicationUser> _userManager;

        public ProductsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private Task<ApplicationUser> GetCurrentUserAsync() => _userManager.GetUserAsync(HttpContext.User);

        // GET: Products
        public async Task<IActionResult> Index(string searchString)
        {
            // the constructor above is going to contain the what the user types in

            //sets the searchstring paramater to viewdata so that it be accessed in views
            ViewData["CurrentFilter"] = searchString;

            var applicationDbContext = _context.Product.Include(p => p.ProductType).Include(p => p.User).AsQueryable();

            //if the search string isn't empty
            if (!String.IsNullOrEmpty(searchString))
            {
                // return the results that contain what the user typed in
                applicationDbContext = applicationDbContext.Where(p => p.Title.Contains(searchString));

            }
            if(applicationDbContext.Count()< 1 )
            {
                
                return View("SearchError");
            }
          

            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            //Fetches the desired product, its product type, and its user
            var product = await _context.Product
                .Include(p => p.ProductType)
                .Include(p => p.User)
                .FirstOrDefaultAsync(m => m.ProductId == id);

            //Finds how many completed orders use contain this product...
            var productsOrdered = await _context.OrderProduct
                .Include(op => op.Order)
                .Where(op => op.ProductId == id)
                .Where(op => op.Order.PaymentTypeId != null)
                .ToListAsync();

            //...and dynamically subtracts that count from the Quantity property of the product resource. Does NOT affect the Quantity in the database!
            product.Quantity = product.Quantity - productsOrdered.Count;

            if (product == null)
            {
                return NotFound();
            }

            //Returns the Details view for the desired product
            return View(product);
        }

        //A brand new function for adding a product to the currently open order
        public async Task<IActionResult> AddToCart(int id)
        {
            //gets the user that's currently logged in
            ApplicationUser currentUser = await GetCurrentUserAsync();

            //will only add a product to the order IF the user is logged int
            if (currentUser != null)
            {
                if (ModelState.IsValid)
                {
                    //Finds the first order that matches the user's Id AND has no payment type (this signifies the order is open, and there should only be one)
                    Order orderToAddTo = await _context.Order
                        .Where(o => o.UserId == currentUser.Id)
                        .Where(o => o.PaymentTypeId == null)
                        .FirstOrDefaultAsync();

                    //If there is no current open order, we'll create one!
                    if (orderToAddTo == null)
                    {
                        //Creates a new order...
                        orderToAddTo = new Order
                        {
                            DateCreated = DateTime.Now,
                            UserId = currentUser.Id,
                            User = currentUser
                        };
                        //...then adds it to the database! This object's id is also now its database id
                        _context.Add(orderToAddTo);
                        await _context.SaveChangesAsync();
                    }

                    //Adds a new OrderProduct resource to link the desired product to the open order
                    OrderProduct newProductOnOrder = new OrderProduct
                    {
                        ProductId = id,
                        OrderId = orderToAddTo.OrderId
                    };

                    //Adds the product to the order and returns us to the Index view
                    _context.Add(newProductOnOrder);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }

            //If the user is not logged in, or the model state is invalid, go to the index page
            return RedirectToAction(nameof(Index));
        }

        // GET: Products/Create
        public IActionResult Create()
        {
            ViewData["ProductTypeId"] = new SelectList(_context.ProductType, "ProductTypeId", "Label");
            ViewData["UserId"] = new SelectList(_context.ApplicationUsers, "Id", "Id");
            return View();
        }

        // POST: Products/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProductId,DateCreated,Description,Title,Price,Quantity,UserId,City,ImagePath,Active,ProductTypeId")] Product product)
        {
            if (ModelState.IsValid)
            {
                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ProductTypeId"] = new SelectList(_context.ProductType, "ProductTypeId", "Label", product.ProductTypeId);
            ViewData["UserId"] = new SelectList(_context.ApplicationUsers, "Id", "Id", product.UserId);
            return View(product);
        }

        // GET: Products/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Product.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            ViewData["ProductTypeId"] = new SelectList(_context.ProductType, "ProductTypeId", "Label", product.ProductTypeId);
            ViewData["UserId"] = new SelectList(_context.ApplicationUsers, "Id", "Id", product.UserId);
            return View(product);
        }

        // POST: Products/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProductId,DateCreated,Description,Title,Price,Quantity,UserId,City,ImagePath,Active,ProductTypeId")] Product product)
        {
            if (id != product.ProductId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(product);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.ProductId))
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
            ViewData["ProductTypeId"] = new SelectList(_context.ProductType, "ProductTypeId", "Label", product.ProductTypeId);
            ViewData["UserId"] = new SelectList(_context.ApplicationUsers, "Id", "Id", product.UserId);
            return View(product);
        }

        // GET: Products/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Product
                .Include(p => p.ProductType)
                .Include(p => p.User)
                .FirstOrDefaultAsync(m => m.ProductId == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Product.FindAsync(id);
            _context.Product.Remove(product);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
            return _context.Product.Any(e => e.ProductId == id);
        }
    }
}

﻿using System;
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

namespace Bangazon.Controllers
{
    public class PaymentTypesController : Controller
    {
        private readonly ApplicationDbContext _context;

        private readonly UserManager<ApplicationUser> _userManager;

        private Task<ApplicationUser> GetCurrentUserAsync() => _userManager.GetUserAsync(HttpContext.User);

       


        public PaymentTypesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
            _context = context;
        }

        // GET: PaymentTypes
        [Authorize]
        //constructor type of paymenttype
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.PaymentType.Include(p => p.User);

            var user = await GetCurrentUserAsync();

            //gets all payment types associated with the user and has and active status of true
            //returns that information to a list
            var userCheck = await _context.PaymentType.Where(p => p.UserId == user.Id && p.Active == true).ToListAsync();

            // if there are 0 payment types assocated with the user
            // or if their or no active payment types
            if (userCheck.Count() < 1 )
            {
                //redirect to a page that tells them 
                //and gives them an option to create a payment type
                return View("NoPaymentTypes");
            }

            return View(userCheck);
                //returns a view that hass all payment types associated with the user
               
        }

        // GET: PaymentTypes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var paymentType = await _context.PaymentType
                .Include(p => p.User)
                .FirstOrDefaultAsync(m => m.PaymentTypeId == id);
            if (paymentType == null)
            {
                return NotFound();
            }

            return View(paymentType);
        }

        // GET: PaymentTypes/Create
        [Authorize]
        public IActionResult Create()
        {
            ViewData["UserId"] = new SelectList(_context.ApplicationUsers, "Id", "Id");
            return View();
        }

        // POST: PaymentTypes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PaymentTypeId,DateCreated,Description,AccountNumber,UserId,Active")] PaymentType paymentType)
        {

            //removes the user and userid so the model state doesnt think that the information is invalid
            ModelState.Remove("User");
            ModelState.Remove("UserId");
            //checks to for all the information in the model state
            if (ModelState.IsValid)
            {
                //gets the current user
                var user = await GetCurrentUserAsync();
                //grabs the current user's id
                paymentType.UserId = user.Id;
                //sets the active status to true anytime a new payment type is created
                paymentType.Active = true;
                //adds all the user and paymentType information into context
                _context.Add(paymentType);
                //saves the changes
                await _context.SaveChangesAsync();
                //redirects back to all the payment types
                return RedirectToAction(nameof(Index));
            }
            ViewData["UserId"] = new SelectList(_context.ApplicationUsers, "Id", "Id", paymentType.UserId);
            return View(paymentType);
        }

        // GET: PaymentTypes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var paymentType = await _context.PaymentType.FindAsync(id);
            if (paymentType == null)
            {
                return NotFound();
            }
            ViewData["UserId"] = new SelectList(_context.ApplicationUsers, "Id", "Id", paymentType.UserId);
            return View(paymentType);
        }

        // POST: PaymentTypes/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PaymentTypeId,DateCreated,Description,AccountNumber,UserId,Active")] PaymentType paymentType)
        {
            if (id != paymentType.PaymentTypeId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(paymentType);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PaymentTypeExists(paymentType.PaymentTypeId))
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
            ViewData["UserId"] = new SelectList(_context.ApplicationUsers, "Id", "Id", paymentType.UserId);
            return View(paymentType);
        }

        // GET: PaymentTypes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var paymentType = await _context.PaymentType
                .Include(p => p.User)
                .FirstOrDefaultAsync(m => m.PaymentTypeId == id);
            if (paymentType == null)
            {
                return NotFound();
            }

            return View(paymentType);
        }

        // POST: PaymentTypes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {   
            //gets the payment type by id
            PaymentType paymentType = await _context.PaymentType.FindAsync(id);

            
            try
            {
               //it first tries to remove the payment type from the data base
                _context.PaymentType.Remove(paymentType);
                //then save the changes
                await _context.SaveChangesAsync();
            }
            // if the payment type is associated with an order
            // this catches the exception when it's active
            catch (Exception) when (paymentType.Active == true)
            {
                //changes the active status to false
                paymentType.Active = false;
                //updates the payment type since it cant be deleted
                _context.Update(paymentType);
                //saves the changes
                await _context.SaveChangesAsync();
            }

            // redirects back to the view of all payment types
            return RedirectToAction(nameof(Index));
        }

        private bool PaymentTypeExists(int id)
        {
            return _context.PaymentType.Any(e => e.PaymentTypeId == id);
        }
    }
}

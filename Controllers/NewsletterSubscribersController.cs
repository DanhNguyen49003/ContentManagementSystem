using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ContentManagementSystem.ApplicationCore.Entities;
using ContentManagementSystem.DataLayer;

namespace ContentManagementSystem.Controllers
{
    public class NewsletterSubscribersController : Controller
    {
        private readonly ContentManageDbContext _context;

        public NewsletterSubscribersController(ContentManageDbContext context)
        {
            _context = context;
        }

        // GET: NewsletterSubscribers
        public async Task<IActionResult> Index()
        {
            return View(await _context.NewsletterSubscribers.ToListAsync());
        }

        // GET: NewsletterSubscribers/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var newsletterSubscriber = await _context.NewsletterSubscribers
                .FirstOrDefaultAsync(m => m.Id == id);
            if (newsletterSubscriber == null)
            {
                return NotFound();
            }

            return View(newsletterSubscriber);
        }

        // GET: NewsletterSubscribers/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: NewsletterSubscribers/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Email,IsActive,CreatedAt")] NewsletterSubscriber newsletterSubscriber)
        {
            if (ModelState.IsValid)
            {
                newsletterSubscriber.Id = Guid.NewGuid();
                _context.NewsletterSubscribers.Add(newsletterSubscriber);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(newsletterSubscriber);
        }

        // GET: NewsletterSubscribers/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var newsletterSubscriber = await _context.NewsletterSubscribers.FindAsync(id);
            if (newsletterSubscriber == null)
            {
                return NotFound();
            }
            return View(newsletterSubscriber);
        }

        // POST: NewsletterSubscribers/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Email,IsActive,CreatedAt")] NewsletterSubscriber newsletterSubscriber)
        {
            if (id != newsletterSubscriber.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.NewsletterSubscribers.Update(newsletterSubscriber);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!NewsletterSubscriberExists(newsletterSubscriber.Id))
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
            return View(newsletterSubscriber);
        }

        // GET: NewsletterSubscribers/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var newsletterSubscriber = await _context.NewsletterSubscribers
                .FirstOrDefaultAsync(m => m.Id == id);
            if (newsletterSubscriber == null)
            {
                return NotFound();
            }

            return View(newsletterSubscriber);
        }

        // POST: NewsletterSubscribers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var newsletterSubscriber = await _context.NewsletterSubscribers.FindAsync(id);
            if (newsletterSubscriber != null)
            {
                _context.NewsletterSubscribers.Remove(newsletterSubscriber);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool NewsletterSubscriberExists(Guid id)
        {
            return _context.NewsletterSubscribers.Any(e => e.Id == id);
        }
    }
}





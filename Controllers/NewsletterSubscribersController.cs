using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ContentManagementSystem.ApplicationCore.Entities;
using ContentManagementSystem.ApplicationCore.Interfaces;
using ContentManagementSystem.DataLayer;

namespace ContentManagementSystem.Controllers
{
    public class NewsletterSubscribersController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public NewsletterSubscribersController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // GET: NewsletterSubscribers
        public async Task<IActionResult> Index()
        {
            return View(await _unitOfWork.NewsletterSubscribers.AsQueryable().ToListAsync());
        }

        // GET: NewsletterSubscribers/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var newsletterSubscriber = await _unitOfWork.NewsletterSubscribers.AsQueryable()
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
                _unitOfWork.NewsletterSubscribers.Add(newsletterSubscriber);
                await _unitOfWork.CompleteAsync();
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

            var newsletterSubscriber = await _unitOfWork.NewsletterSubscribers.GetByIdAsync(id);
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
                    _unitOfWork.NewsletterSubscribers.Update(newsletterSubscriber);
                    await _unitOfWork.CompleteAsync();
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

            var newsletterSubscriber = await _unitOfWork.NewsletterSubscribers.AsQueryable()
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
            var newsletterSubscriber = await _unitOfWork.NewsletterSubscribers.GetByIdAsync(id);
            if (newsletterSubscriber != null)
            {
                _unitOfWork.NewsletterSubscribers.Remove(newsletterSubscriber);
            }

            await _unitOfWork.CompleteAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool NewsletterSubscriberExists(Guid id)
        {
            return _unitOfWork.NewsletterSubscribers.Any(e => e.Id == id);
        }
    }
}



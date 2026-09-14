using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ContentManagementSystem.ApplicationCore.DTOs;
using ContentManagementSystem.ApplicationCore.Entities;
using ContentManagementSystem.ApplicationCore.Interfaces;
using ContentManagementSystem.DataLayer;
using Microsoft.EntityFrameworkCore;

namespace ContentManagementSystem.Services
{
    public class NewsletterSubscriberService : INewsletterSubscriberService
    {
        private readonly ContentManageDbContext _context;

        public NewsletterSubscriberService(ContentManageDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<NewsletterSubscriberDto>> GetAllAsync()
        {
            return await _context.Set<NewsletterSubscriber>()
                .Select(e => new NewsletterSubscriberDto
                {
                    Id = e.Id,
                    Email = e.Email,
                    IsActive = e.IsActive,
                    CreatedAt = e.CreatedAt,                })
                .ToListAsync();
        }

        public async Task<NewsletterSubscriberDto?> GetByIdAsync(Guid id)
        {
            var e = await _context.Set<NewsletterSubscriber>().FindAsync(id);
            if (e == null) return null;

            return new NewsletterSubscriberDto
            {
                Id = e.Id,
                Email = e.Email,
                IsActive = e.IsActive,
                CreatedAt = e.CreatedAt,            };
        }

        public async Task CreateAsync(NewsletterSubscriberDto dto)
        {
            var entity = new NewsletterSubscriber
            {
                Id = Guid.NewGuid(),
                Email = dto.Email,
                IsActive = dto.IsActive,
                CreatedAt = dto.CreatedAt,            };
            _context.Set<NewsletterSubscriber>().Add(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(NewsletterSubscriberDto dto)
        {
            var entity = await _context.Set<NewsletterSubscriber>().FindAsync(dto.Id);
            if (entity != null)
            {
                entity.Email = dto.Email;
                entity.IsActive = dto.IsActive;
                entity.CreatedAt = dto.CreatedAt;                _context.Set<NewsletterSubscriber>().Update(entity);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteAsync(Guid id)
        {
            var entity = await _context.Set<NewsletterSubscriber>().FindAsync(id);
            if (entity != null)
            {
                _context.Set<NewsletterSubscriber>().Remove(entity);
                await _context.SaveChangesAsync();
            }
        }

        public bool Exists(Guid id)
        {
            return _context.Set<NewsletterSubscriber>().Any(e => e.Id == id);
        }
    }
}

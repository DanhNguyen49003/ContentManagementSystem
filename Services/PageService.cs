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
    public class PageService : IPageService
    {
        private readonly ContentManageDbContext _context;

        public PageService(ContentManageDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<PageDto>> GetAllAsync()
        {
            return await _context.Set<Page>()
                .Select(e => new PageDto
                {
                    Id = e.Id,
                    Title = e.Title,
                    Content = e.Content,
                    CreatedAt = e.CreatedAt,                })
                .ToListAsync();
        }

        public async Task<PageDto?> GetByIdAsync(Guid id)
        {
            var e = await _context.Set<Page>().FindAsync(id);
            if (e == null) return null;

            return new PageDto
            {
                Id = e.Id,
                Title = e.Title,
                Content = e.Content,
                CreatedAt = e.CreatedAt,            };
        }

        public async Task CreateAsync(PageDto dto)
        {
            var entity = new Page
            {
                Id = Guid.NewGuid(),
                Title = dto.Title,
                Content = dto.Content,
                CreatedAt = dto.CreatedAt,            };
            _context.Set<Page>().Add(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(PageDto dto)
        {
            var entity = await _context.Set<Page>().FindAsync(dto.Id);
            if (entity != null)
            {
                entity.Title = dto.Title;
                entity.Content = dto.Content;
                entity.CreatedAt = dto.CreatedAt;                _context.Set<Page>().Update(entity);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteAsync(Guid id)
        {
            var entity = await _context.Set<Page>().FindAsync(id);
            if (entity != null)
            {
                _context.Set<Page>().Remove(entity);
                await _context.SaveChangesAsync();
            }
        }

        public bool Exists(Guid id)
        {
            return _context.Set<Page>().Any(e => e.Id == id);
        }
    }
}

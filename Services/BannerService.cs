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
    public class BannerService : IBannerService
    {
        private readonly ContentManageDbContext _context;

        public BannerService(ContentManageDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<BannerDto>> GetAllAsync()
        {
            return await _context.Set<Banner>()
                .Select(e => new BannerDto
                {
                    Id = e.Id,
                    Title = e.Title,
                    ImageUrl = e.ImageUrl,
                    LinkUrl = e.LinkUrl,
                    IsActive = e.IsActive,                })
                .ToListAsync();
        }

        public async Task<BannerDto?> GetByIdAsync(Guid id)
        {
            var e = await _context.Set<Banner>().FindAsync(id);
            if (e == null) return null;

            return new BannerDto
            {
                Id = e.Id,
                Title = e.Title,
                ImageUrl = e.ImageUrl,
                LinkUrl = e.LinkUrl,
                IsActive = e.IsActive,            };
        }

        public async Task CreateAsync(BannerDto dto)
        {
            var entity = new Banner
            {
                Id = Guid.NewGuid(),
                Title = dto.Title,
                ImageUrl = dto.ImageUrl,
                LinkUrl = dto.LinkUrl,
                IsActive = dto.IsActive,            };
            _context.Set<Banner>().Add(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(BannerDto dto)
        {
            var entity = await _context.Set<Banner>().FindAsync(dto.Id);
            if (entity != null)
            {
                entity.Title = dto.Title;
                entity.ImageUrl = dto.ImageUrl;
                entity.LinkUrl = dto.LinkUrl;
                entity.IsActive = dto.IsActive;                _context.Set<Banner>().Update(entity);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteAsync(Guid id)
        {
            var entity = await _context.Set<Banner>().FindAsync(id);
            if (entity != null)
            {
                _context.Set<Banner>().Remove(entity);
                await _context.SaveChangesAsync();
            }
        }

        public bool Exists(Guid id)
        {
            return _context.Set<Banner>().Any(e => e.Id == id);
        }
    }
}

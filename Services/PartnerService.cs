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
    public class PartnerService : IPartnerService
    {
        private readonly ContentManageDbContext _context;

        public PartnerService(ContentManageDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<PartnerDto>> GetAllAsync()
        {
            return await _context.Set<Partner>()
                .Select(e => new PartnerDto
                {
                    Id = e.Id,
                    Name = e.Name,
                    LogoUrl = e.LogoUrl,
                    WebsiteUrl = e.WebsiteUrl,                })
                .ToListAsync();
        }

        public async Task<PartnerDto?> GetByIdAsync(Guid id)
        {
            var e = await _context.Set<Partner>().FindAsync(id);
            if (e == null) return null;

            return new PartnerDto
            {
                Id = e.Id,
                Name = e.Name,
                LogoUrl = e.LogoUrl,
                WebsiteUrl = e.WebsiteUrl,            };
        }

        public async Task CreateAsync(PartnerDto dto)
        {
            var entity = new Partner
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                LogoUrl = dto.LogoUrl,
                WebsiteUrl = dto.WebsiteUrl,            };
            _context.Set<Partner>().Add(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(PartnerDto dto)
        {
            var entity = await _context.Set<Partner>().FindAsync(dto.Id);
            if (entity != null)
            {
                entity.Name = dto.Name;
                entity.LogoUrl = dto.LogoUrl;
                entity.WebsiteUrl = dto.WebsiteUrl;                _context.Set<Partner>().Update(entity);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteAsync(Guid id)
        {
            var entity = await _context.Set<Partner>().FindAsync(id);
            if (entity != null)
            {
                _context.Set<Partner>().Remove(entity);
                await _context.SaveChangesAsync();
            }
        }

        public bool Exists(Guid id)
        {
            return _context.Set<Partner>().Any(e => e.Id == id);
        }
    }
}

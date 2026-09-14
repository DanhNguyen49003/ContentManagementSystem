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
    public class MenuItemService : IMenuItemService
    {
        private readonly ContentManageDbContext _context;

        public MenuItemService(ContentManageDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<MenuItemDto>> GetAllAsync()
        {
            return await _context.Set<MenuItem>()
                .Select(e => new MenuItemDto
                {
                    Id = e.Id,
                    Title = e.Title,
                    Url = e.Url,
                    MenuId = e.MenuId,                })
                .ToListAsync();
        }

        public async Task<MenuItemDto?> GetByIdAsync(Guid id)
        {
            var e = await _context.Set<MenuItem>().FindAsync(id);
            if (e == null) return null;

            return new MenuItemDto
            {
                Id = e.Id,
                Title = e.Title,
                Url = e.Url,
                MenuId = e.MenuId,            };
        }

        public async Task CreateAsync(MenuItemDto dto)
        {
            var entity = new MenuItem
            {
                Id = Guid.NewGuid(),
                Title = dto.Title,
                Url = dto.Url,
                MenuId = dto.MenuId,            };
            _context.Set<MenuItem>().Add(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(MenuItemDto dto)
        {
            var entity = await _context.Set<MenuItem>().FindAsync(dto.Id);
            if (entity != null)
            {
                entity.Title = dto.Title;
                entity.Url = dto.Url;
                entity.MenuId = dto.MenuId;                _context.Set<MenuItem>().Update(entity);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteAsync(Guid id)
        {
            var entity = await _context.Set<MenuItem>().FindAsync(id);
            if (entity != null)
            {
                _context.Set<MenuItem>().Remove(entity);
                await _context.SaveChangesAsync();
            }
        }

        public bool Exists(Guid id)
        {
            return _context.Set<MenuItem>().Any(e => e.Id == id);
        }
    }
}

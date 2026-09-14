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
    public class MenuService : IMenuService
    {
        private readonly ContentManageDbContext _context;

        public MenuService(ContentManageDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<MenuDto>> GetAllAsync()
        {
            return await _context.Set<Menu>()
                .Select(e => new MenuDto
                {
                    Id = e.Id,
                    Name = e.Name,
                    Position = e.Position,
                    CreatedAt = e.CreatedAt,                })
                .ToListAsync();
        }

        public async Task<MenuDto?> GetByIdAsync(Guid id)
        {
            var e = await _context.Set<Menu>().FindAsync(id);
            if (e == null) return null;

            return new MenuDto
            {
                Id = e.Id,
                Name = e.Name,
                Position = e.Position,
                CreatedAt = e.CreatedAt,            };
        }

        public async Task CreateAsync(MenuDto dto)
        {
            var entity = new Menu
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Position = dto.Position,
                CreatedAt = dto.CreatedAt,            };
            _context.Set<Menu>().Add(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(MenuDto dto)
        {
            var entity = await _context.Set<Menu>().FindAsync(dto.Id);
            if (entity != null)
            {
                entity.Name = dto.Name;
                entity.Position = dto.Position;
                entity.CreatedAt = dto.CreatedAt;                _context.Set<Menu>().Update(entity);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteAsync(Guid id)
        {
            var entity = await _context.Set<Menu>().FindAsync(id);
            if (entity != null)
            {
                _context.Set<Menu>().Remove(entity);
                await _context.SaveChangesAsync();
            }
        }

        public bool Exists(Guid id)
        {
            return _context.Set<Menu>().Any(e => e.Id == id);
        }
    }
}

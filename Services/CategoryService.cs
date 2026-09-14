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
    public class CategoryService : ICategoryService
    {
        private readonly ContentManageDbContext _context;

        public CategoryService(ContentManageDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<CategoryDto>> GetAllAsync()
        {
            return await _context.Set<Category>()
                .Select(e => new CategoryDto
                {
                    Id = e.Id,
                    Name = e.Name,
                    Slug = e.Slug,
                    Description = e.Description,                })
                .ToListAsync();
        }

        public async Task<CategoryDto?> GetByIdAsync(Guid id)
        {
            var e = await _context.Set<Category>().FindAsync(id);
            if (e == null) return null;

            return new CategoryDto
            {
                Id = e.Id,
                Name = e.Name,
                Slug = e.Slug,
                Description = e.Description,            };
        }

        public async Task CreateAsync(CategoryDto dto)
        {
            var entity = new Category
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Slug = dto.Slug,
                Description = dto.Description,            };
            _context.Set<Category>().Add(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(CategoryDto dto)
        {
            var entity = await _context.Set<Category>().FindAsync(dto.Id);
            if (entity != null)
            {
                entity.Name = dto.Name;
                entity.Slug = dto.Slug;
                entity.Description = dto.Description;                _context.Set<Category>().Update(entity);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteAsync(Guid id)
        {
            var entity = await _context.Set<Category>().FindAsync(id);
            if (entity != null)
            {
                _context.Set<Category>().Remove(entity);
                await _context.SaveChangesAsync();
            }
        }

        public bool Exists(Guid id)
        {
            return _context.Set<Category>().Any(e => e.Id == id);
        }
    }
}

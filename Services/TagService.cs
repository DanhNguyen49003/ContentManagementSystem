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
    public class TagService : ITagService
    {
        private readonly ContentManageDbContext _context;

        public TagService(ContentManageDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<TagDto>> GetAllAsync()
        {
            return await _context.Set<Tag>()
                .Select(e => new TagDto
                {
                    Id = e.Id,
                    Name = e.Name,                })
                .ToListAsync();
        }

        public async Task<TagDto?> GetByIdAsync(Guid id)
        {
            var e = await _context.Set<Tag>().FindAsync(id);
            if (e == null) return null;

            return new TagDto
            {
                Id = e.Id,
                Name = e.Name,            };
        }

        public async Task CreateAsync(TagDto dto)
        {
            var entity = new Tag
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,            };
            _context.Set<Tag>().Add(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(TagDto dto)
        {
            var entity = await _context.Set<Tag>().FindAsync(dto.Id);
            if (entity != null)
            {
                entity.Name = dto.Name;                _context.Set<Tag>().Update(entity);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteAsync(Guid id)
        {
            var entity = await _context.Set<Tag>().FindAsync(id);
            if (entity != null)
            {
                _context.Set<Tag>().Remove(entity);
                await _context.SaveChangesAsync();
            }
        }

        public bool Exists(Guid id)
        {
            return _context.Set<Tag>().Any(e => e.Id == id);
        }
    }
}

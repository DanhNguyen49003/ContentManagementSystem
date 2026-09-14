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
    public class SettingService : ISettingService
    {
        private readonly ContentManageDbContext _context;

        public SettingService(ContentManageDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<SettingDto>> GetAllAsync()
        {
            return await _context.Set<Setting>()
                .Select(e => new SettingDto
                {
                    Id = e.Id,
                    Key = e.Key,
                    Value = e.Value,
                    Description = e.Description,                })
                .ToListAsync();
        }

        public async Task<SettingDto?> GetByIdAsync(Guid id)
        {
            var e = await _context.Set<Setting>().FindAsync(id);
            if (e == null) return null;

            return new SettingDto
            {
                Id = e.Id,
                Key = e.Key,
                Value = e.Value,
                Description = e.Description,            };
        }

        public async Task CreateAsync(SettingDto dto)
        {
            var entity = new Setting
            {
                Id = Guid.NewGuid(),
                Key = dto.Key,
                Value = dto.Value,
                Description = dto.Description,            };
            _context.Set<Setting>().Add(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(SettingDto dto)
        {
            var entity = await _context.Set<Setting>().FindAsync(dto.Id);
            if (entity != null)
            {
                entity.Key = dto.Key;
                entity.Value = dto.Value;
                entity.Description = dto.Description;                _context.Set<Setting>().Update(entity);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteAsync(Guid id)
        {
            var entity = await _context.Set<Setting>().FindAsync(id);
            if (entity != null)
            {
                _context.Set<Setting>().Remove(entity);
                await _context.SaveChangesAsync();
            }
        }

        public bool Exists(Guid id)
        {
            return _context.Set<Setting>().Any(e => e.Id == id);
        }
    }
}

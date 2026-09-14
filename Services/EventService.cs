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
    public class EventService : IEventService
    {
        private readonly ContentManageDbContext _context;

        public EventService(ContentManageDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<EventDto>> GetAllAsync()
        {
            return await _context.Set<Event>()
                .Select(e => new EventDto
                {
                    Id = e.Id,
                    Name = e.Name,
                    EventDate = e.EventDate,
                    Location = e.Location,
                    CreatedAt = e.CreatedAt,                })
                .ToListAsync();
        }

        public async Task<EventDto?> GetByIdAsync(Guid id)
        {
            var e = await _context.Set<Event>().FindAsync(id);
            if (e == null) return null;

            return new EventDto
            {
                Id = e.Id,
                Name = e.Name,
                EventDate = e.EventDate,
                Location = e.Location,
                CreatedAt = e.CreatedAt,            };
        }

        public async Task CreateAsync(EventDto dto)
        {
            var entity = new Event
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                EventDate = dto.EventDate,
                Location = dto.Location,
                CreatedAt = dto.CreatedAt,            };
            _context.Set<Event>().Add(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(EventDto dto)
        {
            var entity = await _context.Set<Event>().FindAsync(dto.Id);
            if (entity != null)
            {
                entity.Name = dto.Name;
                entity.EventDate = dto.EventDate;
                entity.Location = dto.Location;
                entity.CreatedAt = dto.CreatedAt;                _context.Set<Event>().Update(entity);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteAsync(Guid id)
        {
            var entity = await _context.Set<Event>().FindAsync(id);
            if (entity != null)
            {
                _context.Set<Event>().Remove(entity);
                await _context.SaveChangesAsync();
            }
        }

        public bool Exists(Guid id)
        {
            return _context.Set<Event>().Any(e => e.Id == id);
        }
    }
}

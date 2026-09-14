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
    public class ContactMessageService : IContactMessageService
    {
        private readonly ContentManageDbContext _context;

        public ContactMessageService(ContentManageDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ContactMessageDto>> GetAllAsync()
        {
            return await _context.Set<ContactMessage>()
                .Select(e => new ContactMessageDto
                {
                    Id = e.Id,
                    Name = e.Name,
                    Email = e.Email,
                    Message = e.Message,
                    CreatedAt = e.CreatedAt,                })
                .ToListAsync();
        }

        public async Task<ContactMessageDto?> GetByIdAsync(Guid id)
        {
            var e = await _context.Set<ContactMessage>().FindAsync(id);
            if (e == null) return null;

            return new ContactMessageDto
            {
                Id = e.Id,
                Name = e.Name,
                Email = e.Email,
                Message = e.Message,
                CreatedAt = e.CreatedAt,            };
        }

        public async Task CreateAsync(ContactMessageDto dto)
        {
            var entity = new ContactMessage
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Email = dto.Email,
                Message = dto.Message,
                CreatedAt = dto.CreatedAt,            };
            _context.Set<ContactMessage>().Add(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(ContactMessageDto dto)
        {
            var entity = await _context.Set<ContactMessage>().FindAsync(dto.Id);
            if (entity != null)
            {
                entity.Name = dto.Name;
                entity.Email = dto.Email;
                entity.Message = dto.Message;
                entity.CreatedAt = dto.CreatedAt;                _context.Set<ContactMessage>().Update(entity);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteAsync(Guid id)
        {
            var entity = await _context.Set<ContactMessage>().FindAsync(id);
            if (entity != null)
            {
                _context.Set<ContactMessage>().Remove(entity);
                await _context.SaveChangesAsync();
            }
        }

        public bool Exists(Guid id)
        {
            return _context.Set<ContactMessage>().Any(e => e.Id == id);
        }
    }
}

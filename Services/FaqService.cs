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
    public class FaqService : IFaqService
    {
        private readonly ContentManageDbContext _context;

        public FaqService(ContentManageDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<FaqDto>> GetAllAsync()
        {
            return await _context.Set<Faq>()
                .Select(e => new FaqDto
                {
                    Id = e.Id,
                    Question = e.Question,
                    Answer = e.Answer,
                    CreatedAt = e.CreatedAt,                })
                .ToListAsync();
        }

        public async Task<FaqDto?> GetByIdAsync(Guid id)
        {
            var e = await _context.Set<Faq>().FindAsync(id);
            if (e == null) return null;

            return new FaqDto
            {
                Id = e.Id,
                Question = e.Question,
                Answer = e.Answer,
                CreatedAt = e.CreatedAt,            };
        }

        public async Task CreateAsync(FaqDto dto)
        {
            var entity = new Faq
            {
                Id = Guid.NewGuid(),
                Question = dto.Question,
                Answer = dto.Answer,
                CreatedAt = dto.CreatedAt,            };
            _context.Set<Faq>().Add(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(FaqDto dto)
        {
            var entity = await _context.Set<Faq>().FindAsync(dto.Id);
            if (entity != null)
            {
                entity.Question = dto.Question;
                entity.Answer = dto.Answer;
                entity.CreatedAt = dto.CreatedAt;                _context.Set<Faq>().Update(entity);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteAsync(Guid id)
        {
            var entity = await _context.Set<Faq>().FindAsync(id);
            if (entity != null)
            {
                _context.Set<Faq>().Remove(entity);
                await _context.SaveChangesAsync();
            }
        }

        public bool Exists(Guid id)
        {
            return _context.Set<Faq>().Any(e => e.Id == id);
        }
    }
}

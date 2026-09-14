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
    public class TestimonialService : ITestimonialService
    {
        private readonly ContentManageDbContext _context;

        public TestimonialService(ContentManageDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<TestimonialDto>> GetAllAsync()
        {
            return await _context.Set<Testimonial>()
                .Select(e => new TestimonialDto
                {
                    Id = e.Id,
                    CustomerName = e.CustomerName,
                    Content = e.Content,
                    Rating = e.Rating,
                    CreatedAt = e.CreatedAt,                })
                .ToListAsync();
        }

        public async Task<TestimonialDto?> GetByIdAsync(Guid id)
        {
            var e = await _context.Set<Testimonial>().FindAsync(id);
            if (e == null) return null;

            return new TestimonialDto
            {
                Id = e.Id,
                CustomerName = e.CustomerName,
                Content = e.Content,
                Rating = e.Rating,
                CreatedAt = e.CreatedAt,            };
        }

        public async Task CreateAsync(TestimonialDto dto)
        {
            var entity = new Testimonial
            {
                Id = Guid.NewGuid(),
                CustomerName = dto.CustomerName,
                Content = dto.Content,
                Rating = dto.Rating,
                CreatedAt = dto.CreatedAt,            };
            _context.Set<Testimonial>().Add(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(TestimonialDto dto)
        {
            var entity = await _context.Set<Testimonial>().FindAsync(dto.Id);
            if (entity != null)
            {
                entity.CustomerName = dto.CustomerName;
                entity.Content = dto.Content;
                entity.Rating = dto.Rating;
                entity.CreatedAt = dto.CreatedAt;                _context.Set<Testimonial>().Update(entity);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteAsync(Guid id)
        {
            var entity = await _context.Set<Testimonial>().FindAsync(id);
            if (entity != null)
            {
                _context.Set<Testimonial>().Remove(entity);
                await _context.SaveChangesAsync();
            }
        }

        public bool Exists(Guid id)
        {
            return _context.Set<Testimonial>().Any(e => e.Id == id);
        }
    }
}

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
    public class CommentService : ICommentService
    {
        private readonly ContentManageDbContext _context;

        public CommentService(ContentManageDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<CommentDto>> GetAllAsync()
        {
            return await _context.Set<Comment>()
                .Select(e => new CommentDto
                {
                    Id = e.Id,
                    Content = e.Content,
                    CreatedAt = e.CreatedAt,
                    IsApproved = e.IsApproved,
                    PostId = e.PostId,
                    UserId = e.UserId,                })
                .ToListAsync();
        }

        public async Task<CommentDto?> GetByIdAsync(Guid id)
        {
            var e = await _context.Set<Comment>().FindAsync(id);
            if (e == null) return null;

            return new CommentDto
            {
                Id = e.Id,
                Content = e.Content,
                CreatedAt = e.CreatedAt,
                IsApproved = e.IsApproved,
                PostId = e.PostId,
                UserId = e.UserId,            };
        }

        public async Task CreateAsync(CommentDto dto)
        {
            var entity = new Comment
            {
                Id = Guid.NewGuid(),
                Content = dto.Content,
                CreatedAt = dto.CreatedAt,
                IsApproved = dto.IsApproved,
                PostId = dto.PostId,
                UserId = dto.UserId,            };
            _context.Set<Comment>().Add(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(CommentDto dto)
        {
            var entity = await _context.Set<Comment>().FindAsync(dto.Id);
            if (entity != null)
            {
                entity.Content = dto.Content;
                entity.CreatedAt = dto.CreatedAt;
                entity.IsApproved = dto.IsApproved;
                entity.PostId = dto.PostId;
                entity.UserId = dto.UserId;                _context.Set<Comment>().Update(entity);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteAsync(Guid id)
        {
            var entity = await _context.Set<Comment>().FindAsync(id);
            if (entity != null)
            {
                _context.Set<Comment>().Remove(entity);
                await _context.SaveChangesAsync();
            }
        }

        public bool Exists(Guid id)
        {
            return _context.Set<Comment>().Any(e => e.Id == id);
        }
    }
}

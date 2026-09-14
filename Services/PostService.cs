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
    public class PostService : IPostService
    {
        private readonly ContentManageDbContext _context;

        public PostService(ContentManageDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<PostDto>> GetAllPostsAsync()
        {
            return await _context.Posts
                .Include(p => p.Author)
                .Include(p => p.Category)
                .Select(p => new PostDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    Slug = p.Slug,
                    Summary = p.Summary,
                    Content = p.Content,
                    IsPublished = p.IsPublished,
                    CreatedAt = p.CreatedAt,
                    AuthorId = p.AuthorId,
                    AuthorName = p.Author != null ? p.Author.UserName : "Unknown",
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category != null ? p.Category.Name : "Uncategorized"
                })
                .ToListAsync();
        }

        public async Task<PostDto?> GetPostByIdAsync(Guid id)
        {
            var p = await _context.Posts
                .Include(p => p.Author)
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (p == null) return null;

            return new PostDto
            {
                Id = p.Id,
                Title = p.Title,
                Slug = p.Slug,
                Summary = p.Summary,
                Content = p.Content,
                IsPublished = p.IsPublished,
                CreatedAt = p.CreatedAt,
                AuthorId = p.AuthorId,
                AuthorName = p.Author != null ? p.Author.UserName : "Unknown",
                CategoryId = p.CategoryId,
                CategoryName = p.Category != null ? p.Category.Name : "Uncategorized"
            };
        }

        public async Task<Post> CreatePostAsync(PostDto postDto)
        {
            var post = new Post
            {
                Id = Guid.NewGuid(),
                Title = postDto.Title,
                Slug = postDto.Slug,
                Summary = postDto.Summary,
                Content = postDto.Content,
                IsPublished = postDto.IsPublished,
                CreatedAt = DateTime.UtcNow,
                AuthorId = postDto.AuthorId,
                CategoryId = postDto.CategoryId
            };

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();
            return post;
        }

        public async Task UpdatePostAsync(PostDto postDto)
        {
            var post = await _context.Posts.FindAsync(postDto.Id);
            if (post != null)
            {
                post.Title = postDto.Title;
                post.Slug = postDto.Slug;
                post.Summary = postDto.Summary;
                post.Content = postDto.Content;
                post.IsPublished = postDto.IsPublished;
                post.CategoryId = postDto.CategoryId;
                post.AuthorId = postDto.AuthorId;
                
                _context.Posts.Update(post);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeletePostAsync(Guid id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post != null)
            {
                _context.Posts.Remove(post);
                await _context.SaveChangesAsync();
            }
        }

        public bool PostExists(Guid id)
        {
            return _context.Posts.Any(e => e.Id == id);
        }
    }
}


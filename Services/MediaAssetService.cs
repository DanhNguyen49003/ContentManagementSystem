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
    public class MediaAssetService : IMediaAssetService
    {
        private readonly ContentManageDbContext _context;

        public MediaAssetService(ContentManageDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<MediaAssetDto>> GetAllAsync()
        {
            return await _context.Set<MediaAsset>()
                .Select(e => new MediaAssetDto
                {
                    Id = e.Id,
                    FileName = e.FileName,
                    FilePath = e.FilePath,
                    ContentType = e.ContentType,
                    FileSize = e.FileSize,
                    UploadedAt = e.UploadedAt,
                    UploadedById = e.UploadedById,                })
                .ToListAsync();
        }

        public async Task<MediaAssetDto?> GetByIdAsync(Guid id)
        {
            var e = await _context.Set<MediaAsset>().FindAsync(id);
            if (e == null) return null;

            return new MediaAssetDto
            {
                Id = e.Id,
                FileName = e.FileName,
                FilePath = e.FilePath,
                ContentType = e.ContentType,
                FileSize = e.FileSize,
                UploadedAt = e.UploadedAt,
                UploadedById = e.UploadedById,            };
        }

        public async Task CreateAsync(MediaAssetDto dto)
        {
            var entity = new MediaAsset
            {
                Id = Guid.NewGuid(),
                FileName = dto.FileName,
                FilePath = dto.FilePath,
                ContentType = dto.ContentType,
                FileSize = dto.FileSize,
                UploadedAt = dto.UploadedAt,
                UploadedById = dto.UploadedById,            };
            _context.Set<MediaAsset>().Add(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(MediaAssetDto dto)
        {
            var entity = await _context.Set<MediaAsset>().FindAsync(dto.Id);
            if (entity != null)
            {
                entity.FileName = dto.FileName;
                entity.FilePath = dto.FilePath;
                entity.ContentType = dto.ContentType;
                entity.FileSize = dto.FileSize;
                entity.UploadedAt = dto.UploadedAt;
                entity.UploadedById = dto.UploadedById;                _context.Set<MediaAsset>().Update(entity);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteAsync(Guid id)
        {
            var entity = await _context.Set<MediaAsset>().FindAsync(id);
            if (entity != null)
            {
                _context.Set<MediaAsset>().Remove(entity);
                await _context.SaveChangesAsync();
            }
        }

        public bool Exists(Guid id)
        {
            return _context.Set<MediaAsset>().Any(e => e.Id == id);
        }
    }
}

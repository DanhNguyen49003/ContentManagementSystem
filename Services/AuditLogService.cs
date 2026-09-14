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
    public class AuditLogService : IAuditLogService
    {
        private readonly ContentManageDbContext _context;

        public AuditLogService(ContentManageDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<AuditLogDto>> GetAllAsync()
        {
            return await _context.Set<AuditLog>()
                .Select(e => new AuditLogDto
                {
                    Id = e.Id,
                    Action = e.Action,
                    TableName = e.TableName,
                    RecordId = e.RecordId,
                    OldValues = e.OldValues,
                    NewValues = e.NewValues,
                    Timestamp = e.Timestamp,
                    UserId = e.UserId,                })
                .ToListAsync();
        }

        public async Task<AuditLogDto?> GetByIdAsync(Guid id)
        {
            var e = await _context.Set<AuditLog>().FindAsync(id);
            if (e == null) return null;

            return new AuditLogDto
            {
                Id = e.Id,
                Action = e.Action,
                TableName = e.TableName,
                RecordId = e.RecordId,
                OldValues = e.OldValues,
                NewValues = e.NewValues,
                Timestamp = e.Timestamp,
                UserId = e.UserId,            };
        }

        public async Task CreateAsync(AuditLogDto dto)
        {
            var entity = new AuditLog
            {
                Id = Guid.NewGuid(),
                Action = dto.Action,
                TableName = dto.TableName,
                RecordId = dto.RecordId,
                OldValues = dto.OldValues,
                NewValues = dto.NewValues,
                Timestamp = dto.Timestamp,
                UserId = dto.UserId,            };
            _context.Set<AuditLog>().Add(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(AuditLogDto dto)
        {
            var entity = await _context.Set<AuditLog>().FindAsync(dto.Id);
            if (entity != null)
            {
                entity.Action = dto.Action;
                entity.TableName = dto.TableName;
                entity.RecordId = dto.RecordId;
                entity.OldValues = dto.OldValues;
                entity.NewValues = dto.NewValues;
                entity.Timestamp = dto.Timestamp;
                entity.UserId = dto.UserId;                _context.Set<AuditLog>().Update(entity);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteAsync(Guid id)
        {
            var entity = await _context.Set<AuditLog>().FindAsync(id);
            if (entity != null)
            {
                _context.Set<AuditLog>().Remove(entity);
                await _context.SaveChangesAsync();
            }
        }

        public bool Exists(Guid id)
        {
            return _context.Set<AuditLog>().Any(e => e.Id == id);
        }
    }
}

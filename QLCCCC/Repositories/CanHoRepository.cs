using Microsoft.EntityFrameworkCore;
using QLCCCC.Data;
using QLCCCC.Models;
using QLCCCC.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QLCCCC.Repositories
{
    public class CanHoRepository : ICanHoRepository
    {
        private readonly ApplicationDbContext _context;

        public CanHoRepository(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<IEnumerable<CanHo>> GetAllAsync()
        {
            return await _context.CanHos
                .Include(c => c.ChungCu)
                .Include(c => c.HinhAnhCanHos)
                .AsNoTracking() // Tối ưu hiệu suất cho truy vấn chỉ đọc
                .ToListAsync();
        }

        public async Task<CanHo?> GetByIdAsync(int id)
        {
            return await _context.CanHos
                .AsNoTracking() // Tối ưu hiệu suất cho truy vấn chỉ đọc
                .FirstOrDefaultAsync(c => c.ID == id);
        }

        public async Task<CanHo?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.CanHos
                .Include(c => c.ChungCu)
                .Include(c => c.HinhAnhCanHos)
                .AsNoTracking() // Tối ưu hiệu suất cho truy vấn chỉ đọc
                .FirstOrDefaultAsync(c => c.ID == id);
        }

        public async Task<CanHo?> GetByMaCanAndChungCuAsync(string maCan, int idChungCu)
        {
            if (string.IsNullOrWhiteSpace(maCan))
            {
                throw new ArgumentException("Mã căn không được để trống.", nameof(maCan));
            }

            return await _context.CanHos
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.MaCan == maCan && c.ID_ChungCu == idChungCu);
        }

        public async Task AddAsync(CanHo canHo)
        {
            if (canHo == null)
            {
                throw new ArgumentNullException(nameof(canHo));
            }

            var existingCanHo = await GetByMaCanAndChungCuAsync(canHo.MaCan, canHo.ID_ChungCu);
            if (existingCanHo != null)
            {
                throw new InvalidOperationException($"Mã căn '{canHo.MaCan}' đã tồn tại trong chung cư '{canHo.ChungCu?.Ten ?? "Unknown"}'!");
            }

            _context.CanHos.Add(canHo);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(CanHo canHo)
        {
            if (canHo == null)
            {
                throw new ArgumentNullException(nameof(canHo));
            }

            // Kiểm tra xem có thay đổi gì để tránh lưu không cần thiết
            _context.Entry(canHo).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var canHo = await GetByIdWithDetailsAsync(id);
            if (canHo == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy căn hộ với ID {id}.");
            }

            await DeleteImagesAsync(canHo.HinhAnhCanHos);
            _context.CanHos.Remove(canHo);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteImagesAsync(ICollection<HinhAnhCanHo> hinhAnhs)
        {
            if (hinhAnhs != null && hinhAnhs.Any())
            {
                _context.HinhAnhCanHos.RemoveRange(hinhAnhs);
                await _context.SaveChangesAsync();
            }
        }
    }
}
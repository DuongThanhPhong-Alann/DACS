using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLCCCC.Data;
using QLCCCC.Models;
using QLCCCC.Repositories.Interfaces;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace QLCCCC.Controllers
{
    public class DichVuController : Controller
    {
        private readonly IDichVuRepository _dichVuRepository;
        private readonly ApplicationDbContext _context;

        public DichVuController(IDichVuRepository dichVuRepository, ApplicationDbContext context)
        {
            _dichVuRepository = dichVuRepository ?? throw new ArgumentNullException(nameof(dichVuRepository));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // Trang danh sách dịch vụ
        public async Task<IActionResult> Index()
        {
            var dichVus = await _dichVuRepository.GetAllAsync() ?? new List<DichVu>();

            foreach (var dichVu in dichVus)
            {
                var hinhAnhs = await _context.HinhAnhDichVus
                    .Where(h => h.ID_DichVu == dichVu.ID)
                    .ToListAsync() ?? new List<HinhAnhDichVu>();

                dichVu.HinhAnhDichVus = hinhAnhs;
            }

            return View(dichVus);
        }

        // Chi tiết dịch vụ
        public async Task<IActionResult> Details(int id)
        {
            var dichVu = await _dichVuRepository.GetByIdAsync(id);
            if (dichVu == null) return NotFound();

            // Nạp dữ liệu hình ảnh từ bảng HinhAnhDichVus
            var hinhAnhs = await _context.HinhAnhDichVus
                .Where(h => h.ID_DichVu == dichVu.ID)
                .ToListAsync() ?? new List<HinhAnhDichVu>();

            dichVu.HinhAnhDichVus = hinhAnhs;

            return View(dichVu);
        }

        // Tạo mới dịch vụ
        [HttpGet]
        public IActionResult Create()
        {
            return View(new DichVu());
        }

        [HttpPost]
        public async Task<IActionResult> Create(DichVu model, IFormFile? HinhAnhDichVu)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (HinhAnhDichVu != null)
            {
                var fileName = Path.GetFileName(HinhAnhDichVu.FileName);
                var filePath = Path.Combine("wwwroot/images", fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await HinhAnhDichVu.CopyToAsync(stream);
                }

                model.HinhAnhDichVus = new List<HinhAnhDichVu>
                {
                    new HinhAnhDichVu { DuongDan = "/images/" + fileName }
                };
            }
            else
            {
                model.HinhAnhDichVus ??= new List<HinhAnhDichVu>();
            }

            _context.DichVus.Add(model);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // Chỉnh sửa dịch vụ
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var dichVu = await _dichVuRepository.GetByIdAsync(id);
            if (dichVu == null) return NotFound();
            return View(dichVu);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, DichVu model, IFormFile? HinhAnhDichVu)
        {
            if (id != model.ID)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var dichVu = await _context.DichVus.Include(d => d.HinhAnhDichVus)
                                               .FirstOrDefaultAsync(d => d.ID == id);
            if (dichVu == null)
            {
                return NotFound();
            }

            dichVu.TenDichVu = model.TenDichVu;
            dichVu.MoTa = model.MoTa;
            dichVu.Gia = model.Gia;

            if (HinhAnhDichVu != null)
            {
                var fileName = Path.GetFileName(HinhAnhDichVu.FileName);
                var filePath = Path.Combine("wwwroot/images", fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await HinhAnhDichVu.CopyToAsync(stream);
                }

                _context.HinhAnhDichVus.RemoveRange(dichVu.HinhAnhDichVus);
                dichVu.HinhAnhDichVus = new List<HinhAnhDichVu>
                {
                    new HinhAnhDichVu { DuongDan = "/images/" + fileName }
                };
            }

            _context.Update(dichVu);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // Phương thức GET để hiển thị xác nhận xóa
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var dichVu = await _dichVuRepository.GetByIdAsync(id);
            if (dichVu == null)
            {
                return NotFound();
            }
            return View(dichVu);
        }

        // Phương thức POST để thực hiện xóa
        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var images = await _context.HinhAnhDichVus.Where(h => h.ID_DichVu == id).ToListAsync() ?? new List<HinhAnhDichVu>();

            foreach (var image in images)
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", image.DuongDan.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
                _context.HinhAnhDichVus.Remove(image);
            }

            await _dichVuRepository.DeleteAsync(id);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // Đăng ký dịch vụ cho người dùng
        [Authorize]
        public async Task<IActionResult> RegisterService(int id)
        {
            var dichVu = await _dichVuRepository.GetByIdAsync(id);
            if (dichVu == null)
            {
                return Json(new { success = false, message = "Dịch vụ không tồn tại." });
            }

            string? email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
            {
                return Json(new { success = false, message = "Không tìm thấy email người dùng." });
            }

            var user = await _context.NguoiDungs.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy thông tin người dùng." });
            }

            var cuDan = await _context.CuDans
                .Include(c => c.CanHo)
                .Include(c => c.ChungCu)
                .FirstOrDefaultAsync(c => c.ID_NguoiDung == user.ID);
            if (cuDan == null)
            {
                return Json(new { success = false, message = "Không tìm thấy thông tin cư dân." });
            }

            var response = new
            {
                success = true,
                dichVuId = dichVu.ID,
                tenDichVu = dichVu.TenDichVu,
                hoTen = user.HoTen,
                email = user.Email,
                soDienThoai = user.SoDienThoai,
                idCanHo = cuDan.ID_CanHo,
                maCan = cuDan.CanHo?.MaCan ?? "Không có thông tin",
                idChungCu = cuDan.ID_ChungCu ?? 0,
                tenChungCu = cuDan.ChungCu?.Ten ?? "Không có thông tin",
                soTien = dichVu.Gia
            };

            return Json(response);
        }

        // Xác nhận đăng ký dịch vụ
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmRegisterService([FromBody] RegisterServiceRequest request)
        {
            var dichVu = await _dichVuRepository.GetByIdAsync(request.DichVuId);
            if (dichVu == null)
            {
                return Json(new { success = false, message = "Dịch vụ không tồn tại." });
            }

            var hoaDon = new HoaDonDichVu
            {
                ID_CanHo = request.IdCanHo,
                ID_ChungCu = request.IdChungCu,
                SoTien = request.SoTien,
                NgayLap = DateTime.Now,
                TrangThai = "Chưa thanh toán",
                HoaDonDichVu_DichVus = new List<HoaDonDichVu_DichVu>()
            };

            var hoaDonDichVu_DichVu = new HoaDonDichVu_DichVu
            {
                ID_DichVu = request.DichVuId,
                HoaDonDichVu = hoaDon
            };

            hoaDon.HoaDonDichVu_DichVus.Add(hoaDonDichVu_DichVu);

            _context.HoaDonDichVus.Add(hoaDon);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đăng ký dịch vụ thành công!" });
        }
    }
}
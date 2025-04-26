﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using QLCCCC.Models;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using QLCCCC.Data;
using QLCCCC.Repositories.Interfaces;

namespace QLCCCC.Controllers
{
    public class PhanAnhController : Controller
    {
        private readonly IEmailService _emailService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PhanAnhController> _logger;

        public PhanAnhController(IEmailService emailService, ApplicationDbContext context, ILogger<PhanAnhController> logger)
        {
            _emailService = emailService;
            _context = context;
            _logger = logger;
        }

        // GET: PhanAnh
        public async Task<IActionResult> Index()
        {
            var userIdString = User.FindFirst("UserId")?.Value;

            if (string.IsNullOrEmpty(userIdString))
                return Unauthorized();

            int userId = int.Parse(userIdString);

            IQueryable<PhanAnh> query = _context.PhanAnhs
                .Include(p => p.NguoiDung)
                    .ThenInclude(nd => nd.CuDan)
                        .ThenInclude(cd => cd.CanHo)
                            .ThenInclude(ch => ch.ChungCu)
                .Include(p => p.NguoiDung)
                    .ThenInclude(nd => nd.CuDan)
                        .ThenInclude(cd => cd.ChungCu);

            if (User.IsInRole("Cư dân"))
            {
                // Lọc phản ánh chỉ của người dùng hiện tại
                query = query.Where(p => p.ID_NguoiDung == userId);
            }

            var phanAnhs = await query.ToListAsync();

            return View(phanAnhs);
        }

        // GET: PhanAnh/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var phanAnh = await _context.PhanAnhs
                .Include(p => p.NguoiDung)
                    .ThenInclude(nd => nd.CuDan)
                        .ThenInclude(cd => cd.CanHo)
                            .ThenInclude(ch => ch.ChungCu)
                .FirstOrDefaultAsync(m => m.ID == id);

            if (phanAnh == null) return NotFound();

            return View(phanAnh);
        }


        // GET: PhanAnh/Create
        [Authorize] // Đảm bảo người dùng đã đăng nhập
        public IActionResult Create()
        {
            return View();
        }

        // POST: PhanAnh/Create
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("NoiDung")] PhanAnh phanAnh, IFormFile? HinhAnhFile, string? HinhAnhLink)
        {
            if (ModelState.IsValid)
            {
                var userIdStr = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();

                var userId = int.Parse(userIdStr);

                phanAnh.ID_NguoiDung = userId;
                phanAnh.NgayGui = DateTime.Now;
                phanAnh.TrangThai = TrangThaiPhanAnh.ChuaXuLy;

                // Xử lý ảnh đính kèm
                if (HinhAnhFile != null && HinhAnhFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "phananh");
                    Directory.CreateDirectory(uploadsFolder);
                    var fileName = $"{Guid.NewGuid()}_{HinhAnhFile.FileName}";
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await HinhAnhFile.CopyToAsync(stream);
                    }

                    phanAnh.HinhAnh = $"/uploads/phananh/{fileName}";
                }
                else if (!string.IsNullOrWhiteSpace(HinhAnhLink))
                {
                    phanAnh.HinhAnh = HinhAnhLink.Trim();
                }

                _context.Add(phanAnh);
                await _context.SaveChangesAsync();

                // Lấy thông tin cư dân và căn hộ để gửi mail
                var nguoiDung = await _context.NguoiDungs
                    .Include(nd => nd.CuDan)
                        .ThenInclude(cd => cd.CanHo)
                    .Include(nd => nd.CuDan)
                        .ThenInclude(cd => cd.ChungCu)
                    .FirstOrDefaultAsync(nd => nd.ID == userId);

                if (nguoiDung == null || nguoiDung.CuDan == null)
                {
                    _logger.LogError($"Không tìm thấy thông tin cư dân với ID người dùng {userId}");
                    return RedirectToAction(nameof(Index));
                }

                // Các thông tin để chèn vào email
                var maCuDan = $"ND{nguoiDung.ID:D4}";
                var tenCuDan = nguoiDung.HoTen;
                var emailCuDan = nguoiDung.Email;
                var maCanHo = nguoiDung.CuDan.CanHo?.MaCan ?? "Không có";
                var tenChungCu = nguoiDung.CuDan.ChungCu?.Ten ?? "Không có";
                var soTang = nguoiDung.CuDan.ChungCu?.SoTang?.ToString() ?? "Không có";

                // Soạn nội dung email
                var subject = "Thông báo: Cư dân gửi phản ánh mới";
                var body = $@"
            <p>Kính gửi <strong>Ban quản lý</strong>,</p>

            <p>Một cư dân đã gửi phản ánh mới với thông tin chi tiết như sau:</p>

            <table style='border-collapse: collapse; width: 100%;'>
                <tr>
                    <td style='border: 1px solid #ddd; padding: 8px;'><strong>Mã cư dân:</strong></td>
                    <td style='border: 1px solid #ddd; padding: 8px;'>{maCuDan}</td>
                </tr>
                <tr>
                    <td style='border: 1px solid #ddd; padding: 8px;'><strong>Tên cư dân:</strong></td>
                    <td style='border: 1px solid #ddd; padding: 8px;'>{tenCuDan}</td>
                </tr>
                <tr>
                    <td style='border: 1px solid #ddd; padding: 8px;'><strong>Email:</strong></td>
                    <td style='border: 1px solid #ddd; padding: 8px;'>{emailCuDan}</td>
                </tr>
                <tr>
                    <td style='border: 1px solid #ddd; padding: 8px;'><strong>Mã căn hộ:</strong></td>
                    <td style='border: 1px solid #ddd; padding: 8px;'>{maCanHo}</td>
                </tr>
                <tr>
                    <td style='border: 1px solid #ddd; padding: 8px;'><strong>Chung cư:</strong></td>
                    <td style='border: 1px solid #ddd; padding: 8px;'>{tenChungCu}</td>
                </tr>
                <tr>
                    <td style='border: 1px solid #ddd; padding: 8px;'><strong>Số tầng:</strong></td>
                    <td style='border: 1px solid #ddd; padding: 8px;'>{soTang}</td>
                </tr>
                <tr>
                    <td style='border: 1px solid #ddd; padding: 8px;'><strong>Ngày gửi:</strong></td>
                    <td style='border: 1px solid #ddd; padding: 8px;'>{phanAnh.NgayGui:HH:mm dd/MM/yyyy}</td>
                </tr>
                <tr>
                    <td style='border: 1px solid #ddd; padding: 8px;'><strong>Nội dung phản ánh:</strong></td>
                    <td style='border: 1px solid #ddd; padding: 8px;'>{phanAnh.NoiDung}</td>
                </tr>
                <tr>
                    <td style='border: 1px solid #ddd; padding: 8px;'><strong>Trạng thái:</strong></td>
                    <td style='border: 1px solid #ddd; padding: 8px;'>{phanAnh.TrangThai}</td>
                </tr>
            </table>

            <p>Vui lòng truy cập hệ thống để kiểm tra và xử lý phản ánh này trong thời gian sớm nhất.</p>

            <p>Trân trọng,<br/>Hệ thống quản lý cư dân</p>
        ";

                // Gửi email
                await _emailService.SendEmailAsync("duongthanhphong1618@gmail.com", subject, body);

                return RedirectToAction(nameof(Index));
            }

            return View(phanAnh);
        }




        // GET: PhanAnh/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var phanAnh = await _context.PhanAnhs.FindAsync(id);
            if (phanAnh == null) return NotFound();

            ViewBag.NguoiDungList = new SelectList(_context.NguoiDungs, "ID", "HoTen", phanAnh.ID_NguoiDung);
            ViewBag.TrangThaiList = new SelectList(Enum.GetValues(typeof(TrangThaiPhanAnh))
                .Cast<TrangThaiPhanAnh>()
                .Select(t => new { Value = t.ToString(), Text = GetDisplayName(t) }), "Value", "Text", phanAnh.TrangThai.ToString());
            return View(phanAnh);
        }

        // POST: PhanAnh/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,ID_NguoiDung,NoiDung,TrangThai,NgayGui")] PhanAnh phanAnh)
        {
            if (id != phanAnh.ID) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(phanAnh);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PhanAnhExists(phanAnh.ID)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewBag.NguoiDungList = new SelectList(_context.NguoiDungs, "ID", "HoTen", phanAnh.ID_NguoiDung);
            ViewBag.TrangThaiList = new SelectList(Enum.GetValues(typeof(TrangThaiPhanAnh))
                .Cast<TrangThaiPhanAnh>()
                .Select(t => new { Value = t.ToString(), Text = GetDisplayName(t) }), "Value", "Text", phanAnh.TrangThai.ToString());
            return View(phanAnh);
        }

        // GET: PhanAnh/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var phanAnh = await _context.PhanAnhs
                .Include(p => p.NguoiDung)
                .FirstOrDefaultAsync(m => m.ID == id);

            if (phanAnh == null) return NotFound();

            return View(phanAnh);
        }

        // POST: PhanAnh/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var phanAnh = await _context.PhanAnhs.FindAsync(id);
            if (phanAnh != null)
            {
                _context.PhanAnhs.Remove(phanAnh);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool PhanAnhExists(int id)
        {
            return _context.PhanAnhs.Any(e => e.ID == id);
        }

        private static string GetDisplayName(Enum enumValue)
        {
            var memberInfo = enumValue.GetType().GetMember(enumValue.ToString()).FirstOrDefault();
            if (memberInfo != null)
            {
                var displayAttribute = memberInfo.GetCustomAttribute<DisplayAttribute>(false);
                return displayAttribute?.Name ?? enumValue.ToString();
            }
            return enumValue.ToString();
        }

        // GET: PhanAnh/Reply/5
        [Authorize(Roles = "Ban quản lý")] // Chỉ Ban quản lý mới có thể phản hồi
        public async Task<IActionResult> Reply(int id)
        {
            var phanAnh = await _context.PhanAnhs
                .Include(p => p.NguoiDung)
                .FirstOrDefaultAsync(p => p.ID == id);
            if (phanAnh == null) return NotFound();

            return View(phanAnh);
        }

        // POST: PhanAnh/Reply/5
        [HttpPost]
        [Authorize(Roles = "Ban quản lý")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(int id, string phanHoi)
        {
            var phanAnh = await _context.PhanAnhs
                .Include(p => p.NguoiDung)
                .FirstOrDefaultAsync(p => p.ID == id);

            if (phanAnh == null) return NotFound();

            phanAnh.PhanHoi = phanHoi;
            phanAnh.TrangThai = TrangThaiPhanAnh.HoanThanh;

            _context.Update(phanAnh);
            await _context.SaveChangesAsync();

            var userEmail = phanAnh.NguoiDung.Email;

            if (string.IsNullOrEmpty(userEmail))
            {
                _logger.LogError($"Không tìm thấy email của cư dân có ID {phanAnh.ID_NguoiDung}");
                return RedirectToAction(nameof(Index));
            }

            var subject = "Thông báo: Phản hồi từ Ban Quản lý về phản ánh của bạn";

            var body = $@"
                <p>Xin chào <strong>{phanAnh.NguoiDung.HoTen}</strong>,</p>

                <p>Ban Quản lý xin gửi phản hồi về phản ánh mà bạn đã gửi trước đó. Dưới đây là nội dung chi tiết:</p>

                <table style='border-collapse: collapse; width: 100%;'>
                    <tr>
                        <td style='border: 1px solid #ddd; padding: 8px;'><strong>Ngày gửi phản ánh:</strong></td>
                        <td style='border: 1px solid #ddd; padding: 8px;'>{phanAnh.NgayGui:HH:mm dd/MM/yyyy}</td>
                    </tr>
                    <tr>
                        <td style='border: 1px solid #ddd; padding: 8px;'><strong>Nội dung phản ánh:</strong></td>
                        <td style='border: 1px solid #ddd; padding: 8px;'>{phanAnh.NoiDung}</td>
                    </tr>
                    <tr>
                        <td style='border: 1px solid #ddd; padding: 8px;'><strong>Phản hồi từ Ban Quản lý:</strong></td>
                        <td style='border: 1px solid #ddd; padding: 8px;'>{phanHoi}</td>
                    </tr>
                    <tr>
                        <td style='border: 1px solid #ddd; padding: 8px;'><strong>Trạng thái hiện tại:</strong></td>
                        <td style='border: 1px solid #ddd; padding: 8px;'>{phanAnh.TrangThai}</td>
                    </tr>
                </table>

                <p>Chúng tôi rất mong nhận được thêm ý kiến đóng góp từ bạn để nâng cao chất lượng dịch vụ.</p>

                <p>Trân trọng,<br/>Ban Quản lý</p>
            ";


            try
            {
                await _emailService.SendEmailAsync(userEmail, subject, body, "duongthanhphong1618@gmail.com");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Email sending failed: {ex.Message}");
            }

            return RedirectToAction(nameof(Index));
        }


    }
}
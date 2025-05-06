using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QLCCCC.Data;
using QLCCCC.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QLCCCC.Controllers
{
    public class HoaDonDichVuController : Controller
    {
        private readonly UserManager<NguoiDung> _userManager;
        private readonly ApplicationDbContext _context;

        // Inject UserManager and DbContext
        public HoaDonDichVuController(UserManager<NguoiDung> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            IEnumerable<HoaDonDichVu> hoaDonDichVus;

            // Lấy thông tin cư dân hiện tại
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                // Nếu không tìm thấy người dùng, trả về một lỗi hoặc trang đăng nhập
                return RedirectToAction("Login", "Account");
            }

            // Lấy thông tin căn hộ của cư dân
            var cuDan = await _context.CuDans
                .Include(cd => cd.CanHo)  // Bao gồm thông tin căn hộ của cư dân
                .FirstOrDefaultAsync(cd => cd.ID_NguoiDung == currentUser.ID);

            // Kiểm tra nếu người dùng là "Cư dân"
            if (cuDan != null)
            {
                // Lọc hóa đơn theo căn hộ của cư dân đó
                hoaDonDichVus = await _context.HoaDonDichVus
                    .Include(h => h.CanHo)
                        .ThenInclude(c => c.ChungCu)
                    .Include(h => h.HoaDonDichVu_DichVus)
                        .ThenInclude(hdv => hdv.DichVu)
                    .Where(hd => hd.CanHo.ID == cuDan.ID_CanHo)  // Lọc theo căn hộ của cư dân
                    .AsNoTracking()
                    .ToListAsync();
            }
            // Kiểm tra nếu người dùng là "Ban quản lý"
            else if (User.IsInRole("Ban quản lý"))
            {
                // Nếu là Ban quản lý, không lọc dữ liệu, lấy tất cả hóa đơn
                hoaDonDichVus = await _context.HoaDonDichVus
                    .Include(h => h.CanHo)
                        .ThenInclude(c => c.ChungCu)
                    .Include(h => h.HoaDonDichVu_DichVus)
                        .ThenInclude(hdv => hdv.DichVu)
                    .AsNoTracking()
                    .ToListAsync();
            }
            else
            {
                // Nếu người dùng không phải là "Cư dân" hay "Ban quản lý", không hiển thị gì
                hoaDonDichVus = new List<HoaDonDichVu>();
            }

            return View(hoaDonDichVus);
        }


        [HttpGet]
        public async Task<IActionResult> GetCanHoByChungCu(int chungCuId)
        {
            if (chungCuId == 0)
            {
                return BadRequest(new { message = "Chung cư không hợp lệ." });
            }

            var canHos = await _context.CanHos
                .Where(c => c.ID_ChungCu == chungCuId)
                .Select(c => new
                {
                    id = c.ID,
                    maCan = c.MaCan
                })
                .ToListAsync();

            if (!canHos.Any())
            {
                return NotFound(new { message = "Không có căn hộ nào." });
            }

            return Json(canHos);
        }

        // GET: HoaDonDichVu/Create
        public IActionResult Create()
        {
            ViewBag.CanHos = new SelectList(_context.CanHos, "ID", "MaCan");
            ViewBag.ChungCus = new SelectList(_context.ChungCus, "ID", "Ten");
            ViewBag.DichVus = _context.DichVus.ToList();

            return View();
        }

        // POST: HoaDonDichVu/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(HoaDonDichVu model, List<int> selectedDichVu)
        {
            if (ModelState.IsValid)
            {
                model.SoTien = selectedDichVu != null
                    ? await _context.DichVus.Where(d => selectedDichVu.Contains(d.ID)).SumAsync(d => d.Gia)
                    : 0;

                _context.HoaDonDichVus.Add(model);
                await _context.SaveChangesAsync();

                if (selectedDichVu != null)
                {
                    var hoaDonDichVus = selectedDichVu.Select(dvId => new HoaDonDichVu_DichVu
                    {
                        ID_HoaDon = model.ID,
                        ID_DichVu = dvId
                    }).ToList();

                    _context.HoaDonDichVu_DichVus.AddRange(hoaDonDichVus);
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(nameof(Index));
            }

            ViewBag.CanHos = new SelectList(_context.CanHos, "ID", "MaCan", model.ID_CanHo);
            ViewBag.ChungCus = new SelectList(_context.ChungCus, "ID", "Ten", model.ID_ChungCu);
            ViewBag.DichVus = _context.DichVus.ToList();

            return View(model);
        }

        // GET: HoaDonDichVu/Detail/5
        public async Task<IActionResult> Detail(int? id)
        {
            if (id == null) return NotFound();

            var hoaDon = await _context.HoaDonDichVus
                .Include(h => h.CanHo)
                .Include(h => h.ChungCu)
                .Include(h => h.HoaDonDichVu_DichVus)
                .ThenInclude(hdv => hdv.DichVu)
                .FirstOrDefaultAsync(h => h.ID == id);

            if (hoaDon == null) return NotFound();

            return View(hoaDon);
        }

        // POST: HoaDonDichVu/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string TrangThai)
        {
            if (id <= 0 || string.IsNullOrWhiteSpace(TrangThai))
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ!" });
            }

            var hoaDon = await _context.HoaDonDichVus.FindAsync(id);
            if (hoaDon == null)
            {
                return Json(new { success = false, message = "Không tìm thấy hóa đơn!" });
            }

            hoaDon.TrangThai = TrangThai;
            _context.HoaDonDichVus.Update(hoaDon);
            await _context.SaveChangesAsync();

            return Json(new { success = true, newStatus = TrangThai, message = "Cập nhật trạng thái thành công!" });
        }

        // GET: HoaDonDichVu/Edit/5
     
        public async Task<IActionResult> GetGiaDichVu(List<int> selectedDichVu)
        {
            if (selectedDichVu == null || !selectedDichVu.Any())
            {
                return Json(0);
            }

            // Kiểm tra và tính toán tổng giá dịch vụ được chọn
            var totalPrice = await _context.DichVus
                .Where(d => selectedDichVu.Contains(d.ID))
                .SumAsync(d => d.Gia);

            return Json(totalPrice);
        }

        // GET: HoaDonDichVu/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var hoaDon = await _context.HoaDonDichVus
                .Include(h => h.CanHo)
                .Include(h => h.ChungCu)
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.ID == id);

            if (hoaDon == null) return NotFound();

            return View(hoaDon);
        }

        // POST: HoaDonDichVu/DeleteConfirmed/5
        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var hoaDon = await _context.HoaDonDichVus
                .Include(h => h.HoaDonDichVu_DichVus)
                .FirstOrDefaultAsync(h => h.ID == id);

            if (hoaDon != null)
            {
                _context.HoaDonDichVu_DichVus.RemoveRange(hoaDon.HoaDonDichVu_DichVus);
                _context.HoaDonDichVus.Remove(hoaDon);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }





        // GET: HoaDonDichVu/Duyet/5
public async Task<IActionResult> Duyet(int? id)
{
    if (id == null) return NotFound();

    var hoaDon = await _context.HoaDonDichVus
        .Include(h => h.CanHo)
        .Include(h => h.ChungCu)
        .FirstOrDefaultAsync(h => h.ID == id);

    if (hoaDon == null) return NotFound();

    return View(hoaDon);
}

// POST: HoaDonDichVu/Duyet/5
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Duyet(int id, string trangThai)
{
    if (string.IsNullOrWhiteSpace(trangThai))
    {
        ModelState.AddModelError("", "Trạng thái không hợp lệ.");
        return RedirectToAction(nameof(Index));
    }

    var hoaDon = await _context.HoaDonDichVus.FindAsync(id);
    if (hoaDon == null) return NotFound();

    hoaDon.TrangThai = trangThai;
    _context.Update(hoaDon);
    await _context.SaveChangesAsync();

    return RedirectToAction(nameof(Index));
}

    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Threading.Tasks;
using QLCCCC.Models;
using QLCCCC.Repositories.Interfaces;
using System.Collections.Generic;
using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using QLCCCC.Data;
using QLCCCC.ViewModels;

public class AuthController : Controller
{
    private readonly INguoiDungRepository _repository;
    private readonly ApplicationDbContext _context; // Khai báo biến _context

    // Constructor
    public AuthController(INguoiDungRepository repository, ApplicationDbContext context)
    {
        _repository = repository;
        _context = context; // Khởi tạo _context
    }

    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(NguoiDung model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (string.IsNullOrWhiteSpace(model.Email))
        {
            ModelState.AddModelError("Email", "Email không được để trống!");
            return View(model);
        }

        var existingUser = await _repository.GetByEmailAsync(model.Email);
        if (existingUser != null)
        {
            ModelState.AddModelError("Email", "Email đã tồn tại!");
            return View(model);
        }

        if (string.IsNullOrWhiteSpace(model.MatKhau))
        {
            ModelState.AddModelError("MatKhau", "Mật khẩu không được để trống!");
            return View(model);
        }

        model.LoaiNguoiDung = "Khách"; // Gán quyền mặc định là khách
        await _repository.AddAsync(model);

        return RedirectToAction("Login");
    }

    [HttpPost]
    public async Task<IActionResult> SetAdminRole(int userId)
    {
        var user = await _repository.GetByIdAsync(userId);
        if (user == null)
        {
            return NotFound("Người dùng không tồn tại.");
        }

        user.LoaiNguoiDung = "Ban quản lý";
        await _repository.UpdateAsync(user);

        return Ok("Đã gán quyền Ban quản lý thành công.");
    }

    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string email, string password)
    {
        var user = await _repository.GetByEmailAsync(email);
        if (user == null) // Debug if user isn’t found
        {
            System.Diagnostics.Debug.WriteLine($"No user found for email: {email}");
            ModelState.AddModelError("", "Email không tồn tại");
            return View();
        }
        if (user.MatKhau != password)
        {
            System.Diagnostics.Debug.WriteLine("Password mismatch");
            ModelState.AddModelError("", "Mật khẩu không đúng");
            return View();
        }

        var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, user.HoTen ?? "Người dùng"),
        new Claim("UserId", user.ID.ToString()),
        new Claim(ClaimTypes.Role, user.LoaiNguoiDung ?? "Khách"),
        new Claim(ClaimTypes.Email, user.Email)
    };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties { IsPersistent = true };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    public async Task<IActionResult> UserProfile()
    {
        string email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

        if (email == null)
        {
            return NotFound("Không tìm thấy thông tin tài khoản.");
        }

        var user = await _repository.GetByEmailAsync(email);
        if (user == null)
        {
            return NotFound("Người dùng không tồn tại.");
        }

        var viewModel = new UserProfileViewModel
        {
            NguoiDung = user
        };

        if (user.LoaiNguoiDung == "Cư dân")
        {
            var cuDan = await _context.CuDans
                .Include(c => c.CanHo)
                .Include(c => c.ChungCu)
                .FirstOrDefaultAsync(c => c.ID_NguoiDung == user.ID);

            if (cuDan != null)
            {
                viewModel.CuDan = cuDan;  // Gán thông tin Cư Dân cho ViewModel
            }
            else
            {
                // Log hoặc thông báo không tìm thấy thông tin Cư Dân
                ModelState.AddModelError("", "Không tìm thấy thông tin căn hộ hoặc chung cư.");
            }
        }


        return View(viewModel); // Truyền ViewModel vào view
    }

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }
}
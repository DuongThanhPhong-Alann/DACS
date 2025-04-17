using Microsoft.EntityFrameworkCore;
using QLCCCC.Models;

namespace QLCCCC.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<ChungCu> ChungCus { get; set; } = null!;
        public DbSet<CanHo> CanHos { get; set; } = null!;
        public DbSet<NguoiDung> NguoiDungs { get; set; } = null!;
        public DbSet<CuDan> CuDans { get; set; } = null!;
        public DbSet<DichVu> DichVus { get; set; } = null!;
        public DbSet<HoaDonDichVu> HoaDonDichVus { get; set; } = null!;
        public DbSet<HoaDonDichVu_DichVu> HoaDonDichVu_DichVus { get; set; } = null!;
        public DbSet<PhanAnh> PhanAnhs { get; set; } = null!;
        public DbSet<HinhAnhChungCu> HinhAnhChungCus { get; set; } = null!;
        public DbSet<HinhAnhCanHo> HinhAnhCanHos { get; set; } = null!;
        public DbSet<TinTuc> TinTucs { get; set; } = null!;
        public DbSet<HinhAnhDichVu> HinhAnhDichVus { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Cấu hình cho ChungCu
            modelBuilder.Entity<ChungCu>()
                .HasMany(c => c.CanHos)
                .WithOne(c => c.ChungCu)
                .HasForeignKey(c => c.ID_ChungCu)
                .OnDelete(DeleteBehavior.Cascade);

            // Cấu hình cho CanHo
            modelBuilder.Entity<CanHo>()
                .HasIndex(c => new { c.MaCan, c.ID_ChungCu })
                .IsUnique();

            modelBuilder.Entity<CanHo>()
                .Property(c => c.Gia)
                .HasPrecision(18, 2);

            modelBuilder.Entity<CanHo>()
                .ToTable(t => t.HasCheckConstraint("CK_CanHo_TrangThai",
                    "TrangThai IN (N'Đang bán', N'Đã bán', N'Cho thuê', N'Đã thuê')"));

            modelBuilder.Entity<CanHo>()
                .HasMany(c => c.HinhAnhCanHos)
                .WithOne(h => h.CanHo)
                .HasForeignKey(h => h.ID_CanHo)
                .OnDelete(DeleteBehavior.Cascade);

            // Cấu hình cho NguoiDung
            modelBuilder.Entity<NguoiDung>()
                .ToTable(t => t.HasCheckConstraint("CK_NguoiDung_LoaiNguoiDung",
                    "LoaiNguoiDung IN (N'Cư dân', N'Ban quản lý', N'Khách')"));

            // Cấu hình cho CuDan
            modelBuilder.Entity<CuDan>()
                .HasOne(c => c.NguoiDung)
                .WithMany()
                .HasForeignKey(c => c.ID_NguoiDung)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CuDan>()
                .HasOne(c => c.CanHo)
                .WithMany()
                .HasForeignKey(c => c.ID_CanHo)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CuDan>()
                .HasOne(c => c.ChungCu)
                .WithMany()
                .HasForeignKey(c => c.ID_ChungCu)
                .OnDelete(DeleteBehavior.NoAction);

            // Cấu hình cho DichVu
            modelBuilder.Entity<DichVu>()
                .Property(d => d.Gia)
                .HasPrecision(18, 2);

            // Cấu hình cho HoaDonDichVu
            modelBuilder.Entity<HoaDonDichVu>()
                .Property(h => h.SoTien)
                .HasPrecision(18, 2);

            modelBuilder.Entity<HoaDonDichVu>()
                .ToTable(t => t.HasCheckConstraint("CK_HoaDonDichVu_TrangThai",
                    "TrangThai IN (N'Chưa thanh toán', N'Đã thanh toán')"));

            modelBuilder.Entity<HoaDonDichVu>()
                .HasOne(h => h.CanHo)
                .WithMany()
                .HasForeignKey(h => h.ID_CanHo)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HoaDonDichVu>()
                .HasOne(h => h.ChungCu)
                .WithMany()
                .HasForeignKey(h => h.ID_ChungCu)
                .OnDelete(DeleteBehavior.NoAction);

            // Cấu hình cho HoaDonDichVu_DichVu
            modelBuilder.Entity<HoaDonDichVu_DichVu>()
                .HasKey(hd => new { hd.ID_HoaDon, hd.ID_DichVu });

            modelBuilder.Entity<HoaDonDichVu_DichVu>()
                .HasOne(hd => hd.HoaDonDichVu)
                .WithMany(h => h.HoaDonDichVu_DichVus)
                .HasForeignKey(hd => hd.ID_HoaDon)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HinhAnhDichVu>()
                .HasOne(h => h.DichVu)
                .WithMany(d => d.HinhAnhDichVus)
                .HasForeignKey(h => h.ID_DichVu)
                .OnDelete(DeleteBehavior.Cascade); // 🟢 Xóa dịch vụ sẽ xóa luôn hình ảnh

            modelBuilder.Entity<HoaDonDichVu_DichVu>()
                .HasOne(hd => hd.DichVu)
                .WithMany(d => d.HoaDonDichVu_DichVus)
                .HasForeignKey(hd => hd.ID_DichVu)
                .OnDelete(DeleteBehavior.Cascade);

            // Cấu hình cho PhanAnh
            modelBuilder.Entity<PhanAnh>()
                .HasOne(p => p.NguoiDung)
                .WithMany()
                .HasForeignKey(p => p.ID_NguoiDung)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PhanAnh>()
                .ToTable(t => t.HasCheckConstraint("CK_PhanAnh_TrangThai",
                    "TrangThai IN (0, 1, 2)")); // Sửa thành số để khớp với enum

            // Cấu hình cho HinhAnhChungCu
            modelBuilder.Entity<HinhAnhChungCu>()
                .HasOne(h => h.ChungCu)
                .WithMany(c => c.HinhAnhChungCus)
                .HasForeignKey(h => h.ID_ChungCu)
                .OnDelete(DeleteBehavior.Cascade);

            // Cấu hình cho HinhAnhCanHo
            modelBuilder.Entity<HinhAnhCanHo>()
                .HasKey(h => h.ID);

            modelBuilder.Entity<HinhAnhCanHo>()
                .HasOne(h => h.CanHo)
                .WithMany(c => c.HinhAnhCanHos)
                .HasForeignKey(h => h.ID_CanHo)
                .OnDelete(DeleteBehavior.Cascade);

            // Cấu hình cho TinTuc
            modelBuilder.Entity<TinTuc>()
                .HasMany(t => t.DanhSachPhanAnh)
                .WithOne()
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
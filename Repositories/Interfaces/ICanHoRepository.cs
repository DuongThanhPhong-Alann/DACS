using QLCCCC.Models;

namespace QLCCCC.Repositories.Interfaces
{
    public interface ICanHoRepository
    {
        Task<IEnumerable<CanHo>> GetAllAsync();
        Task<CanHo?> GetByIdAsync(int id);
        Task<CanHo?> GetByIdWithDetailsAsync(int id);
        Task<CanHo?> GetByMaCanAndChungCuAsync(string maCan, int idChungCu);
        Task AddAsync(CanHo canHo);
        Task UpdateAsync(CanHo canHo);
        Task DeleteAsync(int id);
        Task DeleteImagesAsync(ICollection<HinhAnhCanHo> hinhAnhs);
    }
}
using Microsoft.EntityFrameworkCore;
using ServiceManagementApi.Data;
using ServiceManagementApi.DTOs;
using ServiceManagementApi.Models;

namespace ServiceManagementApi.Services;

public interface ICategoryService
{
    Task<List<ServiceCategory>> GetAllAsync();
    Task<ServiceCategory?> GetByIdAsync(int id);
    Task<ServiceCategory> CreateAsync(CreateCategoryDto dto);
    Task<bool> UpdateAsync(UpdateCategoryDto dto);
    Task<bool> DeleteAsync(int id);
}

public class CategoryService : ICategoryService
{
    private readonly ApplicationDbContext _context;

    public CategoryService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ServiceCategory>> GetAllAsync() =>
        await _context.ServiceCategories.OrderBy(c => c.Name).ToListAsync();

    public async Task<ServiceCategory?> GetByIdAsync(int id) =>
        await _context.ServiceCategories.FindAsync(id);

    public async Task<ServiceCategory> CreateAsync(CreateCategoryDto dto)
    {
        var category = new ServiceCategory
        {
            Name = dto.Name,
            Description = dto.Description,
            BaseCharge = dto.BaseCharge,
            SlaHours = dto.SlaHours
        };
        _context.ServiceCategories.Add(category);
        await _context.SaveChangesAsync();
        return category;
    }

    public async Task<bool> UpdateAsync(UpdateCategoryDto dto)
    {
        var category = await _context.ServiceCategories.FindAsync(dto.Id);
        if (category == null) return false;

        category.Name = dto.Name;
        category.Description = dto.Description;
        category.BaseCharge = dto.BaseCharge;
        category.SlaHours = dto.SlaHours;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var category = await _context.ServiceCategories.FindAsync(id);
        if (category == null) return false;

        try
        {
            _context.ServiceCategories.Remove(category);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException ex)
        {
           
            throw new InvalidOperationException("Cannot delete category because it is linked to existing service requests.");
        }
    }
}

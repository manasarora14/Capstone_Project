using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ServiceManagementApi.Data;
using ServiceManagementApi.DTOs;
using ServiceManagementApi.Models;

namespace ServiceManagementApi.Services;

public interface IAdminService
{
    Task<List<object>> GetAllUsersWithRolesAsync();
    Task<IdentityResult> UpdateUserRoleAsync(string userId, string newRole);
    Task<IdentityResult> DeleteUserAsync(string userId);
}


public class AdminService : IAdminService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;


    // Change IdentityUser to ApplicationUser here:
    public AdminService(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    public async Task<List<object>> GetAllUsersWithRolesAsync()
    {
        var users = await _userManager.Users.ToListAsync();
        var userList = new List<object>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userList.Add(new
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Role = roles.FirstOrDefault() ?? "Customer"
            });
        }
        return userList;
    }

    public async Task<IdentityResult> UpdateUserRoleAsync(string userId, string newRole)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return IdentityResult.Failed(new IdentityError { Description = "User not found" });

       
        var currentRoles = await _userManager.GetRolesAsync(user);
        var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);

        if (!removeResult.Succeeded) return removeResult;

        
        return await _userManager.AddToRoleAsync(user, newRole);
    }

    public async Task<IdentityResult> DeleteUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return IdentityResult.Failed(new IdentityError { Description = "User not found." });

        // EXPLICIT CHECK: Look for linked data that blocks deletion
        // Check if user is a Customer in any request OR a Technician in any request
        bool hasRelatedRecords = await _context.ServiceRequests
            .AnyAsync(r => r.CustomerId == userId || r.TechnicianId == userId);

        if (hasRelatedRecords)
        {
            return IdentityResult.Failed(new IdentityError
            {
                Description = "User cannot be deleted because they are associated with existing service requests or ongoing work."
            });
        }

        try
        {
            return await _userManager.DeleteAsync(user);
        }
        catch (Exception ex)
        {
            // Log ex here
            return IdentityResult.Failed(new IdentityError
            {
                Description = "A database error occurred while trying to delete the user."
            });
        }
    }
}
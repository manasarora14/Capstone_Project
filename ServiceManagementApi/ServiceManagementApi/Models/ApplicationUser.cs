using Microsoft.AspNetCore.Identity;

namespace ServiceManagementApi.Models;

public class ApplicationUser : Microsoft.AspNetCore.Identity.IdentityUser
{
    public string FullName { get; set; } = string.Empty;
}
using Microsoft.EntityFrameworkCore;
using ServiceManagementApi.Data;
using ServiceManagementApi.DTOs;
using ServiceManagementApi.Models;
using ServiceManagementApi.Services;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ServiceManagementApi.Tests.Services;

public class CategoryServiceTests
{
    private ApplicationDbContext CreateContext(string dbName)
    {
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ApplicationDbContext(opts);
    }

    [Fact]
    public async Task CreateAsync_AddsCategory()
    {
        var ctx = CreateContext(nameof(CreateAsync_AddsCategory));
        var svc = new CategoryService(ctx);

        var dto = new CreateCategoryDto { Name = "Install", Description = "desc", BaseCharge = 100, SlaHours = 4 };
        var created = await svc.CreateAsync(dto);

        Assert.Equal("Install", created.Name);
        Assert.Equal(1, await ctx.ServiceCategories.CountAsync());
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOrderedByName()
    {
        var ctx = CreateContext(nameof(GetAllAsync_ReturnsOrderedByName));
        ctx.ServiceCategories.AddRange(
            new ServiceCategory { Name = "Zed", BaseCharge = 1, SlaHours = 1 },
            new ServiceCategory { Name = "Alpha", BaseCharge = 1, SlaHours = 1 }
        );
        await ctx.SaveChangesAsync();
        var svc = new CategoryService(ctx);

        var all = await svc.GetAllAsync();

        Assert.Equal("Alpha", all.First().Name);
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsFalse_WhenNotFound()
    {
        var ctx = CreateContext(nameof(UpdateAsync_ReturnsFalse_WhenNotFound));
        var svc = new CategoryService(ctx);

        var result = await svc.UpdateAsync(new UpdateCategoryDto { Id = 99, Name = "X", Description = "D", BaseCharge = 10, SlaHours = 2 });

        Assert.False(result);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesValues()
    {
        var ctx = CreateContext(nameof(UpdateAsync_UpdatesValues));
        ctx.ServiceCategories.Add(new ServiceCategory { Id = 1, Name = "Old", Description = "D", BaseCharge = 10, SlaHours = 2 });
        await ctx.SaveChangesAsync();
        var svc = new CategoryService(ctx);

        var result = await svc.UpdateAsync(new UpdateCategoryDto { Id = 1, Name = "New", Description = "ND", BaseCharge = 20, SlaHours = 5 });

        Assert.True(result);
        var updated = await ctx.ServiceCategories.FindAsync(1);
        Assert.Equal("New", updated!.Name);
        Assert.Equal(5, updated.SlaHours);
    }

    [Fact]
    public async Task DeleteAsync_RemovesCategory()
    {
        var ctx = CreateContext(nameof(DeleteAsync_RemovesCategory));
        ctx.ServiceCategories.Add(new ServiceCategory { Id = 1, Name = "Del", BaseCharge = 5, SlaHours = 1 });
        await ctx.SaveChangesAsync();
        var svc = new CategoryService(ctx);

        var result = await svc.DeleteAsync(1);

        Assert.True(result);
        Assert.Empty(ctx.ServiceCategories);
    }
}


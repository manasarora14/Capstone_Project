using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceManagementApi.DTOs;
using ServiceManagementApi.Services;

namespace ServiceManagementApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin,Manager")]
public class CategoryController : ControllerBase
{
    private readonly ICategoryService _service;

    public CategoryController(ICategoryService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var cat = await _service.GetByIdAsync(id);
        if (cat == null) return NotFound();
        return Ok(cat);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCategoryDto dto)
    {
        var created = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCategoryDto dto)
    {
        if (id != dto.Id) return BadRequest();
        var ok = await _service.UpdateAsync(dto);
        return ok ? Ok() : NotFound();
    }

    
    [NonAction]
    public async Task<IActionResult> Update([FromBody] UpdateCategoryDto dto)
    {
        if (dto == null) return BadRequest();
        return await Update(dto.Id, dto);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var ok = await _service.DeleteAsync(id);
            return ok ? Ok() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

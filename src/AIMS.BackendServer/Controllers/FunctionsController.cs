using AIMS.BackendServer.Data;
using AIMS.BackendServer.Data.Entities;
using AIMS.ViewModels.Systems;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AIMS.BackendServer.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class FunctionsController : ControllerBase
{
    private readonly AimsDbContext _context;
    private readonly IMapper _mapper;

    public FunctionsController(AimsDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    // ─────────────────────────────────────────────────────────
    // GET /api/functions
    // Trả về cây menu (có Children lồng nhau)
    // ─────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var functions = await _context.Functions
            .Include(f => f.Children)
            .OrderBy(f => f.SortOrder)
            .ToListAsync();

        // Chỉ lấy root functions (không có ParentId)
        var roots = functions
            .Where(f => f.ParentId == null)
            .Select(f => MapToTreeVm(f, functions))
            .ToList();

        return Ok(roots);
    }

    // ─────────────────────────────────────────────────────────
    // GET /api/functions/{id}
    // ─────────────────────────────────────────────────────────
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var function = await _context.Functions
            .Include(f => f.Children)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (function == null)
            return NotFound(new { message = $"Function '{id}' không tồn tại." });

        return Ok(_mapper.Map<FunctionVm>(function));
    }

    // ─────────────────────────────────────────────────────────
    // POST /api/functions
    // ─────────────────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFunctionRequest request)
    {
        // Kiểm tra Id trùng
        if (await _context.Functions.AnyAsync(f => f.Id == request.Id.ToUpper()))
            return BadRequest(new { message = $"Function '{request.Id}' đã tồn tại." });

        // Kiểm tra ParentId hợp lệ
        if (!string.IsNullOrEmpty(request.ParentId))
        {
            var parentExists = await _context.Functions
                .AnyAsync(f => f.Id == request.ParentId);
            if (!parentExists)
                return BadRequest(new { message = $"ParentId '{request.ParentId}' không tồn tại." });
        }

        var function = _mapper.Map<Function>(request);
        function.Id = request.Id.ToUpper();

        _context.Functions.Add(function);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById),
            new { id = function.Id },
            _mapper.Map<FunctionVm>(function));
    }

    // ─────────────────────────────────────────────────────────
    // PUT /api/functions/{id}
    // ─────────────────────────────────────────────────────────
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        string id, [FromBody] UpdateFunctionRequest request)
    {
        var function = await _context.Functions.FindAsync(id);
        if (function == null)
            return NotFound(new { message = $"Function '{id}' không tồn tại." });

        function.Name = request.Name;
        function.Url = request.Url;
        function.Icon = request.Icon;
        function.SortOrder = request.SortOrder;
        function.ParentId = request.ParentId;

        await _context.SaveChangesAsync();
        return Ok(_mapper.Map<FunctionVm>(function));
    }

    // ─────────────────────────────────────────────────────────
    // DELETE /api/functions/{id}
    // ─────────────────────────────────────────────────────────
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var function = await _context.Functions
            .Include(f => f.Children)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (function == null)
            return NotFound(new { message = $"Function '{id}' không tồn tại." });

        // Không xóa nếu còn function con
        if (function.Children.Any())
            return BadRequest(new
            {
                message = $"Function '{id}' còn {function.Children.Count} function con. Xóa con trước."
            });

        // Xóa CommandInFunctions liên quan
        var cifs = _context.CommandInFunctions
            .Where(c => c.FunctionId == id);
        _context.CommandInFunctions.RemoveRange(cifs);

        // Xóa Permissions liên quan
        var perms = _context.Permissions
            .Where(p => p.FunctionId == id);
        _context.Permissions.RemoveRange(perms);

        _context.Functions.Remove(function);
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Đã xóa function '{id}'." });
    }

    // ─────────────────────────────────────────────────────────
    // GET /api/functions/{id}/commands
    // Lấy danh sách Commands của 1 Function
    // ─────────────────────────────────────────────────────────
    [HttpGet("{id}/commands")]
    public async Task<IActionResult> GetCommands(string id)
    {
        var functionExists = await _context.Functions.AnyAsync(f => f.Id == id);
        if (!functionExists)
            return NotFound(new { message = $"Function '{id}' không tồn tại." });

        var commands = await _context.CommandInFunctions
            .Where(cif => cif.FunctionId == id)
            .Include(cif => cif.Command)
            .Select(cif => new CommandVm
            {
                Id = cif.Command.Id,
                Name = cif.Command.Name,
            })
            .ToListAsync();

        return Ok(commands);
    }

    // ─────────────────────────────────────────────────────────
    // POST /api/functions/{id}/commands
    // Gán Commands vào Function
    // ─────────────────────────────────────────────────────────
    [HttpPost("{id}/commands")]
    public async Task<IActionResult> AddCommands(
        string id, [FromBody] AddCommandToFunctionRequest request)
    {
        var functionExists = await _context.Functions.AnyAsync(f => f.Id == id);
        if (!functionExists)
            return NotFound(new { message = $"Function '{id}' không tồn tại." });

        foreach (var commandId in request.CommandIds)
        {
            // Kiểm tra Command tồn tại
            var commandExists = await _context.Commands
                .AnyAsync(c => c.Id == commandId);
            if (!commandExists) continue;

            // Kiểm tra đã có chưa
            var alreadyExists = await _context.CommandInFunctions
                .AnyAsync(cif =>
                    cif.FunctionId == id && cif.CommandId == commandId);
            if (alreadyExists) continue;

            _context.CommandInFunctions.Add(new CommandInFunction
            {
                FunctionId = id,
                CommandId = commandId,
            });
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = $"Đã gán commands vào function '{id}'." });
    }

    // ─────────────────────────────────────────────────────────
    // DELETE /api/functions/{functionId}/commands/{commandId}
    // Gỡ Command khỏi Function
    // ─────────────────────────────────────────────────────────
    [HttpDelete("{functionId}/commands/{commandId}")]
    public async Task<IActionResult> RemoveCommand(
        string functionId, string commandId)
    {
        var cif = await _context.CommandInFunctions
            .FirstOrDefaultAsync(c =>
                c.FunctionId == functionId && c.CommandId == commandId);

        if (cif == null)
            return NotFound(new { message = "Không tìm thấy liên kết." });

        _context.CommandInFunctions.Remove(cif);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Đã gỡ command khỏi function." });
    }

    // ─────────────────────────────────────────────────────────
    // Helper: Map Function thành cây FunctionVm đệ quy
    // ─────────────────────────────────────────────────────────
    private FunctionVm MapToTreeVm(
        Function node, List<Function> allFunctions)
    {
        var vm = _mapper.Map<FunctionVm>(node);
        vm.Children = allFunctions
            .Where(f => f.ParentId == node.Id)
            .OrderBy(f => f.SortOrder)
            .Select(f => MapToTreeVm(f, allFunctions))
            .ToList();
        return vm;
    }
}
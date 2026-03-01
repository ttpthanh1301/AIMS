using AIMS.BackendServer.Data;
using AIMS.ViewModels.Systems;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AIMS.BackendServer.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class CommandsController : ControllerBase
{
    private readonly AimsDbContext _context;
    private readonly IMapper _mapper;

    public CommandsController(AimsDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    // GET /api/commands — Lấy tất cả commands
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var commands = await _context.Commands
            .OrderBy(c => c.Id)
            .ToListAsync();

        return Ok(_mapper.Map<List<CommandVm>>(commands));
    }
}

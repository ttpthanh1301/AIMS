using AIMS.BackendServer.Controllers;
using AIMS.BackendServer.Data.Entities;
using AIMS.BackendServer.UnitTests.Helpers;   // ← Thêm using này
using AIMS.ViewModels.Systems;
using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AIMS.BackendServer.UnitTests;

public class RolesControllerTests
{
    private readonly Mock<RoleManager<AppRole>> _roleManagerMock;
    private readonly IMapper _mapper;
    private readonly RolesController _controller;

    public RolesControllerTests()
    {
        var roleStore = new Mock<IRoleStore<AppRole>>();
        _roleManagerMock = new Mock<RoleManager<AppRole>>(
            roleStore.Object, null!, null!, null!, null!);

        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<AppRole, RoleVm>();
            cfg.CreateMap<CreateRoleRequest, AppRole>();
            cfg.CreateMap<UpdateRoleRequest, AppRole>();
        });
        _mapper = config.CreateMapper();

        _controller = new RolesController(_roleManagerMock.Object, _mapper);
    }

    [Fact]
    public async Task GetAll_ReturnsListOfRoles()
    {
        // Arrange
        var roles = new List<AppRole>
        {
            new AppRole { Id = "admin",  Name = "Admin"  },
            new AppRole { Id = "hr",     Name = "HR"     },
            new AppRole { Id = "mentor", Name = "Mentor" },
            new AppRole { Id = "intern", Name = "Intern" },
        };

        // ⭐ Dùng AsyncQueryable thay vì AsQueryable
        _roleManagerMock
            .Setup(m => m.Roles)
            .Returns(new AsyncQueryable<AppRole>(roles));

        // Act
        var result = await _controller.GetAll();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var list = ((OkObjectResult)result).Value as List<RoleVm>;
        list.Should().HaveCount(4);
    }

    [Fact]
    public async Task GetById_ExistingRole_ReturnsOk()
    {
        var role = new AppRole { Id = "admin", Name = "Admin" };
        _roleManagerMock
            .Setup(m => m.FindByIdAsync("admin"))
            .ReturnsAsync(role);

        var result = await _controller.GetById("admin");

        result.Should().BeOfType<OkObjectResult>();
        var vm = ((OkObjectResult)result).Value as RoleVm;
        vm!.Name.Should().Be("Admin");
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        _roleManagerMock
            .Setup(m => m.FindByIdAsync("nonexist"))
            .ReturnsAsync((AppRole?)null);

        var result = await _controller.GetById("nonexist");

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_ValidRequest_ReturnsCreated()
    {
        _roleManagerMock
            .Setup(m => m.RoleExistsAsync("Trainer"))
            .ReturnsAsync(false);

        _roleManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<AppRole>()))
            .ReturnsAsync(IdentityResult.Success);

        var request = new CreateRoleRequest
        {
            Id = "TRAINER",
            Name = "Trainer",
        };

        var result = await _controller.Create(request);

        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task Create_DuplicateRole_ReturnsBadRequest()
    {
        _roleManagerMock
            .Setup(m => m.RoleExistsAsync("Admin"))
            .ReturnsAsync(true);

        var request = new CreateRoleRequest
        {
            Id = "ADMIN",
            Name = "Admin",
        };

        var result = await _controller.Create(request);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Delete_SystemRole_ReturnsBadRequest()
    {
        var role = new AppRole { Id = "admin", Name = "Admin" };
        _roleManagerMock
            .Setup(m => m.FindByIdAsync("admin"))
            .ReturnsAsync(role);

        var result = await _controller.Delete("admin");

        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
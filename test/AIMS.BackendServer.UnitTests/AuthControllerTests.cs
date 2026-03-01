using AIMS.BackendServer.Controllers;
using AIMS.BackendServer.Data.Entities;
using AIMS.BackendServer.Services;
using AIMS.BackendServer.Settings;
using AIMS.ViewModels.Auth;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AIMS.BackendServer.UnitTests;

public class AuthControllerTests
{
    private readonly Mock<UserManager<AppUser>> _userManagerMock;
    private readonly Mock<SignInManager<AppUser>> _signInManagerMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly JwtSettings _jwtSettings;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        // Setup UserManager mock
        var userStore = new Mock<IUserStore<AppUser>>();
        _userManagerMock = new Mock<UserManager<AppUser>>(
            userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        // Setup SignInManager mock
        var contextAccessor = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<AppUser>>();
        _signInManagerMock = new Mock<SignInManager<AppUser>>(
            _userManagerMock.Object, contextAccessor.Object,
            claimsFactory.Object, null!, null!, null!, null!);

        _tokenServiceMock = new Mock<ITokenService>();

        _jwtSettings = new JwtSettings
        {
            Key = "AIMS_TEST_KEY_VERY_LONG_MIN_32_CHARS",
            Issuer = "AIMS.Test",
            Audience = "AIMS.Test",
            ExpiryMinutes = 60,
        };

        _controller = new AuthController(
            _userManagerMock.Object,
            _signInManagerMock.Object,
            _tokenServiceMock.Object,
            _jwtSettings);
    }

    // ─────────────────────────────────────────────────────────
    // TEST: Login thành công
    // ─────────────────────────────────────────────────────────
    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkWithToken()
    {
        // Arrange
        var user = new AppUser
        {
            Id = "user-001",
            Email = "admin@deha.vn",
            FirstName = "System",
            LastName = "Admin",
            IsActive = true,
        };

        _userManagerMock
            .Setup(m => m.FindByEmailAsync("admin@deha.vn"))
            .ReturnsAsync(user);

        _signInManagerMock
            .Setup(m => m.CheckPasswordSignInAsync(user, "Admin@2025!", false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        _userManagerMock
            .Setup(m => m.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Admin" });

        _tokenServiceMock
            .Setup(m => m.GenerateAccessTokenAsync(user))
            .ReturnsAsync("fake-jwt-token-xxx");

        var request = new LoginRequest
        {
            Email = "admin@deha.vn",
            Password = "Admin@2025!",
        };

        // Act
        var result = await _controller.Login(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();

        var ok = (OkObjectResult)result;
        var response = ok.Value as AuthResponse;
        response.Should().NotBeNull();
        response!.AccessToken.Should().Be("fake-jwt-token-xxx");
        response.Roles.Should().Contain("Admin");
    }

    // ─────────────────────────────────────────────────────────
    // TEST: Login thất bại — user không tồn tại
    // ─────────────────────────────────────────────────────────
    [Fact]
    public async Task Login_UserNotFound_ReturnsUnauthorized()
    {
        // Arrange
        _userManagerMock
            .Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((AppUser?)null);

        var request = new LoginRequest
        {
            Email = "notexist@deha.vn",
            Password = "Wrong@123!",
        };

        // Act
        var result = await _controller.Login(request);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    // ─────────────────────────────────────────────────────────
    // TEST: Login thất bại — sai mật khẩu
    // ─────────────────────────────────────────────────────────
    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        // Arrange
        var user = new AppUser
        {
            Id = "user-001",
            Email = "admin@deha.vn",
            IsActive = true,
        };

        _userManagerMock
            .Setup(m => m.FindByEmailAsync("admin@deha.vn"))
            .ReturnsAsync(user);

        _signInManagerMock
            .Setup(m => m.CheckPasswordSignInAsync(
                user, "WrongPassword!", false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

        var request = new LoginRequest
        {
            Email = "admin@deha.vn",
            Password = "WrongPassword!",
        };

        // Act
        var result = await _controller.Login(request);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    // ─────────────────────────────────────────────────────────
    // TEST: Login thất bại — tài khoản bị khóa
    // ─────────────────────────────────────────────────────────
    [Fact]
    public async Task Login_InactiveUser_ReturnsUnauthorized()
    {
        // Arrange
        var user = new AppUser
        {
            Id = "user-002",
            Email = "locked@deha.vn",
            IsActive = false,  // Tài khoản bị khóa
        };

        _userManagerMock
            .Setup(m => m.FindByEmailAsync("locked@deha.vn"))
            .ReturnsAsync(user);

        var request = new LoginRequest
        {
            Email = "locked@deha.vn",
            Password = "Any@2025!",
        };

        // Act
        var result = await _controller.Login(request);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }
}
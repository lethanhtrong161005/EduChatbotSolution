using Business.Services;
using Domain.Contracts;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;

namespace UnitTests;

/// <summary>
/// Happy-case unit tests for <see cref="UserManagementService"/>.
/// All external dependencies (UserManager, RoleManager, IEmailService,
/// IEmailVerificationService, IConfiguration) are mocked with Moq.
/// </summary>
[TestFixture]
public class UserManagementServiceTests
{
    private Mock<UserManager<ApplicationUser>> _userManagerMock = null!;
    private Mock<RoleManager<IdentityRole<Guid>>> _roleManagerMock = null!;
    private Mock<IEmailService> _emailServiceMock = null!;
    private Mock<IEmailVerificationService> _emailVerificationServiceMock = null!;
    private Mock<IConfiguration> _configMock = null!;

    private UserManagementService _sut = null!;

    // ── Test Fixtures ─────────────────────────────────────────────

    private static ApplicationUser MakeUser(string name = "Alice Smith",
        string email = "alice@example.com", bool isActive = true, DateTimeOffset? updatedAt = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            FullName = name,
            Email = email,
            UserName = email,
            IsActive = isActive,
            UpdatedAt = updatedAt ?? DateTimeOffset.UtcNow,
            DeletedAt = null,
        };

    // ── Setup ─────────────────────────────────────────────────────

    [SetUp]
    public void SetUp()
    {
        // UserManager requires several ctor args — we use the standard Moq pattern
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object,
            null!, null!, null!, null!, null!, null!, null!, null!);

        // RoleManager
        var roleStoreMock = new Mock<IRoleStore<IdentityRole<Guid>>>();
        _roleManagerMock = new Mock<RoleManager<IdentityRole<Guid>>>(
            roleStoreMock.Object,
            null!, null!, null!, null!);

        _emailServiceMock = new Mock<IEmailService>();
        _emailVerificationServiceMock = new Mock<IEmailVerificationService>();

        _configMock = new Mock<IConfiguration>();
        _configMock.Setup(c => c["Email:SenderEmail"]).Returns("noreply@educhatai.com");

        _sut = new UserManagementService(
            _userManagerMock.Object,
            _roleManagerMock.Object,
            _emailServiceMock.Object,
            _emailVerificationServiceMock.Object,
            _configMock.Object);
    }

    // ── CREATE ────────────────────────────────────────────────────

    /// <summary>
    /// Happy case: CreateUserAsync — new unique email, valid role →
    /// delegates to InitiateAdminVerificationAsync and returns success.
    /// </summary>
    [Test]
    public async Task CreateUser_HappyCase_ReturnsSuccess()
    {
        // Arrange
        var dto = new CreateUserDto("Bob Jones", "bob@example.com", "P@ssword1", "Student");

        _userManagerMock
            .Setup(m => m.FindByEmailAsync("bob@example.com"))
            .ReturnsAsync((ApplicationUser?)null);           // email not taken

        _userManagerMock
            .Setup(m => m.FindByNameAsync("bob@example.com"))
            .ReturnsAsync((ApplicationUser?)null);

        _roleManagerMock
            .Setup(m => m.RoleExistsAsync("Student"))
            .ReturnsAsync(true);

        _userManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock
            .Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Student"))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock
            .Setup(m => m.AddClaimsAsync(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<System.Security.Claims.Claim>>()))
            .ReturnsAsync(IdentityResult.Success);

        _emailVerificationServiceMock
            .Setup(m => m.InitiateEmailVerificationForExistingUserAsync("bob@example.com", "Bob Jones"))
            .ReturnsAsync((true, (string?)null));

        // Act
        var (success, error) = await _sut.CreateUserAsync(dto);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(error, Is.Null);
        });

        _emailVerificationServiceMock.Verify(
            m => m.InitiateEmailVerificationForExistingUserAsync("bob@example.com", "Bob Jones"),
            Times.Once);
    }

    /// <summary>
    /// CreateUserAsync — duplicate email → returns error without triggering email flow.
    /// </summary>
    [Test]
    public async Task CreateUser_DuplicateEmail_ReturnsError()
    {
        var dto = new CreateUserDto("Bob Jones", "dup@example.com", "P@ssword1", "Student");

        _userManagerMock
            .Setup(m => m.FindByEmailAsync("dup@example.com"))
            .ReturnsAsync(MakeUser(email: "dup@example.com"));   // already exists

        var (success, error) = await _sut.CreateUserAsync(dto);

        Assert.That(success, Is.False);
        Assert.That(error, Does.Contain("already exists"));

        _emailVerificationServiceMock.VerifyNoOtherCalls();
    }

    // ── UPDATE ────────────────────────────────────────────────────

    /// <summary>
    /// Happy case: UpdateUserAsync — name + role change, same email →
    /// saves updated name/UpdatedAt, reassigns role, returns success.
    /// </summary>
    [Test]
    public async Task UpdateUser_HappyCase_ReturnsSuccess()
    {
        var user = MakeUser();
        var stamp = user.UpdatedAt;

        var dto = new UpdateUserDto(user.Id, "Alice Updated", user.Email!, "Lecturer", stamp);

        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(["Student"]);
        _userManagerMock.Setup(m => m.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.AddToRoleAsync(user, "Lecturer"))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.GetClaimsAsync(user)).ReturnsAsync([]);
        _userManagerMock.Setup(m => m.AddClaimAsync(user, It.IsAny<System.Security.Claims.Claim>()))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        var (success, error) = await _sut.UpdateUserAsync(dto);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(error, Is.Null);
            Assert.That(user.FullName, Is.EqualTo("Alice Updated"));
        });
    }

    /// <summary>
    /// UpdateUserAsync — stale UpdatedAt → returns concurrency conflict error.
    /// </summary>
    [Test]
    public async Task UpdateUser_StaleUpdatedAt_ReturnsConcurrencyError()
    {
        var user = MakeUser();
        var staleStamp = user.UpdatedAt.AddSeconds(-60); // stale

        var dto = new UpdateUserDto(user.Id, "Alice", user.Email!, "Student", staleStamp);

        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);

        var (success, error) = await _sut.UpdateUserAsync(dto);

        Assert.That(success, Is.False);
        Assert.That(error, Does.Contain("modified by another administrator"));
    }

    // ── SOFT DELETE ───────────────────────────────────────────────

    /// <summary>
    /// Happy case: SoftDeleteUserAsync — sets DeletedAt, sends deletion email, returns success.
    /// </summary>
    [Test]
    public async Task SoftDeleteUser_HappyCase_SetsDeletedAtAndSendsEmail()
    {
        var user = MakeUser();

        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        _emailServiceMock
            .Setup(m => m.SendAccountDeletedAsync(user.Email!, user.FullName, It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var (success, error) = await _sut.SoftDeleteUserAsync(user.Id);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(error, Is.Null);
            Assert.That(user.DeletedAt, Is.Not.Null);
        });

        _emailServiceMock.Verify(
            m => m.SendAccountDeletedAsync(user.Email!, user.FullName, It.IsAny<string>()),
            Times.Once);
    }

    // ── DISABLE ───────────────────────────────────────────────────

    /// <summary>
    /// Happy case: DisableUserAsync — matching UpdatedAt → sets IsActive=false, sends email.
    /// </summary>
    [Test]
    public async Task DisableUser_HappyCase_SetsIsActiveAndSendsEmail()
    {
        var user = MakeUser(isActive: true);
        var stamp = user.UpdatedAt;

        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        _emailServiceMock
            .Setup(m => m.SendAccountDisabledAsync(user.Email!, user.FullName, It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var (success, error) = await _sut.DisableUserAsync(user.Id, stamp);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(error, Is.Null);
            Assert.That(user.IsActive, Is.False);
        });

        _emailServiceMock.Verify(
            m => m.SendAccountDisabledAsync(user.Email!, user.FullName, It.IsAny<string>()),
            Times.Once);
    }

    /// <summary>
    /// DisableUserAsync — stale UpdatedAt → returns concurrency error without disabling.
    /// </summary>
    [Test]
    public async Task DisableUser_StaleUpdatedAt_ReturnsConcurrencyError()
    {
        var user = MakeUser(isActive: true);
        var stale = user.UpdatedAt.AddSeconds(-30);

        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);

        var (success, error) = await _sut.DisableUserAsync(user.Id, stale);

        Assert.That(success, Is.False);
        Assert.That(error, Does.Contain("modified by another administrator"));
        Assert.That(user.IsActive, Is.True); // NOT changed
    }

    // ── REACTIVATE ────────────────────────────────────────────────

    /// <summary>
    /// Happy case: ReactivateUserAsync — disabled account, matching stamp → IsActive=true.
    /// </summary>
    [Test]
    public async Task ReactivateUser_HappyCase_SetsIsActiveTrue()
    {
        var user = MakeUser(isActive: false);
        var stamp = user.UpdatedAt;

        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        var (success, error) = await _sut.ReactivateUserAsync(user.Id, stamp);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(error, Is.Null);
            Assert.That(user.IsActive, Is.True);
        });
    }
}

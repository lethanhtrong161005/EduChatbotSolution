using Business.Services;
using DataAccess.Repositories;
using DataAccess.UnitOfWork;
using Domain.Common;
using Domain.Contracts;
using Domain.Entities;
using Domain.Exceptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace UnitTests;

[TestFixture]
public class SubjectServiceTests
{
    private Mock<IUnitOfWork> _unitOfWorkMock = null!;
    private Mock<UserManager<ApplicationUser>> _userManagerMock = null!;
    
    private Mock<GenericRepository<Subject>> _subjectRepoMock = null!;
    private Mock<GenericRepository<Chapter>> _chapterRepoMock = null!;
    private Mock<GenericRepository<SubjectMembership>> _membershipRepoMock = null!;
    
    private SubjectService _sut = null!;

    private Mock<GenericRepository<T>> CreateMockRepo<T>() where T : class
    {
        var dbContextMock = new Mock<DbContext>();
        var dbSetMock = new Mock<DbSet<T>>();
        dbContextMock.Setup(c => c.Set<T>()).Returns(dbSetMock.Object);
        return new Mock<GenericRepository<T>>(dbContextMock.Object);
    }

    [SetUp]
    public void SetUp()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object,
            null!, null!, null!, null!, null!, null!, null!, null!);

        _subjectRepoMock = CreateMockRepo<Subject>();
        _chapterRepoMock = CreateMockRepo<Chapter>();
        _membershipRepoMock = CreateMockRepo<SubjectMembership>();

        _unitOfWorkMock.Setup(u => u.Subjects).Returns(_subjectRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Chapters).Returns(_chapterRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.SubjectMemberships).Returns(_membershipRepoMock.Object);

        _sut = new SubjectService(_unitOfWorkMock.Object, _userManagerMock.Object);
    }

    // Helper to mock users
    private static ApplicationUser MakeUser(string fullName, string email, bool isActive = true, DateTimeOffset? deletedAt = null)
    {
        return new ApplicationUser
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            Email = email,
            UserName = email,
            IsActive = isActive,
            DeletedAt = deletedAt
        };
    }

    // ── SUBJECTS CRUD TESTS ─────────────────────────────────────────

    [Test]
    public async Task GetPagedSubjectsAsync_HappyCase_ReturnsPaginatedSubjects()
    {
        // Arrange
        var subjects = new List<Subject>
        {
            new() { Id = Guid.NewGuid(), SubjectCode = "CS101", SubjectName = "Introduction to CS" },
            new() { Id = Guid.NewGuid(), SubjectCode = "SE101", SubjectName = "Software Engineering" }
        };
        var paginatedList = new PaginatedList<Subject>(subjects, 2, 10, 1);

        _subjectRepoMock.Setup(r => r.GetAsync(
            It.IsAny<string[]>(),
            It.IsAny<Expression<Func<Subject, bool>>>(),
            It.IsAny<Func<IQueryable<Subject>, IOrderedQueryable<Subject>>>(),
            It.IsAny<(int, int)>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedList);

        // Act
        var result = await _sut.GetPagedSubjectsAsync("CS", null, 10, 0);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result[0].SubjectCode, Is.EqualTo("CS101"));
        });
    }

    [Test]
    public async Task CreateSubjectAsync_ValidInput_CreatesAndSaves()
    {
        // Arrange
        _subjectRepoMock.Setup(r => r.GetAsync(
            It.IsAny<string[]>(),
            It.IsAny<Expression<Func<Subject, bool>>>(),
            It.IsAny<Func<IQueryable<Subject>, IOrderedQueryable<Subject>>>(),
            It.IsAny<(int, int)>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Subject>()); // No duplicates

        // Act
        var result = await _sut.CreateSubjectAsync("SE102", "Advanced SE", "Description");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.SubjectCode, Is.EqualTo("SE102"));
            Assert.That(result.SubjectName, Is.EqualTo("Advanced SE"));
        });

        _subjectRepoMock.Verify(r => r.InsertAsync(It.IsAny<Subject>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void CreateSubjectAsync_EmptyCode_ThrowsBadRequestException()
    {
        Assert.ThrowsAsync<BadRequestException>(() => _sut.CreateSubjectAsync("", "Test Subject", null));
    }

    [Test]
    public void CreateSubjectAsync_EmptyName_ThrowsBadRequestException()
    {
        Assert.ThrowsAsync<BadRequestException>(() => _sut.CreateSubjectAsync("CS101", "", null));
    }

    [Test]
    public void CreateSubjectAsync_DuplicateCode_ThrowsBadRequestException()
    {
        // Arrange
        var duplicateSubject = new Subject { SubjectCode = "CS101", SubjectName = "Existing" };
        _subjectRepoMock.Setup(r => r.GetAsync(
            It.IsAny<string[]>(),
            It.IsAny<Expression<Func<Subject, bool>>>(),
            It.IsAny<Func<IQueryable<Subject>, IOrderedQueryable<Subject>>>(),
            It.IsAny<(int, int)>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Subject> { duplicateSubject });

        // Act & Assert
        var ex = Assert.ThrowsAsync<BadRequestException>(() => _sut.CreateSubjectAsync("CS101", "New Subject", null));
        Assert.That(ex.Message, Does.Contain("already exists"));
    }

    [Test]
    public async Task UpdateSubjectAsync_ValidInput_UpdatesAndSaves()
    {
        // Arrange
        var subjectId = Guid.NewGuid();
        var existingSubject = new Subject { Id = subjectId, SubjectCode = "CS101", SubjectName = "Old Name" };

        _subjectRepoMock.Setup(r => r.GetByIdAsync(subjectId, It.IsAny<CancellationToken>())).ReturnsAsync(existingSubject);
        _subjectRepoMock.Setup(r => r.GetAsync(
            It.IsAny<string[]>(),
            It.IsAny<Expression<Func<Subject, bool>>>(),
            It.IsAny<Func<IQueryable<Subject>, IOrderedQueryable<Subject>>>(),
            It.IsAny<(int, int)>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Subject>()); // No duplicates

        // Act
        var result = await _sut.UpdateSubjectAsync(subjectId, "CS101-Updated", "New Name", "New Desc");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.SubjectCode, Is.EqualTo("CS101-Updated"));
            Assert.That(result.SubjectName, Is.EqualTo("New Name"));
            Assert.That(result.Description, Is.EqualTo("New Desc"));
        });

        _subjectRepoMock.Verify(r => r.Update(existingSubject), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void UpdateSubjectAsync_NonExistentSubject_ThrowsEntityNotFoundException()
    {
        var subjectId = Guid.NewGuid();
        _subjectRepoMock.Setup(r => r.GetByIdAsync(subjectId, It.IsAny<CancellationToken>())).ReturnsAsync((Subject?)null);

        Assert.ThrowsAsync<EntityNotFoundException>(() => _sut.UpdateSubjectAsync(subjectId, "CS101", "Name", null));
    }

    [Test]
    public async Task DeleteSubjectAsync_ExistingSubject_DeletesAndSaves()
    {
        // Arrange
        var subjectId = Guid.NewGuid();
        var existingSubject = new Subject { Id = subjectId, SubjectCode = "CS101", SubjectName = "Test" };

        _subjectRepoMock.Setup(r => r.GetByIdAsync(subjectId, It.IsAny<CancellationToken>())).ReturnsAsync(existingSubject);

        // Act
        await _sut.DeleteSubjectAsync(subjectId);

        // Assert
        _subjectRepoMock.Verify(r => r.Delete(existingSubject), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── CHAPTERS CRUD TESTS ─────────────────────────────────────────

    [Test]
    public async Task CreateChapterAsync_ValidInput_CreatesAndSaves()
    {
        // Arrange
        var subjectId = Guid.NewGuid();
        var subject = new Subject { Id = subjectId, SubjectCode = "CS101", SubjectName = "Intro" };
        
        _subjectRepoMock.Setup(r => r.GetByIdAsync(subjectId, It.IsAny<CancellationToken>())).ReturnsAsync(subject);

        // Act
        var result = await _sut.CreateChapterAsync(subjectId, "Chapter 1", 1);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.ChapterName, Is.EqualTo("Chapter 1"));
            Assert.That(result.ChapterNumber, Is.EqualTo(1));
            Assert.That(result.SubjectId, Is.EqualTo(subjectId));
        });

        _chapterRepoMock.Verify(r => r.InsertAsync(It.IsAny<Chapter>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void CreateChapterAsync_EmptyName_ThrowsBadRequestException()
    {
        Assert.ThrowsAsync<BadRequestException>(() => _sut.CreateChapterAsync(Guid.NewGuid(), "", 1));
    }

    // ── MEMBERSHIP ASSIGNMENT TESTS ──────────────────────────────────

    [Test]
    public async Task AssignMember_StudentToStudentRole_Succeeds()
    {
        // Arrange
        var subjectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var subject = new Subject { Id = subjectId, SubjectCode = "CS101", SubjectName = "Intro" };
        var user = MakeUser("Alice Student", "alice@student.com");

        _subjectRepoMock.Setup(r => r.GetByIdAsync(subjectId, It.IsAny<CancellationToken>())).ReturnsAsync(subject);
        _userManagerMock.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Student" });
        _membershipRepoMock.Setup(r => r.GetAsync(
            It.IsAny<string[]>(),
            It.IsAny<Expression<Func<SubjectMembership, bool>>>(),
            It.IsAny<Func<IQueryable<SubjectMembership>, IOrderedQueryable<SubjectMembership>>>(),
            It.IsAny<(int, int)>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SubjectMembership>()); // Not already a member

        // Act
        await _sut.AssignMemberAsync(subjectId, userId, MembershipRole.Student);

        // Assert
        _membershipRepoMock.Verify(r => r.InsertAsync(It.Is<SubjectMembership>(m => 
            m.SubjectId == subjectId && m.UserId == userId && m.Role == MembershipRole.Student), 
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void AssignMember_StudentToLecturerRole_ThrowsBadRequestException()
    {
        // Arrange
        var subjectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var subject = new Subject { Id = subjectId, SubjectCode = "CS101", SubjectName = "Intro" };
        var user = MakeUser("Alice Student", "alice@student.com");

        _subjectRepoMock.Setup(r => r.GetByIdAsync(subjectId, It.IsAny<CancellationToken>())).ReturnsAsync(subject);
        _userManagerMock.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Student" }); // User ONLY has Student role

        // Act & Assert
        var ex = Assert.ThrowsAsync<BadRequestException>(() => _sut.AssignMemberAsync(subjectId, userId, MembershipRole.Lecturer));
        Assert.That(ex.Message, Does.Contain("Only users with the Lecturer"));
    }

    [Test]
    public async Task AssignMember_LecturerToChiefRole_Succeeds()
    {
        // Arrange
        var subjectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var subject = new Subject { Id = subjectId, SubjectCode = "CS101", SubjectName = "Intro" };
        var user = MakeUser("Bob Prof", "bob@lecturer.com");

        _subjectRepoMock.Setup(r => r.GetByIdAsync(subjectId, It.IsAny<CancellationToken>())).ReturnsAsync(subject);
        _userManagerMock.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Lecturer" });
        
        // Setup: No existing Chief
        _membershipRepoMock.Setup(r => r.GetAsync(
            It.IsAny<string[]>(),
            It.IsAny<Expression<Func<SubjectMembership, bool>>>(),
            It.IsAny<Func<IQueryable<SubjectMembership>, IOrderedQueryable<SubjectMembership>>>(),
            It.IsAny<(int, int)>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SubjectMembership>()); // No membership found at all

        // Act
        await _sut.AssignMemberAsync(subjectId, userId, MembershipRole.Chief);

        // Assert
        _membershipRepoMock.Verify(r => r.InsertAsync(It.Is<SubjectMembership>(m => 
            m.SubjectId == subjectId && m.UserId == userId && m.Role == MembershipRole.Chief), 
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void AssignMember_ChiefDuplicate_ThrowsBadRequestException()
    {
        // Arrange
        var subjectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var subject = new Subject { Id = subjectId, SubjectCode = "CS101", SubjectName = "Intro" };
        var user = MakeUser("Bob Prof", "bob@lecturer.com");

        _subjectRepoMock.Setup(r => r.GetByIdAsync(subjectId, It.IsAny<CancellationToken>())).ReturnsAsync(subject);
        _userManagerMock.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Lecturer" });
        
        // Setup: Return another membership when queried for Role == Chief
        var existingChief = new SubjectMembership { SubjectId = subjectId, UserId = Guid.NewGuid(), Role = MembershipRole.Chief };
        
        _membershipRepoMock.SetupSequence(r => r.GetAsync(
            It.IsAny<string[]>(),
            It.IsAny<Expression<Func<SubjectMembership, bool>>>(),
            It.IsAny<Func<IQueryable<SubjectMembership>, IOrderedQueryable<SubjectMembership>>>(),
            It.IsAny<(int, int)>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SubjectMembership>()) // 1st call: check if user is already a member
            .ReturnsAsync(new List<SubjectMembership> { existingChief }); // 2nd call: check if chief exists

        // Act & Assert
        var ex = Assert.ThrowsAsync<BadRequestException>(() => _sut.AssignMemberAsync(subjectId, userId, MembershipRole.Chief));
        Assert.That(ex.Message, Does.Contain("already has a Subject-Lead"));
    }

    [Test]
    public void AssignMember_InactiveUser_ThrowsBadRequestException()
    {
        // Arrange
        var subjectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var subject = new Subject { Id = subjectId, SubjectCode = "CS101", SubjectName = "Intro" };
        var user = MakeUser("Bob Inactive", "bob@lecturer.com", isActive: false);

        _subjectRepoMock.Setup(r => r.GetByIdAsync(subjectId, It.IsAny<CancellationToken>())).ReturnsAsync(subject);
        _userManagerMock.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync(user);

        // Act & Assert
        var ex = Assert.ThrowsAsync<BadRequestException>(() => _sut.AssignMemberAsync(subjectId, userId, MembershipRole.Lecturer));
        Assert.That(ex.Message, Does.Contain("inactive or has been deleted"));
    }
}

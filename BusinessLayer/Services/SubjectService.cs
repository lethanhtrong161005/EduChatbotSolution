using DataAccess.UnitOfWork;
using Domain.Common;
using Domain.Contracts;
using Domain.Entities;
using Domain.Exceptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Business.Services;

/// <summary>
/// Service coordinating subject and chapter management along with student/lecturer assignments.
/// </summary>
public class SubjectService(
    IUnitOfWork unitOfWork,
    UserManager<ApplicationUser> userManager) : ISubjectService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    // ── Subjects CRUD ─────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<PaginatedList<Subject>> GetPagedSubjectsAsync(
        string? searchCode,
        string? searchName,
        int limit,
        int offset)
    {
        // 1. Build filter expressions dynamically
        System.Linq.Expressions.Expression<Func<Subject, bool>>? filter = null;

        if (!string.IsNullOrWhiteSpace(searchCode) && !string.IsNullOrWhiteSpace(searchName))
        {
            var code = searchCode.Trim().ToLower();
            var name = searchName.Trim().ToLower();
            filter = s => s.SubjectCode.ToLower().Contains(code) && s.SubjectName.ToLower().Contains(name);
        }
        else if (!string.IsNullOrWhiteSpace(searchCode))
        {
            var code = searchCode.Trim().ToLower();
            filter = s => s.SubjectCode.ToLower().Contains(code);
        }
        else if (!string.IsNullOrWhiteSpace(searchName))
        {
            var name = searchName.Trim().ToLower();
            filter = s => s.SubjectName.ToLower().Contains(name);
        }

        // 2. Setup pagination
        var pageSize = limit > 0 ? limit : 10;
        var pageIndex = (offset / pageSize) + 1;

        // 3. Query
        var paginatedResult = await _unitOfWork.Subjects.GetAsync(
            filter: filter,
            orderBy: q => q.OrderBy(s => s.SubjectCode),
            paginationSettings: (pageSize, pageIndex)
        );

        return (PaginatedList<Subject>)paginatedResult;
    }

    /// <inheritdoc/>
    public async Task<Subject?> GetSubjectByIdAsync(Guid id)
    {
        return await _unitOfWork.Subjects.GetByIdAsync(id);
    }

    /// <inheritdoc/>
    public async Task<Subject> CreateSubjectAsync(string subjectCode, string subjectName, string? description)
    {
        if (string.IsNullOrWhiteSpace(subjectCode))
            throw new BadRequestException("Subject code cannot be empty.");
        if (string.IsNullOrWhiteSpace(subjectName))
            throw new BadRequestException("Subject name cannot be empty.");

        // Check uniqueness of subjectCode
        var code = subjectCode.Trim();
        var existing = await _unitOfWork.Subjects.GetAsync(
            filter: s => s.SubjectCode.ToLower() == code.ToLower());
        
        if (existing.Any())
            throw new BadRequestException($"Subject code '{code}' already exists in the system.");

        var subject = new Subject
        {
            SubjectCode = code,
            SubjectName = subjectName.Trim(),
            Description = description?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Subjects.InsertAsync(subject);
        await _unitOfWork.SaveAsync();

        return subject;
    }

    /// <inheritdoc/>
    public async Task<Subject> UpdateSubjectAsync(Guid id, string subjectCode, string subjectName, string? description)
    {
        if (string.IsNullOrWhiteSpace(subjectCode))
            throw new BadRequestException("Subject code cannot be empty.");
        if (string.IsNullOrWhiteSpace(subjectName))
            throw new BadRequestException("Subject name cannot be empty.");

        var subject = await _unitOfWork.Subjects.GetByIdAsync(id)
            ?? throw new EntityNotFoundException(id);

        // Check uniqueness of subjectCode (excluding current subject)
        var code = subjectCode.Trim();
        var existing = await _unitOfWork.Subjects.GetAsync(
            filter: s => s.SubjectCode.ToLower() == code.ToLower() && s.Id != id);

        if (existing.Any())
            throw new BadRequestException($"Subject code '{code}' is already in use by another subject.");

        subject.SubjectCode = code;
        subject.SubjectName = subjectName.Trim();
        subject.Description = description?.Trim();
        subject.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Subjects.Update(subject);
        await _unitOfWork.SaveAsync();

        return subject;
    }

    /// <inheritdoc/>
    public async Task DeleteSubjectAsync(Guid id)
    {
        var subject = await _unitOfWork.Subjects.GetByIdAsync(id)
            ?? throw new EntityNotFoundException(id);

        _unitOfWork.Subjects.Delete(subject);
        await _unitOfWork.SaveAsync();
    }

    // ── Chapters CRUD ─────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<List<Chapter>> GetChaptersBySubjectIdAsync(Guid subjectId)
    {
        var chapters = await _unitOfWork.Chapters.GetAsync(
            filter: c => c.SubjectId == subjectId,
            orderBy: q => q.OrderBy(c => c.ChapterNumber).ThenBy(c => c.ChapterName)
        );
        return chapters.ToList();
    }

    /// <inheritdoc/>
    public async Task<Chapter?> GetChapterByIdAsync(Guid id)
    {
        return await _unitOfWork.Chapters.GetByIdAsync(id);
    }

    /// <inheritdoc/>
    public async Task<Chapter> CreateChapterAsync(Guid subjectId, string chapterName, int? chapterNumber)
    {
        if (string.IsNullOrWhiteSpace(chapterName))
            throw new BadRequestException("Chapter name cannot be empty.");

        _ = await _unitOfWork.Subjects.GetByIdAsync(subjectId)
            ?? throw new EntityNotFoundException(subjectId);

        var chapter = new Chapter
        {
            SubjectId = subjectId,
            ChapterName = chapterName.Trim(),
            ChapterNumber = chapterNumber,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Chapters.InsertAsync(chapter);
        await _unitOfWork.SaveAsync();

        return chapter;
    }

    /// <inheritdoc/>
    public async Task<Chapter> UpdateChapterAsync(Guid id, string chapterName, int? chapterNumber)
    {
        if (string.IsNullOrWhiteSpace(chapterName))
            throw new BadRequestException("Chapter name cannot be empty.");

        var chapter = await _unitOfWork.Chapters.GetByIdAsync(id)
            ?? throw new EntityNotFoundException(id);

        chapter.ChapterName = chapterName.Trim();
        chapter.ChapterNumber = chapterNumber;
        chapter.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Chapters.Update(chapter);
        await _unitOfWork.SaveAsync();

        return chapter;
    }

    /// <inheritdoc/>
    public async Task DeleteChapterAsync(Guid id)
    {
        var chapter = await _unitOfWork.Chapters.GetByIdAsync(id)
            ?? throw new EntityNotFoundException(id);

        _unitOfWork.Chapters.Delete(chapter);
        await _unitOfWork.SaveAsync();
    }

    // ── Memberships Management ────────────────────────────────

    /// <inheritdoc/>
    public async Task<List<SubjectMembership>> GetMembershipsBySubjectIdAsync(Guid subjectId)
    {
        var memberships = await _unitOfWork.SubjectMemberships.GetAsync(
            includeProperties: ["User"],
            filter: m => m.SubjectId == subjectId,
            orderBy: q => q.OrderBy(m => m.Role).ThenBy(m => m.User.FullName)
        );
        return memberships.ToList();
    }

    /// <inheritdoc/>
    public async Task AssignMemberAsync(Guid subjectId, Guid userId, MembershipRole role)
    {
        var subject = await _unitOfWork.Subjects.GetByIdAsync(subjectId)
            ?? throw new EntityNotFoundException(subjectId);

        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new EntityNotFoundException(userId);

        if (!user.IsActive || user.DeletedAt.HasValue)
            throw new BadRequestException("This user account is inactive or has been deleted.");

        // Verify system role matches the assignment role
        var systemRoles = await _userManager.GetRolesAsync(user);
        if (role == MembershipRole.Student)
        {
            if (!systemRoles.Contains("Student"))
                throw new BadRequestException("Only users with the Student system role can be assigned as a Student member.");
        }
        else if (role == MembershipRole.Lecturer || role == MembershipRole.Chief)
        {
            if (!systemRoles.Contains("Lecturer"))
                throw new BadRequestException("Only users with the Lecturer system role can be assigned as a Lecturer or Subject-Lead.");
        }

        // Check if user is already a member
        var existing = await _unitOfWork.SubjectMemberships.GetAsync(
            filter: m => m.SubjectId == subjectId && m.UserId == userId);

        var existingList = existing.ToList();
        if (existingList.Count > 0)
        {
            var membership = existingList[0];
            if (membership.Role == role)
                return; // Role is already matching, no changes needed.

            // Changing roles: Enforce Chief uniqueness if target role is Chief
            if (role == MembershipRole.Chief)
            {
                var currentChief = await _unitOfWork.SubjectMemberships.GetAsync(
                    filter: m => m.SubjectId == subjectId && m.Role == MembershipRole.Chief && m.UserId != userId);
                if (currentChief.Any())
                    throw new BadRequestException("This subject already has a Subject-Lead. Please remove the current Subject-Lead first.");
            }

            membership.Role = role;
            membership.AssignedAt = DateTime.UtcNow;
            _unitOfWork.SubjectMemberships.Update(membership);
        }
        else
        {
            // Adding new membership: Enforce Chief uniqueness if role is Chief
            if (role == MembershipRole.Chief)
            {
                var currentChief = await _unitOfWork.SubjectMemberships.GetAsync(
                    filter: m => m.SubjectId == subjectId && m.Role == MembershipRole.Chief);
                if (currentChief.Any())
                    throw new BadRequestException("This subject already has a Subject-Lead. Please remove the current Subject-Lead first.");
            }

            var newMembership = new SubjectMembership
            {
                SubjectId = subjectId,
                UserId = userId,
                Role = role,
                AssignedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.SubjectMemberships.InsertAsync(newMembership);
        }

        await _unitOfWork.SaveAsync();
    }

    /// <inheritdoc/>
    public async Task RemoveMemberAsync(Guid subjectId, Guid userId)
    {
        var existing = await _unitOfWork.SubjectMemberships.GetAsync(
            filter: m => m.SubjectId == subjectId && m.UserId == userId);
        
        var existingList = existing.ToList();
        if (existingList.Count > 0)
        {
            _unitOfWork.SubjectMemberships.Delete(existingList[0]);
            await _unitOfWork.SaveAsync();
        }
    }

    /// <inheritdoc/>
    public async Task<List<ApplicationUser>> GetEligibleUsersForAssignmentAsync(
        Guid subjectId,
        MembershipRole role,
        string? search)
    {
        // 1. Determine which system role is required
        var requiredSystemRole = role == MembershipRole.Student ? "Student" : "Lecturer";
        
        // 2. Fetch users in system role
        var usersInRole = await _userManager.GetUsersInRoleAsync(requiredSystemRole);

        // 3. Filter active, non-deleted, and matching the search query
        var query = usersInRole.Where(u => u.IsActive && !u.DeletedAt.HasValue);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var cleanSearch = search.Trim().ToLower();
            query = query.Where(u => u.FullName.ToLower().Contains(cleanSearch) || 
                                     (u.Email != null && u.Email.ToLower().Contains(cleanSearch)));
        }

        // 4. Exclude users who are already members of this subject
        var currentMembers = await _unitOfWork.SubjectMemberships.GetAsync(
            filter: m => m.SubjectId == subjectId);
        
        var assignedUserIds = currentMembers.Select(m => m.UserId).ToHashSet();

        return query.Where(u => !assignedUserIds.Contains(u.Id)).ToList();
    }
}

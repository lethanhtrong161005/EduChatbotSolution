using Domain.Common;
using Domain.Entities;

namespace Domain.Contracts;

/// <summary>
/// Defines the contract for subject administration, chapter CRUD, and member assignments.
/// </summary>
public interface ISubjectService
{
    // ── Subjects CRUD ─────────────────────────────────────────
    
    /// <summary>
    /// Returns a paginated list of subjects, optionally filtered by subject code or name.
    /// </summary>
    Task<PaginatedList<Subject>> GetPagedSubjectsAsync(
        string? searchCode,
        string? searchName,
        int limit,
        int offset);

    /// <summary>
    /// Gets a subject by its unique Guid identifier.
    /// </summary>
    Task<Subject?> GetSubjectByIdAsync(Guid id);

    /// <summary>
    /// Creates a new educational subject in the database.
    /// </summary>
    Task<Subject> CreateSubjectAsync(string subjectCode, string subjectName, string? description);

    /// <summary>
    /// Updates an existing subject's details.
    /// </summary>
    Task<Subject> UpdateSubjectAsync(Guid id, string subjectCode, string subjectName, string? description);

    /// <summary>
    /// Deletes a subject and cascades deletes all its chapters, documents, chunks, and memberships.
    /// </summary>
    Task DeleteSubjectAsync(Guid id);

    // ── Chapters CRUD ─────────────────────────────────────────

    /// <summary>
    /// Gets all chapters belonging to a specific subject, ordered by chapter number.
    /// </summary>
    Task<List<Chapter>> GetChaptersBySubjectIdAsync(Guid subjectId);

    /// <summary>
    /// Gets a chapter by its unique Guid identifier.
    /// </summary>
    Task<Chapter?> GetChapterByIdAsync(Guid id);

    /// <summary>
    /// Creates a new chapter inside a subject.
    /// </summary>
    Task<Chapter> CreateChapterAsync(Guid subjectId, string chapterName, int? chapterNumber);

    /// <summary>
    /// Updates an existing chapter.
    /// </summary>
    Task<Chapter> UpdateChapterAsync(Guid id, string chapterName, int? chapterNumber);

    /// <summary>
    /// Deletes a chapter.
    /// </summary>
    Task DeleteChapterAsync(Guid id);

    // ── Memberships Management ────────────────────────────────

    /// <summary>
    /// Gets all active memberships (students, lecturers, chiefs) for a subject.
    /// </summary>
    Task<List<SubjectMembership>> GetMembershipsBySubjectIdAsync(Guid subjectId);

    /// <summary>
    /// Assigns an active user to a subject with a specific membership role (Student, Lecturer, Chief).
    /// </summary>
    Task AssignMemberAsync(Guid subjectId, Guid userId, MembershipRole role);

    /// <summary>
    /// Removes a user from a subject's membership.
    /// </summary>
    Task RemoveMemberAsync(Guid subjectId, Guid userId);

    /// <summary>
    /// Returns a list of active users that are eligible to be assigned to the subject (e.g. not already members).
    /// </summary>
    Task<List<ApplicationUser>> GetEligibleUsersForAssignmentAsync(
        Guid subjectId,
        MembershipRole role,
        string? search);
}

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
    /// Gets a subject by its unique integer identifier.
    /// </summary>
    Task<Subject?> GetSubjectByIdAsync(int id);

    /// <summary>
    /// Creates a new educational subject in the database.
    /// </summary>
    Task<Subject> CreateSubjectAsync(string subjectCode, string subjectName, string? description);

    /// <summary>
    /// Updates an existing subject's details.
    /// </summary>
    Task<Subject> UpdateSubjectAsync(int id, string subjectCode, string subjectName, string? description);

    /// <summary>
    /// Deletes a subject and cascades deletes all its chapters, documents, chunks, and memberships.
    /// </summary>
    Task DeleteSubjectAsync(int id);

    // ── Chapters CRUD ─────────────────────────────────────────

    /// <summary>
    /// Gets all chapters belonging to a specific subject, ordered by chapter number.
    /// </summary>
    Task<List<Chapter>> GetChaptersBySubjectIdAsync(int subjectId);

    /// <summary>
    /// Gets a chapter by its unique Guid identifier.
    /// </summary>
    Task<Chapter?> GetChapterByIdAsync(int id);

    /// <summary>
    /// Creates a new chapter inside a subject.
    /// </summary>
    Task<Chapter> CreateChapterAsync(int subjectId, string chapterName, int? chapterNumber);

    /// <summary>
    /// Updates an existing chapter.
    /// </summary>
    Task<Chapter> UpdateChapterAsync(int id, string chapterName, int? chapterNumber);

    /// <summary>
    /// Deletes a chapter.
    /// </summary>
    Task DeleteChapterAsync(int id);

    // ── Memberships Management ────────────────────────────────

    /// <summary>
    /// Gets all active memberships (students, lecturers, chiefs) for a subject.
    /// </summary>
    Task<List<SubjectMembership>> GetMembershipsBySubjectIdAsync(int subjectId);

    /// <summary>
    /// Assigns an active user to a subject with a specific membership role (Student, Lecturer, Chief).
    /// </summary>
    Task AssignMemberAsync(int subjectId, Guid userId, MembershipRole role);

    /// <summary>
    /// Removes a user from a subject's membership.
    /// </summary>
    Task RemoveMemberAsync(int subjectId, Guid userId);

    /// <summary>
    /// Returns a list of active users that are eligible to be assigned to the subject (e.g. not already members).
    /// </summary>
    Task<List<ApplicationUser>> GetEligibleUsersForAssignmentAsync(
        int subjectId,
        MembershipRole role,
        string? search);

    // ==================================================
    Task<IEnumerable<Subject>> GetAccessibleSubjectsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> IsChiefAsync(int subjectId, Guid userId, CancellationToken cancellationToken = default);
}

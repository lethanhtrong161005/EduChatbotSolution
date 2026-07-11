using DataAccess.Data;
using DataAccess.Repositories;
using Domain.Entities;

namespace DataAccess.UnitOfWork;

/// <summary>
/// Implements the Unit of Work pattern, providing lazy-loaded repositories
/// for all aggregate roots and coordinating saves via a shared DbContext.
/// </summary>
public class UnitOfWork(EduChatAiDbContext context) : IUnitOfWork
{
    readonly EduChatAiDbContext _context = context;

    GenericRepository<ApplicationUser>? _users;
    GenericRepository<ApplicationRole>? _roles;
    GenericRepository<ApplicationUserRole>? _userRoles;
    GenericRepository<Plan>? _plans;
    GenericRepository<PlanOption>? _planOptions;
    GenericRepository<Order>? _orders;
    GenericRepository<Subscription>? _subscriptions;
    GenericRepository<Payment>? _payments;
    GenericRepository<Subject>? _subjects;
    GenericRepository<Membership>? _memberships;
    GenericRepository<SubjectStorageConfiguration>? _subjectStorageConfigurations;
    GenericRepository<SubjectAiConfiguration>? _subjectAiConfigurations;
    GenericRepository<GlobalAiConfiguration>? _globalAiConfigurations;
    GenericRepository<Chapter>? _chapters;
    GenericRepository<Document>? _documents;
    GenericRepository<DocumentChapter>? _documentChapters;
    GenericRepository<DocumentComment>? _documentComments;
    GenericRepository<ParsedSection>? _parsedSections;
    GenericRepository<Chunk>? _chunks;
    GenericRepository<ChatSession>? _chatSessions;
    GenericRepository<ChatSessionTitleGenerationSettings>? _chatSessionTitleGenerationSettings;
    GenericRepository<ChatSessionTitleGenerationMetrics>? _chatSessionTitleGenerationMetrics;
    GenericRepository<ChatMessage>? _chatMessages;
    GenericRepository<ChatMessageGenerationSettings>? _chatMessageGenerationSettings;
    GenericRepository<ChatMessageGenerationMetrics>? _chatMessageGenerationMetrics;
    GenericRepository<Citation>? _citations;
    GenericRepository<CitationOccurrence>? _citationOccurrences;
    GenericRepository<TestQuestion>? _testQuestions;
    GenericRepository<Experiment>? _experiments;
    GenericRepository<TestResponse>? _testResponses;

    /// <inheritdoc/>
    public GenericRepository<ApplicationUser> Users => _users ??= new GenericRepository<ApplicationUser>(_context);
    /// <inheritdoc/>
    public GenericRepository<ApplicationRole> Roles => _roles ??= new GenericRepository<ApplicationRole>(_context);
    /// <inheritdoc/>
    public GenericRepository<ApplicationUserRole> UserRoles => _userRoles ??= new GenericRepository<ApplicationUserRole>(_context);
    /// <inheritdoc/>
    public GenericRepository<Plan> Plans => _plans ??= new GenericRepository<Plan>(_context);
    /// <inheritdoc/>
    public GenericRepository<PlanOption> PlanOptions => _planOptions ??= new GenericRepository<PlanOption>(_context);
    /// <inheritdoc/>
    public GenericRepository<Order> Orders => _orders ??= new GenericRepository<Order>(_context);
    /// <inheritdoc/>
    public GenericRepository<Subscription> Subscriptions => _subscriptions ??= new GenericRepository<Subscription>(_context);
    /// <inheritdoc/>
    public GenericRepository<Payment> Payments => _payments ??= new GenericRepository<Payment>(_context);
    /// <inheritdoc/>
    public GenericRepository<Subject> Subjects => _subjects ??= new GenericRepository<Subject>(_context);
    /// <inheritdoc/>
    public GenericRepository<Membership> Memberships => _memberships ??= new GenericRepository<Membership>(_context);
    /// <inheritdoc/>
    public GenericRepository<SubjectStorageConfiguration> SubjectStorageConfigurations => _subjectStorageConfigurations ??= new GenericRepository<SubjectStorageConfiguration>(_context);
    /// <inheritdoc/>
    public GenericRepository<SubjectAiConfiguration> SubjectAiConfigurations => _subjectAiConfigurations ??= new GenericRepository<SubjectAiConfiguration>(_context);
    /// <inheritdoc/>
    public GenericRepository<GlobalAiConfiguration> GlobalAiConfigurations => _globalAiConfigurations ??= new GenericRepository<GlobalAiConfiguration>(_context);
    /// <inheritdoc/>
    public GenericRepository<Chapter> Chapters => _chapters ??= new GenericRepository<Chapter>(_context);
    /// <inheritdoc/>
    public GenericRepository<Document> Documents => _documents ??= new GenericRepository<Document>(_context);
    /// <inheritdoc/>
    public GenericRepository<DocumentChapter> DocumentChapters => _documentChapters ??= new GenericRepository<DocumentChapter>(_context);
    /// <inheritdoc/>
    public GenericRepository<DocumentComment> DocumentComments => _documentComments ??= new GenericRepository<DocumentComment>(_context);
    /// <inheritdoc/>
    public GenericRepository<ParsedSection> ParsedSections => _parsedSections ??= new GenericRepository<ParsedSection>(_context);
    /// <inheritdoc/>
    public GenericRepository<Chunk> Chunks => _chunks ??= new GenericRepository<Chunk>(_context);
    /// <inheritdoc/>
    public GenericRepository<ChatSession> ChatSessions => _chatSessions ??= new GenericRepository<ChatSession>(_context);
    /// <inheritdoc/>
    public GenericRepository<ChatSessionTitleGenerationSettings> ChatSessionTitleGenerationSettings => _chatSessionTitleGenerationSettings ??= new GenericRepository<ChatSessionTitleGenerationSettings>(_context);
    /// <inheritdoc/>
    public GenericRepository<ChatSessionTitleGenerationMetrics> ChatSessionTitleGenerationMetrics => _chatSessionTitleGenerationMetrics ??= new GenericRepository<ChatSessionTitleGenerationMetrics>(_context);
    /// <inheritdoc/>
    public GenericRepository<ChatMessage> ChatMessages => _chatMessages ??= new GenericRepository<ChatMessage>(_context);
    /// <inheritdoc/>
    public GenericRepository<ChatMessageGenerationSettings> ChatMessageGenerationSettings => _chatMessageGenerationSettings ??= new GenericRepository<ChatMessageGenerationSettings>(_context);
    /// <inheritdoc/>
    public GenericRepository<ChatMessageGenerationMetrics> ChatMessageGenerationMetrics => _chatMessageGenerationMetrics ??= new GenericRepository<ChatMessageGenerationMetrics>(_context);
    /// <inheritdoc/>
    public GenericRepository<Citation> Citations => _citations ??= new GenericRepository<Citation>(_context);
    /// <inheritdoc/>
    public GenericRepository<CitationOccurrence> CitationOccurrences => _citationOccurrences ??= new GenericRepository<CitationOccurrence>(_context);
    /// <inheritdoc/>
    public GenericRepository<TestQuestion> TestQuestions => _testQuestions ??= new GenericRepository<TestQuestion>(_context);
    /// <inheritdoc/>
    public GenericRepository<Experiment> Experiments => _experiments ??= new GenericRepository<Experiment>(_context);
    /// <inheritdoc/>
    public GenericRepository<TestResponse> TestResponses => _testResponses ??= new GenericRepository<TestResponse>(_context);

    /// <inheritdoc/>
    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);
        Dispose(false);
        GC.SuppressFinalize(this);
    }

    private byte _disposed = 0;

    /// <summary>Releases the DbContext resources synchronously.</summary>
    /// <param name="disposing">Indicates whether managed resources should be released.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 0)
        {
            if (disposing)
            {
                _context.Dispose();
            }
        }
    }

    /// <summary>Releases the DbContext resources asynchronously.</summary>
    protected virtual async ValueTask DisposeAsyncCore()
    {
        await _context.DisposeAsync().ConfigureAwait(false);
    }
}

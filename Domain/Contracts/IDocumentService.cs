
using Domain.Entities;

namespace Domain.Contracts
{
    public interface IDocumentService
    {
        
        Task<Document?> GetDocumentWithCommentsAsync(Guid documentId);
        
        
        Task<DocumentComment> AddCommentAsync(Guid documentId, Guid userId, string content);
    }
}
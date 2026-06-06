using Domain.Contracts;
using DataAccess.UnitOfWork;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BusinessLayer.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly IUnitOfWork _unitOfWork;

        public DocumentService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<Document>> GetDocumentsByChapterAsync(Guid chapterId)
        {
            
            return await _unitOfWork.Documents.GetAsync(
                filter: d => d.ChapterId == chapterId
            );
        }

        public async Task<Document?> GetDocumentWithCommentsAsync(Guid documentId)
        {

            var result = await _unitOfWork.Documents.GetAsync(
                filter: d => d.Id == documentId,
                includeProperties: new string[] { "Comments", "Comments.User" } 
            );
            
            return result.FirstOrDefault();
        }

        public async Task<DocumentComment> AddCommentAsync(Guid documentId, Guid userId, string content)
        {
            var comment = new DocumentComment
            {
                DocumentId = documentId,
                UserId = userId,
                Content = content
            };

            await _unitOfWork.DocumentComments.InsertAsync(comment);
            
            await _unitOfWork.SaveAsync(); 

            return comment;
        }
    }
}
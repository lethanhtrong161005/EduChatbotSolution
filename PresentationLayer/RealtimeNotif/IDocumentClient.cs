using Domain.DTOs;

namespace Presentation.RealtimeNotif;

public interface IDocumentClient
{
    Task UpdateStatus(DocumentStatusUpdate documentStatusUpdate);
}

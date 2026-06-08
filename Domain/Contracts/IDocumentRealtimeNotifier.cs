using Domain.DTOs;

namespace Domain.Contracts;

public interface IDocumentRealtimeNotifier
{
    Task UpdateStatus(DocumentStatusUpdate documentStatusUpdate);
}

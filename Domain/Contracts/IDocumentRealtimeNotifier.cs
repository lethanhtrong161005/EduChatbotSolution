using Domain.Contracts.DTOs;

namespace Domain.Contracts;

public interface IDocumentRealtimeNotifier
{
    Task UpdateStatus(DocumentStatusUpdate documentStatusUpdate);
}

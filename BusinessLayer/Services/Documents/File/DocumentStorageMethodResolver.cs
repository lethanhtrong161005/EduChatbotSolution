using DataAccess.UnitOfWork;
using Domain.Contracts;
using Domain.Entities;

namespace Business.Services.Documents.File;

public class DocumentStorageMethodResolver(IUnitOfWork unitOfWork) : IDocumentStorageMethodResolver
{
    public async Task<DocumentStorageMethod> ResolveForPersistenceAsync(
        Document document,
        CancellationToken cxlTkn = default)
    {
        var config = (await unitOfWork.SubjectStorageConfigurations.GetAsync(
                filter: e => e.Id == document.SubjectId,
                cancellationToken: cxlTkn))
            .FirstOrDefault();

        return config?.StorageMethod switch
        {
            DocumentStorageMethod.LocalHardDrive => DocumentStorageMethod.LocalHardDrive,
            DocumentStorageMethod.Supabase => DocumentStorageMethod.Supabase,
            _ => DocumentStorageMethod.Supabase,
        };
    }
}

using DataAccess.UnitOfWork;
using Domain.Contracts;
using Domain.Entities;
using Domain.Utils;

namespace Business.Services.Documents.File;

public class DocumentStorageMethodResolver(IUnitOfWork unitOfWork) : IDocumentStorageMethodResolver
{
    public async Task<FileStorageMethod> ResolveForPersistenceAsync(
        Document document,
        CancellationToken cxlTkn = default)
    {
        var config = (await unitOfWork.SubjectStorageConfigurations.GetAsync(
                filter: e => e.Id == document.SubjectId,
                cancellationToken: cxlTkn))
            .FirstOrDefault();

        return config?.StorageMethod switch
        {
            FileStorageMethod.LocalHardDrive => FileStorageMethod.LocalHardDrive,
            FileStorageMethod.Supabase => FileStorageMethod.Supabase,
            _ => FileStorageMethod.Supabase,
        };
    }
}

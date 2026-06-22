namespace Presentation.ViewModels;

public class DocumentLibraryVm
{
    public List<SubjectLookupVm> Subjects { get; set; } = [];
}

public class SubjectLookupVm
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

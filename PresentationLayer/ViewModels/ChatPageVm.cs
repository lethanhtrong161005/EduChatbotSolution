namespace Presentation.ViewModels;

public class ChatPageVm
{
    public Guid? ActiveSessionId { get; set; }

    public List<SubjectSelectionVm> Subjects { get; set; } = [];
}

public class SubjectSelectionVm
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
}

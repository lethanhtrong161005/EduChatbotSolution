namespace Presentation.Models;

public class ChatPageVm
{
    public Guid? ActiveSessionId { get; set; }

    public List<SubjectSelectionVm> Subjects { get; set; } = [];

    public List<ChatSidebarSessionVm> Sessions { get; set; } = [];
}

public class SubjectSelectionVm
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
}

public class ChatSidebarSessionVm
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public DateTime LastMessageAt { get; set; }
}

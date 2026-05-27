namespace DataAccessLayer.Entities;

public class Document : UuidEntity
{
    public Guid ChapterId { get; set; }
    public Guid UploaderId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long Size { get; set; }
    public string Path { get; set; } = string.Empty;


    public virtual Chapter Chapter { get; set; } = null!;
    public virtual User Uploader { get; set; } = null!;
}

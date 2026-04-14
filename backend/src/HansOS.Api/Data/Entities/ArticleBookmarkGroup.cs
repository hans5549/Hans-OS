namespace HansOS.Api.Data.Entities;

/// <summary>文章收藏群組</summary>
public class ArticleBookmarkGroup
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ApplicationUser User { get; set; } = null!;
}

namespace HansOS.Api.Data.Entities;

/// <summary>文章收藏來源類型</summary>
public enum ArticleBookmarkSourceType
{
    /// <summary>外部文章連結</summary>
    ExternalUrl = 1,

    /// <summary>系統內建文章</summary>
    InternalArticle = 2,
}

/// <summary>文章收藏</summary>
public class ArticleBookmark
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ArticleBookmarkSourceType SourceType { get; set; }
    public string? SourceId { get; set; }
    public string? Url { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? CustomTitle { get; set; }
    public string? ExcerptSnapshot { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? Domain { get; set; }
    public string? Note { get; set; }
    public Guid? GroupId { get; set; }
    public string[] Tags { get; set; } = [];
    public bool IsPinned { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastOpenedAt { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public ArticleBookmarkGroup? Group { get; set; }
}

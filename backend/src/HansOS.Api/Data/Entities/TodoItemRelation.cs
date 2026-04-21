namespace HansOS.Api.Data.Entities;

/// <summary>任務間關聯（單向儲存，反向在查詢層計算）</summary>
public class TodoItemRelation
{
    public Guid Id { get; set; }

    /// <summary>主體任務（關聯發起方）</summary>
    public Guid SourceItemId { get; set; }
    public TodoItem SourceItem { get; set; } = null!;

    /// <summary>目標任務（關聯對象）</summary>
    public Guid TargetItemId { get; set; }
    public TodoItem TargetItem { get; set; } = null!;

    public TodoRelationType RelationType { get; set; }
}

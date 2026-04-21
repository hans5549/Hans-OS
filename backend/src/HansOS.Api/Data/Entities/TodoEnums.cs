namespace HansOS.Api.Data.Entities;

/// <summary>待辦任務狀態</summary>
public enum TodoStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
}

/// <summary>待辦任務優先級</summary>
public enum TodoPriority
{
    /// <summary>緊急 (P1)</summary>
    Urgent = 0,

    /// <summary>高 (P2)</summary>
    High = 1,

    /// <summary>中 (P3)</summary>
    Medium = 2,

    /// <summary>低 (P4)</summary>
    Low = 3,
}

/// <summary>待辦任務難度</summary>
public enum TodoDifficulty
{
    /// <summary>簡單（&lt; 30 分鐘）</summary>
    Easy = 0,

    /// <summary>中等（1–2 小時）</summary>
    Medium = 1,

    /// <summary>困難（半天）</summary>
    Hard = 2,

    /// <summary>超難（一天以上）</summary>
    VeryHard = 3,
}

/// <summary>週期性任務模式</summary>
public enum RecurrencePattern
{
    Daily = 0,
    Weekly = 1,
    Monthly = 2,
    Yearly = 3,
}

/// <summary>任務關聯類型</summary>
public enum TodoRelationType
{
    /// <summary>依賴（Source 依賴 Target）</summary>
    DependsOn = 0,

    /// <summary>相關（僅標記，不阻塞）</summary>
    Related = 1,
}

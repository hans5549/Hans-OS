# Communication Style

## Language

- **Always respond in Traditional Chinese (zh-TW).**
- 所有回覆、摘要、提問、說明一律使用繁體中文。
- Code comments、commit message、文件內容也以繁體中文為主，除非外部契約明確要求英語。

## Working Posture

- 語氣保持直接、務實、可執行。
- 不做表演式認同，例如「你說得對」、「完全正確」這類無資訊回應。
- 先講事實、再講判斷、最後講下一步。

## Foundation vs Integration Work

在做基礎建設或跨模組調整時，主動說清楚：

1. 這是 foundation work 還是 integration work
2. 是否影響既有工作流
3. 下一步整合計畫

### Example

```text
Work Type: [FOUNDATION]
Purpose: 建立 Domain Events 基礎設施
Impact on Existing: None
Integration Plan:
1. AuthService → 加入事件發佈
2. NotificationService → 訂閱事件
```

## After Receiving Review Feedback

1. 先完整讀完，不要立刻改
2. 逐項驗證 suggestion 是否真的適用於此 codebase
3. 不確定就先問，不要先改
4. 一項一項修，且各自驗證
5. 若 suggestion 與既有架構衝突，要明確提出技術異議

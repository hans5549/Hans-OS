import type { SystemDesignContent } from '../../_shared/types';

export const content: SystemDesignContent = {
  title: 'Scalability',
  sections: [
    {
      id: 'section-0',
      title: '分散式系統中的可擴展性(Scalability)',
      blocks: [
        { type: 'subheading', text: '什麼是可擴展性?' },
        { type: 'paragraph', text: 'Scalability(可擴展性) 指的是系統能隨著需求(使用者、資料量、流量)的增加, 透過增加資源來維持效能與穩定性的能力。' },
        { type: 'paragraph', text: '在分散式系統中,可擴展性是關鍵,因為使用量往往會不斷成長,我們也要確保系統 在高流量下仍維持低延遲和高吞吐量。' },
        { type: 'list', ordered: false, items: ['吞吐量 (Throughput):每秒能處理的請求。', '延遲 (Latency):請求處理所需時間。'] },
        { type: 'subheading', text: '為什麼重要?' },
        { type: 'list', ordered: true, items: ['應對高流量:使用者與請求量成長時,若架構不可擴展,可能會發生延遲、錯誤甚'] },
        { type: 'paragraph', text: '至宕機。' },
        { type: 'list', ordered: true, items: ['成本效率:可擴展系統能根據需求彈性增加或減少資源,避免浪費。', '業務成長:產品成功後,架構必須支撐更多使用者,否則成長會成為壓力。', '可靠性:分散式可擴展設計能避免單點故障,提高整體穩定度。'] },
        { type: 'subheading', text: '垂直擴展(Vertical Scaling)' },
        { type: 'paragraph', text: '假設你有一台咖啡機(伺服器),每天要幫忙沖咖啡給員工(處理請求,一開始它只 有單孔濾網,一次只能沖 1 杯。當需求變大時,你換成一台更高級的咖啡機,有三孔濾 網,一次能沖 3 杯(加 RAM、換更快的 CPU、用更大的硬碟)。' },
        { type: 'paragraph', text: '做法:升級單一伺服器(增加 CPU、記憶體、硬碟)。' },
        { type: 'paragraph', text: '優點:簡單、改動少。' },
        { type: 'paragraph', text: '缺點:有物理上限,成本高。' },
        { type: 'subheading', text: '水平擴展(Horizontal Scaling)' },
        { type: 'paragraph', text: '還是咖啡的例子:舊咖啡機能力有限,但員工數量增加很多。與其一直換更高級的咖 啡機,不如多買幾台普通咖啡機,請更多人同時操作,一起沖咖啡(加伺服器節點, 做負載平衡)。' },
        { type: 'paragraph', text: '做法:增加伺服器數量,透過 Load Balancer (負載平衡) 讓多台機器分擔工作。' },
        { type: 'paragraph', text: '優點:理論上無限擴展,彈性佳,能應對大規模成長。' },
        { type: 'paragraph', text: '缺點:系統設計複雜,需要處理分散式資料一致性等問題。' },
        { type: 'subheading', text: '此章節的重點:' },
        { type: 'paragraph', text: '一般來說,企業會先透過垂直擴展解決短期需求,等流量持續增加時,再轉向水平擴 展來支撐長期成長。' },
        { type: 'table', headers: ['比較項目', 'Vertical Scaling', 'Horizontal Scaling'], rows: [['做法', '升級單機', '增加機器數量'], ['上限', '有物理上限', '理論無限'], ['複雜度', '低', '高'], ['成本', '初期低、後期貴', '彈性'], ['適合', '短期、快速解法', '長期、大規模']] },
      ],
    },
  ],
  selfTest: [
    { question: 'Throughput 和 Latency 分別指什麼?', answer: '• Throughput(吞吐量):每秒能處理的請求數量。 • Latency(延遲):單一請求從發出到完成所需的時間。' },
    { question: '以下敘述何者正確?(A) Horizontal scaling 設計比 vertical scaling 簡單 (B) Vertical scaling 理論上沒有上限 (C) Horizontal scaling 需要處理分散式資料一致性 問題 (D) 一般建議直接從 horizontal scaling 開始', answer: '答案:(C) (A) 錯 — horizontal scaling 的設計更複雜,需要 load balancer 和分散式架 構。 (B) 錯 — vertical scaling 有物理上限(單台機器的 CPU、RAM 上限)。 (C) 對 — 多台機器間需要處理資料一致性。 (D) 錯 — 一般先用 vertical scaling 解決短期需求,再轉向 horizontal scaling。' },
    { question: '為什麼可擴展性對業務很重要?列舉至少三個原因。', answer: '應對高流量:使用者與請求量成長時,避免延遲、錯誤或宕機。成本效率:根據需 求彈性增減資源,避免浪費。業務成長:架構必須支撐更多使用者。可靠性:分散 式設計避免單點故障,提高穩定度。' },
  ],
};

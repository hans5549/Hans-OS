<script lang="ts" setup>
defineOptions({ name: 'QrCodeGeneratorPage' });

interface BilingualText {
  en: string;
  zhTw: string;
}

interface MetricCard {
  description: BilingualText;
  label: BilingualText;
  value: BilingualText;
}

interface RequirementCard {
  points: BilingualText[];
  title: BilingualText;
}

type SummaryCard =
  | {
      body: BilingualText;
      kind: 'body';
      title: BilingualText;
    }
  | {
      kind: 'points';
      points: BilingualText[];
      title: BilingualText;
    };

interface ApiEndpoint {
  method: 'DELETE' | 'GET' | 'POST' | 'PUT';
  notes: BilingualText[];
  path: string;
  requestExample?: string;
  responseExample?: string;
  summary: BilingualText;
}

interface FlowSection {
  emphasis: BilingualText;
  steps: BilingualText[];
  title: BilingualText;
}

interface DeepDiveSection {
  decisions: BilingualText[];
  problem: BilingualText;
  title: BilingualText;
  tradeoffs: BilingualText[];
}

interface SectionLink {
  id: string;
  title: BilingualText;
}

const copy = (en: string, zhTw: string): BilingualText => ({ en, zhTw });

const pageNote = {
  message: copy(
    'This is a system design case study, not an interactive QR code generator.',
    '這是一篇系統設計案例研究，不是可直接操作的 QR Code 產生器。',
  ),
  description: copy(
    'This page turns the PDF “Design a QR code Generator” into a readable web case study focused on requirements, data flow, performance, and scaling decisions.',
    '本頁將 PDF《Design a QR code Generator》整理成更好讀的網頁案例，重點放在需求、資料流、效能與擴充性決策。',
  ),
};

const heroChecklist = [
  copy(
    'Start with the summary cards before reading the APIs.',
    '先看摘要卡片，再進入 API 與細節內容。',
  ),
  copy(
    'Treat redirect latency and cache freshness as the central trade-off.',
    '把 redirect 延遲與快取新鮮度視為整份設計的核心取捨。',
  ),
  copy(
    'Read code samples as interface contracts, not implementation details.',
    '把程式範例視為介面契約，而不是最終實作細節。',
  ),
];

const sectionLinks: SectionLink[] = [
  { id: 'summary', title: copy('Executive Summary', '執行摘要') },
  { id: 'requirements', title: copy('Requirements', '需求整理') },
  { id: 'api-design', title: copy('API Design', 'API 設計') },
  { id: 'flows', title: copy('Core Flows', '核心流程') },
  { id: 'deep-dives', title: copy('Deep Dives', '關鍵設計拆解') },
  { id: 'scaling', title: copy('Scaling / Decisions', '擴充與決策') },
];

const metricCards: MetricCard[] = [
  {
    label: copy('Input Limit', '輸入限制'),
    value: copy('ASCII URL <= 20 chars', 'ASCII 網址 <= 20 個字元'),
    description: copy(
      'Validate the submitted URL before creating a QR code and enforce the stated length limit.',
      '建立 QR Code 前要先驗證 URL，並嚴格執行題目給定的長度限制。',
    ),
  },
  {
    label: copy('Latency Goal', '延遲目標'),
    value: copy('< 100ms Redirect', '< 100ms 重新導向'),
    description: copy(
      'Redirect is the critical read path, so the latency target must stay extremely low.',
      'Redirect 是最重要的讀取路徑，因此延遲目標必須維持在極低水位。',
    ),
  },
  {
    label: copy('Design Scale', '設計規模'),
    value: copy('1B QR / 100M users', '10 億筆 QR / 1 億使用者'),
    description: copy(
      'The design assumes a high-capacity, highly available, read-heavy system from day one.',
      '這份設計從一開始就假設系統具備高容量、高可用，以及讀多寫少的特性。',
    ),
  },
];

const summaryCards: SummaryCard[] = [
  {
    kind: 'body',
    title: copy('What problem is this design solving?', '這個設計在解什麼問題？'),
    body: copy(
      'The service accepts a URL, creates a globally unique qr_token, and generates the corresponding QR code. When the QR code is scanned, the system uses that token to resolve the original URL and returns a redirect.',
      '這個服務接收一個 URL，建立全球唯一的 qr_token，並產生對應的 QR Code。當使用者掃描後，系統再用該 token 找回原始網址並完成重新導向。',
    ),
  },
  {
    kind: 'points',
    title: copy('The decisions that matter most', '最重要的三個設計決策'),
    points: [
      copy(
        'Choose 302 instead of 301 so mappings can still be updated or deleted later.',
        '選 302 而不是 301，才能讓之後的更新或刪除立即生效。',
      ),
      copy(
        'Use index + cache + CDN to absorb the read-heavy redirect workload.',
        '用索引、快取與 CDN 去吸收讀多寫少的 redirect 壓力。',
      ),
      copy(
        'Combine Base62 encoding with a UNIQUE constraint to balance short tokens and collision safety.',
        '把 Base62 編碼與資料庫 UNIQUE 約束搭配使用，在短 token 與碰撞安全之間取得平衡。',
      ),
    ],
  },
];

const requirements: RequirementCard[] = [
  {
    title: copy('Functional Requirements', '功能需求'),
    points: [
      copy(
        'Users can submit a URL and receive a generated qr_token.',
        '使用者可以提交 URL，並取得系統產生的 qr_token。',
      ),
      copy(
        'Users can manage QR codes they created earlier.',
        '使用者可以管理自己先前建立的 QR Code。',
      ),
      copy(
        'After scanning a QR code, the system redirects to the original URL.',
        '當使用者掃描 QR Code 後，系統需要重新導向到原始網址。',
      ),
    ],
  },
  {
    title: copy('Non-Functional Requirements', '非功能需求'),
    points: [
      copy(
        'The service should remain available 24/7 with high availability.',
        '服務必須 24/7 持續可用，並具備高可用性。',
      ),
      copy(
        'Redirect latency should stay below 100ms.',
        '重新導向延遲應維持在 100ms 以下。',
      ),
      copy(
        'The system should support 1 billion QR codes and 100 million users.',
        '系統要能支援 10 億筆 QR Code 與 1 億使用者。',
      ),
    ],
  },
];

const apiEndpoints: ApiEndpoint[] = [
  {
    method: 'POST',
    path: 'v1/qr_code',
    summary: copy(
      'Create a new QR code mapping.',
      '建立新的 QR Code 對應關係。',
    ),
    requestExample: `{\n  "url": "https://example.com"\n}`,
    responseExample: `{\n  "qr_token": "Ab3xK9"\n}`,
    notes: [
      copy(
        'Validate the URL before generating a globally unique qr_token.',
        '在產生全球唯一的 qr_token 之前，先完成 URL 驗證。',
      ),
      copy(
        'Persist the mapping in the QrCodes table and rely on a DB UNIQUE constraint for final uniqueness.',
        '把映射資料寫入 QrCodes 資料表，並以資料庫 UNIQUE 約束作為最後的唯一性防線。',
      ),
    ],
  },
  {
    method: 'GET',
    path: 'v1/qr_code_image/:qr_token?dimension=300&color=000000&border=10',
    summary: copy(
      'Return the QR image location based on the token and image parameters.',
      '依據 token 與圖片參數回傳 QR 圖像位置。',
    ),
    responseExample: `{\n  "image_location": "https://cdn.example.com/qrcode/Ab3xK9.png"\n}`,
    notes: [
      copy(
        'The QR image behaves like static content and fits well in object storage plus a CDN.',
        'QR 圖像本質上接近靜態內容，很適合放在物件儲存搭配 CDN。',
      ),
      copy(
        'Query parameters control image settings such as dimension, color, and border.',
        '查詢參數負責控制圖片尺寸、顏色與邊框等設定。',
      ),
    ],
  },
  {
    method: 'PUT',
    path: 'v1/qr_code/:qr_token',
    summary: copy(
      'Update the original destination URL for an existing QR code.',
      '更新既有 QR Code 對應的原始目的地網址。',
    ),
    requestExample: `{\n  "url": "https://example.com/new-destination"\n}`,
    notes: [
      copy(
        'The same qr_token remains in use, so redirects must always resolve the latest mapping.',
        '同一個 qr_token 會持續沿用，因此 redirect 必須永遠解析到最新的映射結果。',
      ),
    ],
  },
  {
    method: 'DELETE',
    path: 'v1/qr_code/:qr_token',
    summary: copy(
      'Delete the QR code mapping for the specified token.',
      '刪除指定 token 的 QR Code 映射資料。',
    ),
    notes: [
      copy(
        'Deletion must invalidate cache entries so stale mappings are not served afterward.',
        '刪除後必須同步清掉快取，避免後續仍送出過期映射。',
      ),
    ],
  },
  {
    method: 'GET',
    path: 'v1/qr_code/:qr_token',
    summary: copy(
      'Resolve the original URL, or perform the redirect in the real scan flow.',
      '解析原始網址，或在真實掃描流程中直接執行 redirect。',
    ),
    responseExample: `{\n  "url": "https://example.com"\n}`,
    notes: [
      copy(
        'The URL embedded in the QR code is https://myqrcode.com/{qr_token}.',
        '寫進 QR Code 的網址會是 https://myqrcode.com/{qr_token}。',
      ),
      copy(
        'The design chooses a 302 Temporary Redirect so every request rechecks the latest state.',
        '此設計採用 302 Temporary Redirect，讓每次請求都重新確認最新狀態。',
      ),
    ],
  },
];

const flows: FlowSection[] = [
  {
    title: copy('Create / Edit Flow', '建立 / 編輯流程'),
    emphasis: copy(
      'Key point: validate input first, persist the mapping second, and return the token last.',
      '關鍵順序：先驗證輸入，再持久化映射，最後才回傳 token。',
    ),
    steps: [
      copy(
        'The client calls POST /v1/qr_code with a URL in the request body.',
        '客戶端以 request body 帶入 URL，呼叫 POST /v1/qr_code。',
      ),
      copy(
        'The server validates the URL and rejects invalid input immediately.',
        '伺服器先驗證 URL，若輸入無效就立即拒絕。',
      ),
      copy(
        'The service generates a globally unique qr_token and stores the row in the QrCodes table.',
        '服務產生全球唯一的 qr_token，並把資料列寫入 QrCodes 資料表。',
      ),
      copy(
        'If a token collision happens, the DB UNIQUE constraint fails and the service retries with a new token.',
        '若 token 發生碰撞，資料庫 UNIQUE 約束會失敗，服務再以新 token 重試。',
      ),
      copy(
        'The response returns the qr_token; updates keep using the existing token.',
        '回應會帶回 qr_token；之後更新時仍沿用既有 token。',
      ),
    ],
  },
  {
    title: copy('Retrieve / Redirect Flow', '讀取 / 重新導向流程'),
    emphasis: copy(
      'Key point: the image can be cached aggressively, but redirect mappings must stay up to date.',
      '關鍵重點：圖片可以積極快取，但 redirect 映射一定要保持最新。',
    ),
    steps: [
      copy(
        'The client calls GET /v1/qr_code_image/:qr_token with dimension, color, and border parameters.',
        '客戶端呼叫 GET /v1/qr_code_image/:qr_token，並附帶尺寸、顏色與邊框參數。',
      ),
      copy(
        'The server generates the QR image or returns an existing image resource location.',
        '伺服器產生 QR 圖像，或直接回傳既有圖片資源位置。',
      ),
      copy(
        'The QR code points to https://myqrcode.com/{qr_token}.',
        'QR Code 實際指向的是 https://myqrcode.com/{qr_token}。',
      ),
      copy(
        'After a scan, the backend looks up the latest original URL in the QrCodes table.',
        '掃描後，後端會到 QrCodes 資料表查詢最新的原始網址。',
      ),
      copy(
        'The server returns a 302 redirect instead of a 301 to avoid permanent browser caching.',
        '伺服器回傳 302 而不是 301，避免瀏覽器永久快取舊結果。',
      ),
    ],
  },
];

const redirectComparison = {
  title: copy('Why 302 instead of 301?', '為什麼選 302 而不是 301？'),
  permanent: {
    title: copy('301 Permanent Redirect', '301 永久重新導向'),
    body: copy(
      'Browsers tend to cache the result. If the mapping is later updated or deleted, future requests may bypass the server entirely.',
      '瀏覽器通常會快取結果。若映射之後被更新或刪除，後續請求可能直接繞過伺服器。',
    ),
  },
  temporary: {
    title: copy('302 Temporary Redirect', '302 暫時重新導向'),
    body: copy(
      'Every request still flows through the backend, so users see the latest mapping after an owner edits the destination.',
      '每次請求都仍會經過後端，因此當擁有者修改目的地後，使用者能拿到最新映射。',
    ),
  },
};

const deepDives: DeepDiveSection[] = [
  {
    title: copy('How to generate a unique token', '如何產生唯一 token'),
    problem: copy(
      'The key space must be large enough to keep collisions rare while still producing a short, portable qr_token.',
      '鍵值空間必須夠大，才能讓碰撞機率維持很低，同時又能產生短小且容易攜帶的 qr_token。',
    ),
    decisions: [
      copy(
        'Use a hash function for fixed-length output, then mix in a secret or nonce so identical inputs do not always map to the same token.',
        '先用雜湊函式得到固定長度輸出，再加入 secret 或 nonce，避免相同輸入永遠映射到同一個 token。',
      ),
      copy(
        'Encode the hash with Base62 to make the token shorter and easier to transmit, then truncate to the desired length.',
        '把雜湊結果做 Base62 編碼，讓 token 更短、更好傳輸，再截斷到目標長度。',
      ),
      copy(
        'Rely on a DB UNIQUE constraint as the final guardrail and retry if a collision still appears.',
        '把資料庫 UNIQUE 約束當作最後一道防線；若仍發生碰撞，就重試產生新 token。',
      ),
    ],
    tradeoffs: [
      copy(
        'A purely deterministic hash reduces DB lookups, but the same long URL always maps to the same short token, which is weaker for privacy and product flexibility.',
        '完全決定性的雜湊可以減少查庫，但相同長網址永遠得到同一個短 token，對隱私與產品彈性都較差。',
      ),
      copy(
        'Adding a nonce improves randomness, but then the token-to-URL mapping must be stored in the database.',
        '加入 nonce 可以提升隨機性，但也代表 token 與 URL 的映射必須落地儲存到資料庫。',
      ),
    ],
  },
  {
    title: copy('How to make redirects fast', '如何讓 redirect 夠快'),
    problem: copy(
      'Read traffic is far higher than write traffic, so the database quickly becomes a bottleneck if every redirect hits it directly.',
      '讀取流量遠高於寫入流量，因此若每次 redirect 都直接打資料庫，資料庫很快就會成為瓶頸。',
    ),
    decisions: [
      copy(
        'Index qr_token to avoid full-table scans.',
        '對 qr_token 建索引，避免整表掃描。',
      ),
      copy(
        'Use cache for hot mappings and fall back to the database only on cache misses.',
        '把熱門映射放進快取，只有在 cache miss 時才回到資料庫。',
      ),
      copy(
        'Push QR images and hot mappings as far forward as possible, ideally to the CDN or edge.',
        '把 QR 圖像與熱門映射盡量往前推，理想上要推到 CDN 或 edge 層。',
      ),
    ],
    tradeoffs: [
      copy(
        'A local cache is simple, but hit rates drop when traffic spreads across many instances.',
        '本地快取實作簡單，但當流量分散到多個實例後，命中率會下降。',
      ),
      copy(
        'A distributed cache improves hit rates, but adds system complexity and operational cost.',
        '分散式快取能提升命中率，但會增加系統複雜度與維運成本。',
      ),
      copy(
        'If updates and deletes must take effect immediately, cache invalidation becomes mandatory.',
        '如果更新與刪除必須立即生效，就一定要做好快取失效機制。',
      ),
    ],
  },
  {
    title: copy('How the system scales', '系統如何擴展'),
    problem: copy(
      'Supporting 1 billion records and 100 million users requires capacity planning and fault tolerance from the start.',
      '若要支援 10 億筆資料與 1 億使用者，從一開始就必須做好容量規劃與容錯設計。',
    ),
    decisions: [
      copy(
        'Keep application servers stateless so they can scale horizontally.',
        '讓應用伺服器維持無狀態，才能進行水平擴充。',
      ),
      copy(
        'Because reads dominate writes, add read replicas to absorb redirect traffic.',
        '由於讀取遠多於寫入，因此要加上 read replicas 去承接 redirect 流量。',
      ),
      copy(
        'Use notifications plus cron-driven cleanup for stale data that has not been used for a long time.',
        '對於長期未使用的陳舊資料，可用通知加上 cron 排程清理。',
      ),
    ],
    tradeoffs: [
      copy(
        'A single DB instance may work for a while, but it is weaker for resilience.',
        '單一資料庫實例短期內也許可行，但在韌性上會更脆弱。',
      ),
      copy(
        'Replicas improve read performance and availability, but add consistency and failover management costs.',
        '副本能提升讀取效能與可用性，但也會帶來一致性與故障切換管理成本。',
      ),
    ],
  },
];

const scalingContent = {
  capacity: copy(
    'The document assumes a read-heavy workload and estimates that 500 million redirects per day is roughly 5,787 requests per second. That is why the redirect path becomes the center of the indexing, caching, and CDN discussion.',
    '這份文件假設系統屬於讀多寫少，並估算每天 5 億次 redirect 約等於每秒 5,787 次請求。也因此，redirect 路徑自然成為索引、快取與 CDN 設計的核心。',
  ),
  snapshotNote: copy(
    'Think of this as the shortest path from a phone scan to the final destination URL.',
    '可以把它理解成：手機掃描後，要如何以最短路徑抵達最終網址。',
  ),
  takeaways: [
    copy(
      'Redirect is the most important read path, so hot tokens should land in cache and CDN first.',
      'Redirect 是最重要的讀取路徑，因此熱門 token 應優先落在快取與 CDN。',
    ),
    copy(
      'The QR image itself is static content, which makes it a strong fit for CDN delivery.',
      'QR 圖像本身屬於靜態內容，很適合交給 CDN 發送。',
    ),
    copy(
      'Choosing 302 over 301 prioritizes product flexibility: updates or deletes can take effect immediately.',
      '選擇 302 而不是 301，代表把產品彈性放在優先位置：更新或刪除都能立即生效。',
    ),
    copy(
      'At 100 million users and 5 redirects per user per day, the steady-state load is about 5,787 redirects per second.',
      '若有 1 億使用者且每人每天 5 次 redirect，穩態流量大約會落在每秒 5,787 次 redirect。',
    ),
  ],
};

const architectureSnapshot = [
  'Client -> QR Image / CDN',
  '-> https://myqrcode.com/{qr_token}',
  '-> App Server (stateless)',
  '-> Cache (hot mappings)',
  '-> DB / Read Replicas',
  '-> Original URL',
  '-> 302 Redirect',
].join('\n');
</script>

<template>
  <div
    lang="en"
    class="min-h-full bg-[radial-gradient(circle_at_top_left,_rgba(56,189,248,0.12),_transparent_28%),radial-gradient(circle_at_top_right,_rgba(59,130,246,0.12),_transparent_26%),linear-gradient(180deg,_#f8fbff_0%,_#f8fafc_38%,_#ffffff_100%)]"
  >
    <div class="mx-auto max-w-7xl space-y-6 px-4 py-6 md:px-6 lg:space-y-8 lg:px-8 lg:py-8">
      <a-alert
        type="info"
        show-icon
        class="rounded-3xl border border-sky-100/90 bg-white/[0.85] shadow-sm shadow-sky-100/70"
      >
        <template #message>
          <div class="space-y-1">
            <p class="text-sm font-semibold text-slate-900">{{ pageNote.message.en }}</p>
            <p lang="zh-Hant" class="text-sm leading-6 text-slate-600">{{ pageNote.message.zhTw }}</p>
          </div>
        </template>
        <template #description>
          <div class="space-y-1 pt-1">
            <p class="text-sm leading-7 text-slate-600">{{ pageNote.description.en }}</p>
            <p lang="zh-Hant" class="text-sm leading-7 text-slate-500">{{ pageNote.description.zhTw }}</p>
          </div>
        </template>
      </a-alert>

      <section class="overflow-hidden rounded-[32px] border border-slate-200/70 bg-slate-950 text-slate-50 shadow-[0_28px_90px_-48px_rgba(15,23,42,0.75)]">
        <div class="grid gap-8 px-6 py-7 lg:grid-cols-[1.2fr_0.8fr] lg:px-8 lg:py-9">
          <div class="space-y-6">
            <div class="space-y-2">
              <div class="inline-flex rounded-full border border-sky-400/30 bg-sky-400/10 px-3 py-1 text-[11px] font-semibold uppercase tracking-[0.24em] text-sky-200">
                System Design Case Study
              </div>
              <p lang="zh-Hant" class="text-sm leading-6 text-slate-300">系統設計案例研究</p>
            </div>

            <div class="space-y-4">
              <h1 class="max-w-4xl text-4xl font-semibold tracking-tight text-white md:text-5xl">
                QR Code Generator
              </h1>
              <p class="max-w-4xl text-base leading-8 text-slate-200 md:text-lg">
                The QR image is the easy part. The hard part is making
                <span class="font-semibold text-white">token generation, redirect freshness, and caching</span>
                feel instant even when the system grows to billions of mappings.
              </p>
              <p lang="zh-Hant" class="max-w-4xl text-sm leading-7 text-slate-300 md:text-base">
                產圖本身其實不難，真正困難的是讓
                <span class="font-semibold text-slate-100">token 生成、redirect 新鮮度與快取策略</span>
                在資料量與流量都放大後，仍然維持即時且可靠。
              </p>
            </div>

            <div class="flex flex-wrap gap-2 text-xs">
              <a-tag color="processing">
                Read-heavy / <span lang="zh-Hant">讀多寫少</span>
              </a-tag>
              <a-tag color="cyan">
                302 Redirect / <span lang="zh-Hant">302 轉址</span>
              </a-tag>
              <a-tag color="purple">
                Base62 Token / <span lang="zh-Hant">Base62 權杖</span>
              </a-tag>
              <a-tag color="gold">
                Cache + CDN / <span lang="zh-Hant">快取 + CDN</span>
              </a-tag>
            </div>
          </div>

          <div class="grid gap-4 md:grid-cols-2 lg:grid-cols-1">
            <div class="rounded-[28px] border border-white/10 bg-white/[0.06] p-5 backdrop-blur">
              <div class="space-y-1">
                <p class="text-sm font-semibold text-white">Reading lens</p>
                <p lang="zh-Hant" class="text-sm leading-6 text-slate-300">先抓核心壓力，再看細節取捨</p>
              </div>
              <div class="mt-4 space-y-4">
                <div
                  v-for="item in heroChecklist"
                  :key="item.en"
                  class="space-y-1 border-b border-white/10 pb-4 last:border-b-0 last:pb-0"
                >
                  <p class="text-sm font-medium leading-6 text-slate-100">{{ item.en }}</p>
                  <p lang="zh-Hant" class="text-sm leading-6 text-slate-300">{{ item.zhTw }}</p>
                </div>
              </div>
            </div>

            <div class="rounded-[28px] border border-sky-400/20 bg-gradient-to-br from-sky-400/15 via-slate-900 to-slate-950 p-5">
              <div class="space-y-1">
                <p class="text-sm font-semibold text-white">Architecture snapshot</p>
                <p lang="zh-Hant" class="text-sm leading-6 text-slate-300">從掃描到轉址的最短路徑</p>
              </div>
              <pre class="mt-4 overflow-x-auto rounded-2xl border border-white/10 bg-slate-950/80 p-4 text-xs leading-6 text-sky-100">{{ architectureSnapshot }}</pre>
              <p class="mt-3 text-xs leading-6 text-slate-300">{{ scalingContent.snapshotNote.en }}</p>
              <p lang="zh-Hant" class="text-xs leading-6 text-slate-400">{{ scalingContent.snapshotNote.zhTw }}</p>
            </div>
          </div>
        </div>
      </section>

      <div class="grid gap-4 xl:hidden">
        <div class="rounded-[28px] border border-slate-200/70 bg-white/90 p-5 shadow-[0_24px_60px_-50px_rgba(15,23,42,0.45)] backdrop-blur">
          <div class="space-y-1">
            <p class="text-sm font-semibold text-slate-900">Page Map</p>
            <p lang="zh-Hant" class="text-sm leading-6 text-slate-500">頁面導覽</p>
          </div>
          <div class="mt-4 grid gap-2 sm:grid-cols-2">
            <a
              v-for="link in sectionLinks"
              :key="`${link.id}-mobile`"
              :href="`#${link.id}`"
              class="block cursor-pointer rounded-2xl border border-slate-200 bg-slate-50/70 px-4 py-3 transition hover:border-sky-300 hover:bg-sky-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-sky-500"
            >
              <p class="text-sm font-semibold text-slate-800">{{ link.title.en }}</p>
              <p lang="zh-Hant" class="mt-1 text-sm leading-6 text-slate-500">{{ link.title.zhTw }}</p>
            </a>
          </div>
        </div>

        <div class="grid gap-4 md:grid-cols-3">
          <div
            v-for="metric in metricCards"
            :key="`${metric.label.en}-mobile`"
            class="rounded-[28px] border border-slate-200/70 bg-white/90 p-5 shadow-[0_24px_60px_-50px_rgba(15,23,42,0.45)] backdrop-blur"
          >
            <div class="space-y-1">
              <p class="text-xs font-semibold uppercase tracking-[0.18em] text-slate-500">{{ metric.label.en }}</p>
              <p lang="zh-Hant" class="text-sm text-slate-500">{{ metric.label.zhTw }}</p>
            </div>
            <div class="mt-3 space-y-1">
              <p class="text-lg font-semibold text-slate-950">{{ metric.value.en }}</p>
              <p lang="zh-Hant" class="text-sm font-medium text-slate-500">{{ metric.value.zhTw }}</p>
            </div>
            <div class="mt-3 space-y-1">
              <p class="text-sm leading-7 text-slate-700">{{ metric.description.en }}</p>
              <p lang="zh-Hant" class="text-sm leading-7 text-slate-500">{{ metric.description.zhTw }}</p>
            </div>
          </div>
        </div>
      </div>

      <div class="grid gap-6 xl:grid-cols-[minmax(0,1fr)_320px] xl:items-start">
        <div class="space-y-6 lg:space-y-8">
          <section
            id="summary"
            class="scroll-mt-24 rounded-[28px] border border-slate-200/70 bg-white/90 p-6 shadow-[0_24px_60px_-50px_rgba(15,23,42,0.45)] backdrop-blur md:p-8"
          >
            <div class="space-y-6">
              <div class="space-y-3">
                <p class="text-sm font-semibold uppercase tracking-[0.2em] text-sky-600">Executive Summary</p>
                <div class="space-y-3">
                  <h2 class="text-3xl font-semibold tracking-tight text-slate-950">Read the system in one focused pass</h2>
                  <p class="max-w-3xl text-sm leading-7 text-slate-700 md:text-base">
                    Start here to understand the product framing, the primary bottleneck, and the three design choices
                    that shape everything else on the page.
                  </p>
                  <p lang="zh-Hant" class="max-w-3xl text-sm leading-7 text-slate-500 md:text-base">
                    從這裡開始抓整份設計的問題定義、主要瓶頸，以及會一路影響後續實作的三個關鍵決策。
                  </p>
                </div>
              </div>

              <div class="grid gap-4 lg:grid-cols-[1.05fr_0.95fr]">
                <div
                  v-for="card in summaryCards"
                  :key="card.title.en"
                  :class="card.kind === 'body'
                    ? 'rounded-[28px] bg-slate-950 p-6 text-slate-50 shadow-lg shadow-slate-200/20'
                    : 'rounded-[28px] border border-sky-100 bg-sky-50/70 p-6'"
                >
                  <div class="space-y-1">
                    <p :class="card.kind === 'body' ? 'text-lg font-semibold text-white' : 'text-lg font-semibold text-slate-950'">
                      {{ card.title.en }}
                    </p>
                    <p
                      lang="zh-Hant"
                      :class="card.kind === 'body' ? 'text-sm font-medium text-slate-300' : 'text-sm font-medium text-sky-800'"
                    >
                      {{ card.title.zhTw }}
                    </p>
                  </div>

                  <div
                    v-if="card.kind === 'body'"
                    class="mt-5 space-y-3"
                  >
                    <p class="text-sm leading-7 text-slate-200 md:text-[15px]">{{ card.body.en }}</p>
                    <p lang="zh-Hant" class="text-sm leading-7 text-slate-300 md:text-[15px]">{{ card.body.zhTw }}</p>
                  </div>

                  <ol
                    v-else
                    class="mt-5 space-y-3"
                  >
                    <li
                      v-for="(point, index) in card.points"
                      :key="point.en"
                      class="flex gap-3 rounded-2xl bg-white px-4 py-4 shadow-sm shadow-sky-100/70"
                    >
                      <span class="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-sky-600 text-xs font-semibold text-white">
                        {{ index + 1 }}
                      </span>
                      <div class="space-y-1">
                        <p class="text-sm leading-7 text-slate-700">{{ point.en }}</p>
                        <p lang="zh-Hant" class="text-sm leading-7 text-slate-500">{{ point.zhTw }}</p>
                      </div>
                    </li>
                  </ol>
                </div>
              </div>
            </div>
          </section>

          <section
            id="requirements"
            class="scroll-mt-24 rounded-[28px] border border-slate-200/70 bg-white/90 p-6 shadow-[0_24px_60px_-50px_rgba(15,23,42,0.45)] backdrop-blur md:p-8"
          >
            <div class="space-y-6">
              <div class="space-y-3">
                <p class="text-sm font-semibold uppercase tracking-[0.2em] text-sky-600">Requirements</p>
                <h2 class="text-3xl font-semibold tracking-tight text-slate-950">Separate what the system does from what pressures it</h2>
                <p class="max-w-3xl text-sm leading-7 text-slate-700 md:text-base">
                  Functional requirements define the user-facing surface. Non-functional requirements define the real
                  engineering pressure around latency, availability, and scale.
                </p>
                <p lang="zh-Hant" class="max-w-3xl text-sm leading-7 text-slate-500 md:text-base">
                  功能需求描述產品表面要做什麼；非功能需求則決定真正的工程壓力，像是延遲、可用性與規模。
                </p>
              </div>

              <div class="grid gap-4 xl:grid-cols-2">
                <div
                  v-for="(section, index) in requirements"
                  :key="section.title.en"
                  :class="index === 0
                    ? 'rounded-[28px] border border-sky-100 bg-gradient-to-br from-sky-50 to-white p-6'
                    : 'rounded-[28px] border border-violet-100 bg-gradient-to-br from-violet-50 to-white p-6'"
                >
                  <div class="space-y-1">
                    <p class="text-lg font-semibold text-slate-950">{{ section.title.en }}</p>
                    <p lang="zh-Hant" class="text-sm font-medium text-slate-500">{{ section.title.zhTw }}</p>
                  </div>

                  <ul class="mt-5 space-y-3">
                    <li
                      v-for="point in section.points"
                      :key="point.en"
                      class="rounded-2xl border border-white/80 bg-white/95 px-4 py-4 shadow-sm shadow-slate-100"
                    >
                      <p class="text-sm leading-7 text-slate-700">{{ point.en }}</p>
                      <p lang="zh-Hant" class="mt-1 text-sm leading-7 text-slate-500">{{ point.zhTw }}</p>
                    </li>
                  </ul>
                </div>
              </div>
            </div>
          </section>

          <section
            id="api-design"
            class="scroll-mt-24 rounded-[28px] border border-slate-200/70 bg-white/90 p-6 shadow-[0_24px_60px_-50px_rgba(15,23,42,0.45)] backdrop-blur md:p-8"
          >
            <div class="space-y-6">
              <div class="space-y-3">
                <p class="text-sm font-semibold uppercase tracking-[0.2em] text-sky-600">API Design</p>
                <h2 class="text-3xl font-semibold tracking-tight text-slate-950">Small API surface, high operational impact</h2>
                <p class="max-w-3xl text-sm leading-7 text-slate-700 md:text-base">
                  The endpoint list is short, but every route carries consequences for uniqueness, cache invalidation,
                  asset delivery, and redirect freshness.
                </p>
                <p lang="zh-Hant" class="max-w-3xl text-sm leading-7 text-slate-500 md:text-base">
                  端點看似不多，但每一條路由都會牽動唯一性、快取失效、靜態資產傳遞，以及 redirect 的新鮮度。
                </p>
              </div>

              <div class="grid gap-4 xl:grid-cols-2">
                <div
                  v-for="endpoint in apiEndpoints"
                  :key="`${endpoint.method}-${endpoint.path}`"
                  :class="endpoint.method === 'GET'
                    ? 'rounded-[28px] border border-blue-100 bg-blue-50/40 p-5'
                    : endpoint.method === 'POST'
                      ? 'rounded-[28px] border border-emerald-100 bg-emerald-50/50 p-5'
                      : endpoint.method === 'PUT'
                        ? 'rounded-[28px] border border-amber-100 bg-amber-50/50 p-5'
                        : 'rounded-[28px] border border-rose-100 bg-rose-50/50 p-5'"
                >
                  <div class="space-y-4">
                    <div class="flex flex-wrap items-center gap-2">
                      <a-tag
                        :color="endpoint.method === 'GET'
                          ? 'blue'
                          : endpoint.method === 'POST'
                            ? 'green'
                            : endpoint.method === 'PUT'
                              ? 'orange'
                              : 'red'"
                      >
                        {{ endpoint.method }}
                      </a-tag>
                      <code class="rounded-full bg-white px-3 py-1 text-[11px] font-medium text-slate-700 shadow-sm">
                        {{ endpoint.path }}
                      </code>
                    </div>

                    <div class="space-y-1">
                      <p class="text-base font-semibold text-slate-900">{{ endpoint.summary.en }}</p>
                      <p lang="zh-Hant" class="text-sm leading-7 text-slate-500">{{ endpoint.summary.zhTw }}</p>
                    </div>

                    <div
                      v-if="endpoint.requestExample"
                      class="overflow-x-auto rounded-2xl bg-slate-950 p-4 text-xs leading-6 text-slate-100 shadow-inner"
                    >
                      <p class="mb-1 text-slate-300">Request</p>
                      <p lang="zh-Hant" class="mb-2 text-slate-400">請求範例</p>
                      <pre>{{ endpoint.requestExample }}</pre>
                    </div>

                    <div
                      v-if="endpoint.responseExample"
                      class="overflow-x-auto rounded-2xl bg-slate-950 p-4 text-xs leading-6 text-slate-100 shadow-inner"
                    >
                      <p class="mb-1 text-slate-300">Response</p>
                      <p lang="zh-Hant" class="mb-2 text-slate-400">回應範例</p>
                      <pre>{{ endpoint.responseExample }}</pre>
                    </div>

                    <ul class="space-y-3">
                      <li
                        v-for="note in endpoint.notes"
                        :key="note.en"
                        class="rounded-2xl bg-white px-4 py-4 shadow-sm shadow-slate-100"
                      >
                        <p class="text-sm leading-7 text-slate-700">{{ note.en }}</p>
                        <p lang="zh-Hant" class="mt-1 text-sm leading-7 text-slate-500">{{ note.zhTw }}</p>
                      </li>
                    </ul>
                  </div>
                </div>
              </div>
            </div>
          </section>

          <section
            id="flows"
            class="scroll-mt-24 rounded-[28px] border border-slate-200/70 bg-white/90 p-6 shadow-[0_24px_60px_-50px_rgba(15,23,42,0.45)] backdrop-blur md:p-8"
          >
            <div class="space-y-6">
              <div class="space-y-3">
                <p class="text-sm font-semibold uppercase tracking-[0.2em] text-sky-600">Core Flows</p>
                <h2 class="text-3xl font-semibold tracking-tight text-slate-950">Follow write-path logic and read-path pressure separately</h2>
                <p class="max-w-3xl text-sm leading-7 text-slate-700 md:text-base">
                  These flows show where validation, persistence, caching, and redirect behavior start to matter.
                </p>
                <p lang="zh-Hant" class="max-w-3xl text-sm leading-7 text-slate-500 md:text-base">
                  這兩條流程把驗證、落地儲存、快取與 redirect 行為拆開來看，會更容易理解整體壓力來源。
                </p>
              </div>

              <div class="grid gap-4 xl:grid-cols-2">
                <div
                  v-for="flow in flows"
                  :key="flow.title.en"
                  class="rounded-[28px] border border-slate-200 bg-slate-50/70 p-6"
                >
                  <div class="space-y-1">
                    <p class="text-lg font-semibold text-slate-950">{{ flow.title.en }}</p>
                    <p lang="zh-Hant" class="text-sm font-medium text-slate-500">{{ flow.title.zhTw }}</p>
                  </div>

                  <div class="mt-5 rounded-2xl border border-amber-100 bg-amber-50 px-4 py-4">
                    <p class="text-sm font-medium leading-7 text-amber-900">{{ flow.emphasis.en }}</p>
                    <p lang="zh-Hant" class="mt-1 text-sm leading-7 text-amber-700">{{ flow.emphasis.zhTw }}</p>
                  </div>

                  <ol class="mt-5 space-y-4">
                    <li
                      v-for="(step, index) in flow.steps"
                      :key="step.en"
                      class="flex gap-4"
                    >
                      <span class="mt-1 flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-slate-900 text-xs font-semibold text-white">
                        {{ index + 1 }}
                      </span>
                      <div class="min-w-0 rounded-2xl bg-white px-4 py-4 shadow-sm shadow-slate-100">
                        <p class="text-sm leading-7 text-slate-700">{{ step.en }}</p>
                        <p lang="zh-Hant" class="mt-1 text-sm leading-7 text-slate-500">{{ step.zhTw }}</p>
                      </div>
                    </li>
                  </ol>
                </div>
              </div>

              <div class="grid gap-4 md:grid-cols-2">
                <div class="md:col-span-2 space-y-1">
                  <p class="text-sm font-semibold uppercase tracking-[0.18em] text-slate-500">{{ redirectComparison.title.en }}</p>
                  <p lang="zh-Hant" class="text-sm leading-6 text-slate-500">{{ redirectComparison.title.zhTw }}</p>
                </div>

                <div class="rounded-[28px] border border-slate-200 bg-slate-50/70 p-6">
                  <div class="space-y-1">
                    <h3 class="text-lg font-semibold text-slate-900">{{ redirectComparison.permanent.title.en }}</h3>
                    <p lang="zh-Hant" class="text-sm font-medium text-slate-500">{{ redirectComparison.permanent.title.zhTw }}</p>
                  </div>
                  <div class="mt-4 rounded-2xl border border-slate-200 bg-white px-4 py-4">
                    <p class="text-sm leading-7 text-slate-700">{{ redirectComparison.permanent.body.en }}</p>
                    <p lang="zh-Hant" class="mt-1 text-sm leading-7 text-slate-500">{{ redirectComparison.permanent.body.zhTw }}</p>
                  </div>
                </div>

                <div class="rounded-[28px] border border-blue-200 bg-blue-50/70 p-6">
                  <div class="space-y-1">
                    <h3 class="text-lg font-semibold text-blue-950">{{ redirectComparison.temporary.title.en }}</h3>
                    <p lang="zh-Hant" class="text-sm font-medium text-blue-700">{{ redirectComparison.temporary.title.zhTw }}</p>
                  </div>
                  <div class="mt-4 rounded-2xl border border-blue-100 bg-white/90 px-4 py-4">
                    <p class="text-sm leading-7 text-blue-900">{{ redirectComparison.temporary.body.en }}</p>
                    <p lang="zh-Hant" class="mt-1 text-sm leading-7 text-blue-700">{{ redirectComparison.temporary.body.zhTw }}</p>
                  </div>
                </div>
              </div>
            </div>
          </section>

          <section
            id="deep-dives"
            class="scroll-mt-24 rounded-[28px] border border-slate-200/70 bg-white/90 p-6 shadow-[0_24px_60px_-50px_rgba(15,23,42,0.45)] backdrop-blur md:p-8"
          >
            <div class="space-y-6">
              <div class="space-y-3">
                <p class="text-sm font-semibold uppercase tracking-[0.2em] text-sky-600">Deep Dives</p>
                <h2 class="text-3xl font-semibold tracking-tight text-slate-950">Focus on the decisions that change operability over time</h2>
                <p class="max-w-3xl text-sm leading-7 text-slate-700 md:text-base">
                  These are the questions that usually decide whether the service stays fast, correct, and maintainable
                  after the first launch.
                </p>
                <p lang="zh-Hant" class="max-w-3xl text-sm leading-7 text-slate-500 md:text-base">
                  這些題目通常決定系統在上線之後，能不能持續維持速度、正確性，以及可維運性。
                </p>
              </div>

              <div class="space-y-4">
                <div
                  v-for="section in deepDives"
                  :key="section.title.en"
                  class="rounded-[28px] border border-slate-200 bg-slate-50/70 p-6"
                >
                  <div class="space-y-1">
                    <p class="text-lg font-semibold text-slate-950">{{ section.title.en }}</p>
                    <p lang="zh-Hant" class="text-sm font-medium text-slate-500">{{ section.title.zhTw }}</p>
                  </div>

                  <div class="mt-5 grid gap-4 xl:grid-cols-[0.9fr_1.1fr_1fr]">
                    <div class="rounded-2xl border border-slate-200 bg-white px-4 py-4 shadow-sm shadow-slate-100">
                      <div class="space-y-1">
                        <p class="text-sm font-semibold text-slate-900">Problem</p>
                        <p lang="zh-Hant" class="text-sm font-medium text-slate-500">問題</p>
                      </div>
                      <p class="mt-3 text-sm leading-7 text-slate-700">{{ section.problem.en }}</p>
                      <p lang="zh-Hant" class="mt-1 text-sm leading-7 text-slate-500">{{ section.problem.zhTw }}</p>
                    </div>

                    <div class="rounded-2xl border border-slate-200 bg-white px-4 py-4 shadow-sm shadow-slate-100">
                      <div class="space-y-1">
                        <p class="text-sm font-semibold text-slate-900">Design Decisions</p>
                        <p lang="zh-Hant" class="text-sm font-medium text-slate-500">設計決策</p>
                      </div>
                      <ul class="mt-3 space-y-3">
                        <li
                          v-for="item in section.decisions"
                          :key="item.en"
                          class="rounded-2xl bg-slate-50 px-4 py-4"
                        >
                          <p class="text-sm leading-7 text-slate-700">{{ item.en }}</p>
                          <p lang="zh-Hant" class="mt-1 text-sm leading-7 text-slate-500">{{ item.zhTw }}</p>
                        </li>
                      </ul>
                    </div>

                    <div class="rounded-2xl border border-slate-200 bg-white px-4 py-4 shadow-sm shadow-slate-100">
                      <div class="space-y-1">
                        <p class="text-sm font-semibold text-slate-900">Trade-offs</p>
                        <p lang="zh-Hant" class="text-sm font-medium text-slate-500">取捨</p>
                      </div>
                      <ul class="mt-3 space-y-3">
                        <li
                          v-for="item in section.tradeoffs"
                          :key="item.en"
                          class="rounded-2xl bg-slate-50 px-4 py-4"
                        >
                          <p class="text-sm leading-7 text-slate-700">{{ item.en }}</p>
                          <p lang="zh-Hant" class="mt-1 text-sm leading-7 text-slate-500">{{ item.zhTw }}</p>
                        </li>
                      </ul>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </section>

          <section
            id="scaling"
            class="scroll-mt-24 rounded-[28px] border border-slate-200/70 bg-white/90 p-6 shadow-[0_24px_60px_-50px_rgba(15,23,42,0.45)] backdrop-blur md:p-8"
          >
            <div class="space-y-6">
              <div class="space-y-3">
                <p class="text-sm font-semibold uppercase tracking-[0.2em] text-sky-600">Scaling / Decisions</p>
                <h2 class="text-3xl font-semibold tracking-tight text-slate-950">Scale planning starts from the redirect path</h2>
                <p class="max-w-3xl text-sm leading-7 text-slate-700 md:text-base">
                  This is a read-heavy system, so scaling starts with redirect traffic, cache hit rates, and static
                  asset delivery—not with the QR image generator itself.
                </p>
                <p lang="zh-Hant" class="max-w-3xl text-sm leading-7 text-slate-500 md:text-base">
                  這是一個讀多寫少的系統，所以擴充策略要先從 redirect 流量、快取命中率與靜態資產傳遞出發，而不是從產圖服務本身開始。
                </p>
              </div>

              <div class="grid gap-4 xl:grid-cols-[1.05fr_0.95fr]">
                <div class="space-y-4">
                  <div class="rounded-[28px] border border-slate-200 bg-slate-50/70 p-6">
                    <div class="space-y-1">
                      <p class="text-lg font-semibold text-slate-950">Capacity Thinking</p>
                      <p lang="zh-Hant" class="text-sm font-medium text-slate-500">容量思考</p>
                    </div>
                    <p class="mt-4 text-sm leading-7 text-slate-700 md:text-[15px]">{{ scalingContent.capacity.en }}</p>
                    <p lang="zh-Hant" class="mt-1 text-sm leading-7 text-slate-500 md:text-[15px]">{{ scalingContent.capacity.zhTw }}</p>
                  </div>

                  <div class="rounded-[28px] border border-slate-200 bg-slate-950 p-6 text-slate-100 shadow-[0_20px_60px_-45px_rgba(15,23,42,0.85)]">
                    <div class="space-y-1">
                      <p class="text-sm font-semibold text-white">Architecture Snapshot</p>
                      <p lang="zh-Hant" class="text-sm font-medium text-slate-300">架構快照</p>
                    </div>
                    <pre class="mt-4 overflow-x-auto rounded-2xl border border-white/10 bg-white/5 p-4 text-xs leading-6 text-slate-100">{{ architectureSnapshot }}</pre>
                    <p class="mt-4 text-xs leading-6 text-slate-300">{{ scalingContent.snapshotNote.en }}</p>
                    <p lang="zh-Hant" class="text-xs leading-6 text-slate-400">{{ scalingContent.snapshotNote.zhTw }}</p>
                  </div>
                </div>

                <div class="rounded-[28px] border border-slate-200 bg-slate-50/70 p-6">
                  <div class="space-y-1">
                    <p class="text-lg font-semibold text-slate-950">Key Takeaways</p>
                    <p lang="zh-Hant" class="text-sm font-medium text-slate-500">關鍵結論</p>
                  </div>
                  <ul class="mt-5 space-y-3">
                    <li
                      v-for="point in scalingContent.takeaways"
                      :key="point.en"
                      class="rounded-2xl border border-white/80 bg-white px-4 py-4 shadow-sm shadow-slate-100"
                    >
                      <p class="text-sm leading-7 text-slate-700">{{ point.en }}</p>
                      <p lang="zh-Hant" class="mt-1 text-sm leading-7 text-slate-500">{{ point.zhTw }}</p>
                    </li>
                  </ul>
                </div>
              </div>
            </div>
          </section>
        </div>

        <aside class="hidden space-y-4 xl:sticky xl:top-6 xl:block">
          <div class="rounded-[28px] border border-slate-200/70 bg-white/90 p-5 shadow-[0_24px_60px_-50px_rgba(15,23,42,0.45)] backdrop-blur">
            <div class="space-y-1">
              <p class="text-sm font-semibold text-slate-900">Page Map</p>
              <p lang="zh-Hant" class="text-sm leading-6 text-slate-500">頁面導覽</p>
            </div>
            <div class="mt-4 space-y-2">
              <a
                v-for="link in sectionLinks"
                :key="link.id"
                :href="`#${link.id}`"
                class="block cursor-pointer rounded-2xl border border-slate-200 bg-slate-50/70 px-4 py-3 transition hover:border-sky-300 hover:bg-sky-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-sky-500"
              >
                <p class="text-sm font-semibold text-slate-800">{{ link.title.en }}</p>
                <p lang="zh-Hant" class="mt-1 text-sm leading-6 text-slate-500">{{ link.title.zhTw }}</p>
              </a>
            </div>
          </div>

          <div class="rounded-[28px] border border-slate-200/70 bg-white/90 p-5 shadow-[0_24px_60px_-50px_rgba(15,23,42,0.45)] backdrop-blur">
            <div class="space-y-1">
              <p class="text-sm font-semibold text-slate-900">System pressure points</p>
              <p lang="zh-Hant" class="text-sm leading-6 text-slate-500">先看的三個系統壓力</p>
            </div>

            <div class="mt-4 space-y-3">
              <div
                v-for="metric in metricCards"
                :key="metric.label.en"
                class="rounded-2xl border border-slate-200 bg-slate-50/70 px-4 py-4"
              >
                <div class="space-y-1">
                  <p class="text-xs font-semibold uppercase tracking-[0.18em] text-slate-500">{{ metric.label.en }}</p>
                  <p lang="zh-Hant" class="text-sm text-slate-500">{{ metric.label.zhTw }}</p>
                </div>
                <div class="mt-3 space-y-1">
                  <p class="text-lg font-semibold text-slate-950">{{ metric.value.en }}</p>
                  <p lang="zh-Hant" class="text-sm font-medium text-slate-500">{{ metric.value.zhTw }}</p>
                </div>
                <div class="mt-3 space-y-1">
                  <p class="text-sm leading-7 text-slate-700">{{ metric.description.en }}</p>
                  <p lang="zh-Hant" class="text-sm leading-7 text-slate-500">{{ metric.description.zhTw }}</p>
                </div>
              </div>
            </div>
          </div>

          <div class="rounded-[28px] border border-slate-200/70 bg-slate-950 p-5 text-slate-50 shadow-[0_24px_60px_-50px_rgba(15,23,42,0.7)]">
            <div class="space-y-1">
              <p class="text-sm font-semibold text-white">How to read this page</p>
              <p lang="zh-Hant" class="text-sm leading-6 text-slate-300">閱讀建議</p>
            </div>
            <div class="mt-4 space-y-4">
              <div
                v-for="item in heroChecklist"
                :key="`${item.en}-aside`"
                class="space-y-1 border-b border-white/10 pb-4 last:border-b-0 last:pb-0"
              >
                <p class="text-sm font-medium leading-6 text-slate-100">{{ item.en }}</p>
                <p lang="zh-Hant" class="text-sm leading-6 text-slate-300">{{ item.zhTw }}</p>
              </div>
            </div>
          </div>
        </aside>
      </div>
    </div>
  </div>
</template>

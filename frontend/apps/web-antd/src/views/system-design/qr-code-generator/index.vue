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

const summaryCards = [
  {
    title: copy('What problem is this design solving?', '這個設計在解什麼問題？'),
    body: copy(
      'The service accepts a URL, creates a globally unique qr_token, and generates the corresponding QR code. When the QR code is scanned, the system uses that token to resolve the original URL and returns a redirect.',
      '這個服務接收一個 URL，建立全球唯一的 qr_token，並產生對應的 QR Code。當使用者掃描後，系統再用該 token 找回原始網址並完成重新導向。',
    ),
  },
  {
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
</script>

<template>
  <div
    lang="en"
    class="mx-auto max-w-7xl space-y-8 p-5"
  >
    <a-alert
      type="info"
      show-icon
      class="rounded-2xl border border-sky-100"
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

    <section class="space-y-6">
      <div class="overflow-hidden rounded-3xl bg-slate-950 text-slate-50 shadow-sm">
        <div class="grid gap-6 px-6 py-8 lg:grid-cols-[1.2fr_0.8fr] lg:px-8 lg:py-10">
          <div class="space-y-5">
            <div class="space-y-2">
              <p class="text-sm font-medium uppercase tracking-[0.2em] text-sky-300">
                System Design Case Study
              </p>
              <p lang="zh-Hant" class="text-sm leading-6 text-slate-300">系統設計案例研究</p>
            </div>

            <div class="space-y-3">
              <h1 class="text-3xl font-semibold md:text-4xl">QR Code Generator</h1>
              <p class="max-w-4xl text-base leading-8 text-slate-100">
                The design treats a QR code generator as a service that accepts a URL, creates a QR image, and
                redirects through a tokenized endpoint. The real difficulty is not image generation itself, but
                <span class="font-semibold text-white">
                  unique token creation, low-latency redirects, caching strategy, and large-scale growth
                </span>.
              </p>
              <p lang="zh-Hant" class="max-w-4xl text-sm leading-7 text-slate-300">
                這個設計把 QR Code 產生器視為一個服務：接收 URL、建立 QR 圖像，並透過 token 化端點完成
                redirect。真正困難的不是產圖本身，而是
                <span lang="zh-Hant" class="font-semibold text-slate-100">
                  唯一 token 的產生、低延遲重新導向、快取策略，以及大規模成長
                </span>。
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

          <div class="rounded-2xl border border-white/10 bg-white/5 p-5">
            <div class="space-y-1">
              <p class="text-sm font-semibold text-white">How to read this page</p>
              <p lang="zh-Hant" class="text-sm leading-6 text-slate-300">這頁怎麼讀最快</p>
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
        </div>
      </div>

      <div class="grid gap-4 md:grid-cols-3">
        <a-card
          v-for="metric in metricCards"
          :key="metric.label.en"
          class="h-full rounded-2xl shadow-sm"
          size="small"
        >
          <div class="space-y-3">
            <div class="space-y-1">
              <p class="text-sm font-medium text-slate-500">{{ metric.label.en }}</p>
               <p lang="zh-Hant" class="text-sm text-slate-500">{{ metric.label.zhTw }}</p>
            </div>
            <div class="space-y-1">
              <p class="text-xl font-semibold text-slate-900">{{ metric.value.en }}</p>
              <p lang="zh-Hant" class="text-sm font-medium text-slate-500">{{ metric.value.zhTw }}</p>
            </div>
            <div class="space-y-1">
              <p class="text-sm leading-7 text-slate-700">{{ metric.description.en }}</p>
              <p lang="zh-Hant" class="text-sm leading-7 text-slate-500">{{ metric.description.zhTw }}</p>
            </div>
          </div>
        </a-card>
      </div>
    </section>

    <section
      id="summary"
      class="space-y-4"
    >
      <div class="space-y-2">
        <p class="text-sm font-semibold uppercase tracking-wide text-blue-600">Executive Summary</p>
        <h2 class="text-2xl font-semibold text-slate-900">Read the system in one pass</h2>
        <p class="max-w-4xl text-sm leading-7 text-slate-700">
          Use this section to understand the problem framing, the page structure, and the three decisions that
          drive the rest of the design.
        </p>
        <p lang="zh-Hant" class="max-w-4xl text-sm leading-7 text-slate-500">
          這一節的目的是先讓你快速抓到問題定義、頁面結構，以及驅動整份設計的三個核心決策。
        </p>
      </div>

      <a-card class="rounded-2xl shadow-sm">
        <div class="space-y-4">
          <div class="space-y-1">
            <p class="text-sm font-semibold text-slate-900">Page Map</p>
            <p lang="zh-Hant" class="text-sm leading-6 text-slate-500">頁面導覽</p>
          </div>

          <div class="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
            <a
              v-for="link in sectionLinks"
              :key="link.id"
              :href="`#${link.id}`"
              class="block cursor-pointer rounded-2xl border border-slate-200 p-4 transition hover:border-blue-300 hover:bg-blue-50/40 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-blue-500"
            >
              <p class="text-sm font-semibold text-slate-800">{{ link.title.en }}</p>
              <p lang="zh-Hant" class="mt-1 text-sm leading-6 text-slate-500">{{ link.title.zhTw }}</p>
            </a>
          </div>
        </div>
      </a-card>

      <div class="grid gap-4 lg:grid-cols-2">
        <a-card
          v-for="card in summaryCards"
          :key="card.title.en"
          class="h-full rounded-2xl shadow-sm"
        >
          <template #title>
            <div class="space-y-1">
              <p class="text-base font-semibold text-slate-900">{{ card.title.en }}</p>
              <p lang="zh-Hant" class="text-sm font-medium text-slate-500">{{ card.title.zhTw }}</p>
            </div>
          </template>

          <div
            v-if="'body' in card"
            class="space-y-2"
          >
            <p class="text-sm leading-7 text-slate-700">{{ card.body.en }}</p>
            <p lang="zh-Hant" class="text-sm leading-7 text-slate-500">{{ card.body.zhTw }}</p>
          </div>

          <ul
            v-else
            class="space-y-3"
          >
            <li
              v-for="point in card.points"
              :key="point.en"
              class="rounded-xl bg-slate-50 p-4"
            >
              <p class="text-sm leading-7 text-slate-700">{{ point.en }}</p>
              <p lang="zh-Hant" class="mt-1 text-sm leading-7 text-slate-500">{{ point.zhTw }}</p>
            </li>
          </ul>
        </a-card>
      </div>
    </section>

    <section
      id="requirements"
      class="space-y-4"
    >
      <div class="space-y-2">
        <p class="text-sm font-semibold uppercase tracking-wide text-blue-600">Requirements</p>
        <h2 class="text-2xl font-semibold text-slate-900">Separate what the system must do from how well it must do it</h2>
        <p class="max-w-4xl text-sm leading-7 text-slate-700">
          The functional requirements define the product surface, while the non-functional requirements set the
          real design pressure around availability, latency, and scale.
        </p>
        <p lang="zh-Hant" class="max-w-4xl text-sm leading-7 text-slate-500">
          功能需求決定產品表面長相，非功能需求則決定真正的設計壓力：可用性、延遲與擴充規模。
        </p>
      </div>

      <div class="grid gap-4 xl:grid-cols-2">
        <a-card
          v-for="section in requirements"
          :key="section.title.en"
          class="rounded-2xl shadow-sm"
        >
          <template #title>
            <div class="space-y-1">
              <p class="text-base font-semibold text-slate-900">{{ section.title.en }}</p>
              <p lang="zh-Hant" class="text-sm font-medium text-slate-500">{{ section.title.zhTw }}</p>
            </div>
          </template>

          <ul class="space-y-3">
            <li
              v-for="point in section.points"
              :key="point.en"
              class="rounded-xl bg-slate-50 p-4"
            >
              <p class="text-sm leading-7 text-slate-700">{{ point.en }}</p>
              <p lang="zh-Hant" class="mt-1 text-sm leading-7 text-slate-500">{{ point.zhTw }}</p>
            </li>
          </ul>
        </a-card>
      </div>
    </section>

    <section
      id="api-design"
      class="space-y-4"
    >
      <div class="space-y-2">
        <p class="text-sm font-semibold uppercase tracking-wide text-blue-600">API Design</p>
        <h2 class="text-2xl font-semibold text-slate-900">Small API surface, high operational impact</h2>
        <p class="max-w-4xl text-sm leading-7 text-slate-700">
          The API list is short, but each endpoint carries important behavior around uniqueness, cache invalidation,
          static asset delivery, and redirect freshness.
        </p>
        <p lang="zh-Hant" class="max-w-4xl text-sm leading-7 text-slate-500">
          API 面向看起來不多，但每個端點都帶著關鍵行為：唯一性、快取失效、靜態資產傳遞，以及 redirect 的新鮮度。
        </p>
      </div>

      <a-card class="rounded-2xl shadow-sm">
        <template #title>
          <div class="space-y-1">
            <p class="text-base font-semibold text-slate-900">API Design</p>
            <p lang="zh-Hant" class="text-sm font-medium text-slate-500">API 設計</p>
          </div>
        </template>

        <div class="grid gap-4 xl:grid-cols-2">
          <a-card
            v-for="endpoint in apiEndpoints"
            :key="`${endpoint.method}-${endpoint.path}`"
            class="h-full rounded-2xl border border-slate-200"
            size="small"
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
                <code class="rounded bg-slate-100 px-2 py-1 text-xs text-slate-700">
                  {{ endpoint.path }}
                </code>
              </div>

              <div class="space-y-1">
                <p class="text-sm font-medium leading-7 text-slate-800">{{ endpoint.summary.en }}</p>
                <p lang="zh-Hant" class="text-sm leading-7 text-slate-500">{{ endpoint.summary.zhTw }}</p>
              </div>

              <div
                v-if="endpoint.requestExample"
                class="overflow-x-auto rounded-2xl bg-slate-950 p-4 text-xs leading-6 text-slate-100"
              >
                <p class="mb-1 text-slate-300">Request</p>
                <p lang="zh-Hant" class="mb-2 text-slate-400">請求範例</p>
                <pre>{{ endpoint.requestExample }}</pre>
              </div>

              <div
                v-if="endpoint.responseExample"
                class="overflow-x-auto rounded-2xl bg-slate-950 p-4 text-xs leading-6 text-slate-100"
              >
                <p class="mb-1 text-slate-300">Response</p>
                <p lang="zh-Hant" class="mb-2 text-slate-400">回應範例</p>
                <pre>{{ endpoint.responseExample }}</pre>
              </div>

              <ul class="space-y-3">
                <li
                  v-for="note in endpoint.notes"
                  :key="note.en"
                  class="rounded-xl bg-slate-50 p-4"
                >
                  <p class="text-sm leading-7 text-slate-700">{{ note.en }}</p>
                  <p lang="zh-Hant" class="mt-1 text-sm leading-7 text-slate-500">{{ note.zhTw }}</p>
                </li>
              </ul>
            </div>
          </a-card>
        </div>
      </a-card>
    </section>

    <section
      id="flows"
      class="space-y-4"
    >
      <div class="space-y-2">
        <p class="text-sm font-semibold uppercase tracking-wide text-blue-600">Core Flows</p>
        <h2 class="text-2xl font-semibold text-slate-900">Follow the write path and the read path separately</h2>
        <p class="max-w-4xl text-sm leading-7 text-slate-700">
          These flows show where validation, storage, caching, and redirect behavior matter most.
        </p>
        <p lang="zh-Hant" class="max-w-4xl text-sm leading-7 text-slate-500">
          這兩條流程分別標出驗證、儲存、快取與 redirect 行為最重要的節點。
        </p>
      </div>

      <div class="grid gap-4 xl:grid-cols-2">
        <a-card
          v-for="flow in flows"
          :key="flow.title.en"
          class="rounded-2xl shadow-sm"
        >
          <template #title>
            <div class="space-y-1">
              <p class="text-base font-semibold text-slate-900">{{ flow.title.en }}</p>
              <p lang="zh-Hant" class="text-sm font-medium text-slate-500">{{ flow.title.zhTw }}</p>
            </div>
          </template>

          <p class="mb-4 rounded-xl bg-amber-50 px-4 py-3 text-sm leading-7 text-amber-800">
            <span class="font-medium">{{ flow.emphasis.en }}</span>
            <span lang="zh-Hant" class="mt-1 block text-amber-700">{{ flow.emphasis.zhTw }}</span>
          </p>

          <ol class="space-y-3">
            <li
              v-for="step in flow.steps"
              :key="step.en"
              class="rounded-xl bg-slate-50 p-4"
            >
              <p class="text-sm leading-7 text-slate-700">{{ step.en }}</p>
              <p lang="zh-Hant" class="mt-1 text-sm leading-7 text-slate-500">{{ step.zhTw }}</p>
            </li>
          </ol>
        </a-card>
      </div>

      <a-card class="rounded-2xl shadow-sm">
        <template #title>
          <div class="space-y-1">
            <p class="text-base font-semibold text-slate-900">{{ redirectComparison.title.en }}</p>
            <p lang="zh-Hant" class="text-sm font-medium text-slate-500">{{ redirectComparison.title.zhTw }}</p>
          </div>
        </template>

        <div class="grid gap-4 md:grid-cols-2">
          <div class="rounded-2xl border border-slate-200 p-5">
            <div class="space-y-1">
              <h3 class="text-base font-semibold text-slate-900">{{ redirectComparison.permanent.title.en }}</h3>
              <p lang="zh-Hant" class="text-sm font-medium text-slate-500">{{ redirectComparison.permanent.title.zhTw }}</p>
            </div>
            <div class="mt-3 space-y-1">
              <p class="text-sm leading-7 text-slate-700">{{ redirectComparison.permanent.body.en }}</p>
              <p lang="zh-Hant" class="text-sm leading-7 text-slate-500">{{ redirectComparison.permanent.body.zhTw }}</p>
            </div>
          </div>

          <div class="rounded-2xl border border-blue-200 bg-blue-50 p-5">
            <div class="space-y-1">
              <h3 class="text-base font-semibold text-blue-900">{{ redirectComparison.temporary.title.en }}</h3>
              <p lang="zh-Hant" class="text-sm font-medium text-blue-700">{{ redirectComparison.temporary.title.zhTw }}</p>
            </div>
            <div class="mt-3 space-y-1">
              <p class="text-sm leading-7 text-blue-900">{{ redirectComparison.temporary.body.en }}</p>
              <p lang="zh-Hant" class="text-sm leading-7 text-blue-700">{{ redirectComparison.temporary.body.zhTw }}</p>
            </div>
          </div>
        </div>
      </a-card>
    </section>

    <section
      id="deep-dives"
      class="space-y-4"
    >
      <div class="space-y-2">
        <p class="text-sm font-semibold uppercase tracking-wide text-blue-600">Deep Dives</p>
        <h2 class="text-2xl font-semibold text-slate-900">Focus on the questions that decide long-term operability</h2>
        <p class="max-w-4xl text-sm leading-7 text-slate-700">
          These are the design questions that usually determine whether the service stays fast, correct, and
          maintainable at scale.
        </p>
        <p lang="zh-Hant" class="max-w-4xl text-sm leading-7 text-slate-500">
          這些問題通常決定服務在大規模下，是否仍能維持速度、正確性與可維運性。
        </p>
      </div>

      <div class="grid gap-4">
        <a-card
          v-for="section in deepDives"
          :key="section.title.en"
          class="rounded-2xl shadow-sm"
        >
          <template #title>
            <div class="space-y-1">
              <p class="text-base font-semibold text-slate-900">{{ section.title.en }}</p>
              <p lang="zh-Hant" class="text-sm font-medium text-slate-500">{{ section.title.zhTw }}</p>
            </div>
          </template>

          <div class="grid gap-4 xl:grid-cols-[1.2fr_1fr]">
            <div class="space-y-4">
              <div class="rounded-2xl border border-slate-200 bg-slate-50 p-4">
                <div class="space-y-1">
                  <p class="text-sm font-semibold text-slate-900">Problem</p>
                  <p lang="zh-Hant" class="text-sm font-medium text-slate-500">問題</p>
                </div>
                <p class="mt-3 text-sm leading-7 text-slate-700">{{ section.problem.en }}</p>
                <p lang="zh-Hant" class="mt-1 text-sm leading-7 text-slate-500">{{ section.problem.zhTw }}</p>
              </div>

              <div class="rounded-2xl border border-slate-200 p-4">
                <div class="space-y-1">
                  <p class="text-sm font-semibold text-slate-900">Design Decisions</p>
                  <p lang="zh-Hant" class="text-sm font-medium text-slate-500">設計決策</p>
                </div>
                <ul class="mt-3 space-y-3">
                  <li
                    v-for="item in section.decisions"
                    :key="item.en"
                    class="rounded-xl bg-slate-50 p-4"
                  >
                    <p class="text-sm leading-7 text-slate-700">{{ item.en }}</p>
                    <p lang="zh-Hant" class="mt-1 text-sm leading-7 text-slate-500">{{ item.zhTw }}</p>
                  </li>
                </ul>
              </div>
            </div>

            <div class="rounded-2xl border border-slate-200 p-4">
              <div class="space-y-1">
                <p class="text-sm font-semibold text-slate-900">Trade-offs</p>
                <p lang="zh-Hant" class="text-sm font-medium text-slate-500">取捨</p>
              </div>
              <ul class="mt-3 space-y-3">
                <li
                  v-for="item in section.tradeoffs"
                  :key="item.en"
                  class="rounded-xl bg-slate-50 p-4"
                >
                  <p class="text-sm leading-7 text-slate-700">{{ item.en }}</p>
                  <p lang="zh-Hant" class="mt-1 text-sm leading-7 text-slate-500">{{ item.zhTw }}</p>
                </li>
              </ul>
            </div>
          </div>
        </a-card>
      </div>
    </section>

    <section
      id="scaling"
      class="space-y-4"
    >
      <div class="space-y-2">
        <p class="text-sm font-semibold uppercase tracking-wide text-blue-600">Scaling / Decisions</p>
        <h2 class="text-2xl font-semibold text-slate-900">Scale planning starts from the redirect path</h2>
        <p class="max-w-4xl text-sm leading-7 text-slate-700">
          This system is read-heavy, so scale planning begins with redirect traffic, cache hit rates, and static
          asset delivery rather than with the QR image generator itself.
        </p>
        <p lang="zh-Hant" class="max-w-4xl text-sm leading-7 text-slate-500">
          這個系統屬於讀多寫少，因此擴容思考要從 redirect 流量、快取命中率與靜態資產傳遞開始，而不是從產圖服務本身開始。
        </p>
      </div>

      <a-card class="rounded-2xl shadow-sm">
        <template #title>
          <div class="space-y-1">
            <p class="text-base font-semibold text-slate-900">Scaling / Key Decisions</p>
            <p lang="zh-Hant" class="text-sm font-medium text-slate-500">擴充 / 關鍵決策</p>
          </div>
        </template>

        <div class="grid gap-4 xl:grid-cols-[1.1fr_0.9fr]">
          <div class="space-y-4">
            <div class="rounded-2xl border border-slate-200 bg-slate-50 p-4">
              <div class="space-y-1">
                <p class="text-sm font-semibold text-slate-900">Capacity Thinking</p>
                <p lang="zh-Hant" class="text-sm font-medium text-slate-500">容量思考</p>
              </div>
              <p class="mt-3 text-sm leading-7 text-slate-700">{{ scalingContent.capacity.en }}</p>
              <p lang="zh-Hant" class="mt-1 text-sm leading-7 text-slate-500">{{ scalingContent.capacity.zhTw }}</p>
            </div>

            <div class="overflow-x-auto rounded-2xl bg-slate-950 p-4 text-xs leading-6 text-slate-100">
              <p class="mb-1 text-slate-300">Architecture Snapshot</p>
              <p lang="zh-Hant" class="mb-2 text-slate-400">架構快照</p>
              <pre>Client -> QR Image / CDN
            -> https://myqrcode.com/{qr_token}
            -> App Server (stateless)
            -> Cache (hot mappings)
            -> DB / Read Replicas
            -> Original URL
            -> 302 Redirect</pre>
              <p class="mt-3 text-xs leading-6 text-slate-300">{{ scalingContent.snapshotNote.en }}</p>
              <p lang="zh-Hant" class="text-xs leading-6 text-slate-400">{{ scalingContent.snapshotNote.zhTw }}</p>
            </div>
          </div>

          <div class="rounded-2xl border border-slate-200 p-4">
            <div class="space-y-1">
              <p class="text-sm font-semibold text-slate-900">Key Takeaways</p>
              <p lang="zh-Hant" class="text-sm font-medium text-slate-500">關鍵結論</p>
            </div>
            <ul class="mt-3 space-y-3">
              <li
                v-for="point in scalingContent.takeaways"
                :key="point.en"
                class="rounded-xl bg-slate-50 p-4"
              >
                <p class="text-sm leading-7 text-slate-700">{{ point.en }}</p>
                <p lang="zh-Hant" class="mt-1 text-sm leading-7 text-slate-500">{{ point.zhTw }}</p>
              </li>
            </ul>
          </div>
        </div>
      </a-card>
    </section>
  </div>
</template>

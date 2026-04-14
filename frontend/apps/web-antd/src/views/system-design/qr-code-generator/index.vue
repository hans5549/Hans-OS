<script lang="ts" setup>
defineOptions({ name: 'QrCodeGeneratorPage' });

interface MetricCard {
  description: string;
  label: string;
  value: string;
}

interface RequirementCard {
  points: string[];
  title: string;
}

interface ApiEndpoint {
  method: 'DELETE' | 'GET' | 'POST' | 'PUT';
  notes: string[];
  path: string;
  requestExample?: string;
  responseExample?: string;
  summary: string;
}

interface FlowSection {
  emphasis: string;
  steps: string[];
  title: string;
}

interface DeepDiveSection {
  decisions: string[];
  problem: string;
  title: string;
  tradeoffs: string[];
}

interface SectionLink {
  id: string;
  title: string;
}

const sectionLinks: SectionLink[] = [
  { id: 'summary', title: 'Executive Summary' },
  { id: 'requirements', title: 'Requirements' },
  { id: 'api-design', title: 'API Design' },
  { id: 'flows', title: 'Core Flows' },
  { id: 'deep-dives', title: 'Deep Dives' },
  { id: 'scaling', title: 'Scaling / Decisions' },
];

const metricCards: MetricCard[] = [
  {
    label: 'Input Limit',
    value: 'ASCII URL <= 20 chars',
    description: 'Validate the submitted URL before creating a QR code and enforce the stated length limit.',
  },
  {
    label: 'Latency Goal',
    value: '< 100ms Redirect',
    description: 'Redirect is the critical read path, so the latency target must stay extremely low.',
  },
  {
    label: 'Design Scale',
    value: '1B QR / 100M users',
    description: 'The design assumes a high-capacity, highly available, read-heavy system from day one.',
  },
];

const requirements: RequirementCard[] = [
  {
    title: 'Functional Requirements',
    points: [
      'Users can submit a URL and receive a generated qr_token.',
      'Users can manage QR codes they created earlier.',
      'After scanning a QR code, the system redirects to the original URL.',
    ],
  },
  {
    title: 'Non-Functional Requirements',
    points: [
      'The service should remain available 24/7 with high availability.',
      'Redirect latency should stay below 100ms.',
      'The system should support 1 billion QR codes and 100 million users.',
    ],
  },
];

const apiEndpoints: ApiEndpoint[] = [
  {
    method: 'POST',
    path: 'v1/qr_code',
    summary: 'Create a new QR code mapping.',
    requestExample: `{\n  "url": "https://example.com"\n}`,
    responseExample: `{\n  "qr_token": "Ab3xK9"\n}`,
    notes: [
      'Validate the URL before generating a globally unique qr_token.',
      'Persist the mapping in the QrCodes table and rely on a DB UNIQUE constraint for final uniqueness.',
    ],
  },
  {
    method: 'GET',
    path: 'v1/qr_code_image/:qr_token?dimension=300&color=000000&border=10',
    summary: 'Return the QR image location based on the token and image parameters.',
    responseExample: `{\n  "image_location": "https://cdn.example.com/qrcode/Ab3xK9.png"\n}`,
    notes: [
      'The QR image behaves like static content and fits well in object storage plus a CDN.',
      'Query parameters control image settings such as dimension, color, and border.',
    ],
  },
  {
    method: 'PUT',
    path: 'v1/qr_code/:qr_token',
    summary: 'Update the original destination URL for an existing QR code.',
    requestExample: `{\n  "url": "https://example.com/new-destination"\n}`,
    notes: [
      'The same qr_token remains in use, so redirects must always resolve the latest mapping.',
    ],
  },
  {
    method: 'DELETE',
    path: 'v1/qr_code/:qr_token',
    summary: 'Delete the QR code mapping for the specified token.',
    notes: [
      'Deletion must invalidate cache entries so stale mappings are not served afterward.',
    ],
  },
  {
    method: 'GET',
    path: 'v1/qr_code/:qr_token',
    summary: 'Resolve the original URL, or perform the redirect in the real scan flow.',
    responseExample: `{\n  "url": "https://example.com"\n}`,
    notes: [
      'The URL embedded in the QR code is https://myqrcode.com/{qr_token}.',
      'The design chooses a 302 Temporary Redirect so every request rechecks the latest state.',
    ],
  },
];

const flows: FlowSection[] = [
  {
    title: 'Create / Edit Flow',
    emphasis: 'Key point: validate input first, persist the mapping second, and return the token last.',
    steps: [
      'The client calls POST /v1/qr_code with a URL in the request body.',
      'The server validates the URL and rejects invalid input immediately.',
      'The service generates a globally unique qr_token and stores the row in the QrCodes table.',
      'If a token collision happens, the DB UNIQUE constraint fails and the service retries with a new token.',
      'The response returns the qr_token; updates keep using the existing token.',
    ],
  },
  {
    title: 'Retrieve / Redirect Flow',
    emphasis: 'Key point: the image can be cached aggressively, but redirect mappings must stay up to date.',
    steps: [
      'The client calls GET /v1/qr_code_image/:qr_token with dimension, color, and border parameters.',
      'The server generates the QR image or returns an existing image resource location.',
      'The QR code points to https://myqrcode.com/{qr_token}.',
      'After a scan, the backend looks up the latest original URL in the QrCodes table.',
      'The server returns a 302 redirect instead of a 301 to avoid permanent browser caching.',
    ],
  },
];

const deepDives: DeepDiveSection[] = [
  {
    title: 'How to generate a unique token',
    problem: 'The key space must be large enough to keep collisions rare while still producing a short, portable qr_token.',
    decisions: [
      'Use a hash function for fixed-length output, then mix in a secret or nonce so identical inputs do not always map to the same token.',
      'Encode the hash with Base62 to make the token shorter and easier to transmit, then truncate to the desired length.',
      'Rely on a DB UNIQUE constraint as the final guardrail and retry if a collision still appears.',
    ],
    tradeoffs: [
      'A purely deterministic hash reduces DB lookups, but the same long URL always maps to the same short token, which is weaker for privacy and product flexibility.',
      'Adding a nonce improves randomness, but then the token-to-URL mapping must be stored in the database.',
    ],
  },
  {
    title: 'How to make redirects fast',
    problem: 'Read traffic is far higher than write traffic, so the database quickly becomes a bottleneck if every redirect hits it directly.',
    decisions: [
      'Index qr_token to avoid full-table scans.',
      'Use cache for hot mappings and fall back to the database only on cache misses.',
      'Push QR images and hot mappings as far forward as possible, ideally to the CDN or edge.',
    ],
    tradeoffs: [
      'A local cache is simple, but hit rates drop when traffic spreads across many instances.',
      'A distributed cache improves hit rates, but adds system complexity and operational cost.',
      'If updates and deletes must take effect immediately, cache invalidation becomes mandatory.',
    ],
  },
  {
    title: 'How the system scales',
    problem: 'Supporting 1 billion records and 100 million users requires capacity planning and fault tolerance from the start.',
    decisions: [
      'Keep application servers stateless so they can scale horizontally.',
      'Because reads dominate writes, add read replicas to absorb redirect traffic.',
      'Use notifications plus cron-driven cleanup for stale data that has not been used for a long time.',
    ],
    tradeoffs: [
      'A single DB instance may work for a while, but it is weaker for resilience.',
      'Replicas improve read performance and availability, but add consistency and failover management costs.',
    ],
  },
];

const scalingPoints = [
  'Redirect is the most important read path, so hot tokens should land in cache and CDN first.',
  'The QR image itself is static content, which makes it a strong fit for CDN delivery.',
  'Choosing 302 over 301 prioritizes product flexibility: updates or deletes can take effect immediately.',
  'At 100 million users and 5 redirects per user per day, the steady-state load is about 5,787 redirects per second.',
];
</script>

<template>
  <div class="space-y-6 p-5">
    <a-alert
      type="info"
      show-icon
      message="This is a system design case study, not an interactive QR code generator."
      description="This page turns the PDF “Design a QR code Generator” into a readable web case study focused on requirements, data flow, performance, and scaling decisions."
    />

    <section class="space-y-4">
      <div class="rounded-2xl bg-slate-950 px-6 py-8 text-slate-50 shadow-sm">
        <div class="space-y-4">
          <a-tag color="blue">System Design Case Study</a-tag>
          <h1 class="text-3xl font-semibold md:text-4xl">QR Code Generator</h1>
          <p class="max-w-4xl text-base leading-7 text-slate-200">
            The design treats a QR code generator as a service that accepts a URL, creates a QR image, and
            redirects through a tokenized endpoint. The real difficulty is not image generation itself, but
            <span class="font-semibold text-white">unique token creation, low-latency redirects, caching strategy, and large-scale growth</span>.
          </p>
          <div class="flex flex-wrap gap-2 text-xs">
            <a-tag color="processing">Read-heavy</a-tag>
            <a-tag color="cyan">302 Redirect</a-tag>
            <a-tag color="purple">Base62 Token</a-tag>
            <a-tag color="gold">Cache + CDN</a-tag>
          </div>
        </div>
      </div>

      <div class="grid gap-4 md:grid-cols-3">
        <a-card
          v-for="metric in metricCards"
          :key="metric.label"
          size="small"
          class="h-full"
        >
          <div class="space-y-2">
            <p class="text-sm text-slate-500">{{ metric.label }}</p>
            <p class="text-xl font-semibold text-slate-900">{{ metric.value }}</p>
            <p class="text-sm leading-6 text-slate-600">{{ metric.description }}</p>
          </div>
        </a-card>
      </div>
    </section>

    <section id="summary">
      <a-card title="Executive Summary">
        <div class="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
          <div
            v-for="link in sectionLinks"
            :key="link.id"
            class="rounded-xl border border-slate-200 p-4 transition hover:border-blue-300 hover:bg-blue-50/40"
          >
            <a :href="`#${link.id}`" class="text-sm font-medium text-slate-700">
              {{ link.title }}
            </a>
          </div>
        </div>

        <a-divider />

        <div class="grid gap-4 lg:grid-cols-2">
          <div class="rounded-xl border border-slate-200 bg-slate-50 p-4">
            <h2 class="mb-2 text-base font-semibold text-slate-900">What problem is this design solving?</h2>
            <p class="text-sm leading-7 text-slate-600">
              The service accepts a URL, creates a globally unique qr_token, and generates the corresponding QR
              code. When the QR code is scanned, the system uses that token to resolve the original URL and
              returns a redirect.
            </p>
          </div>
          <div class="rounded-xl border border-slate-200 bg-slate-50 p-4">
            <h2 class="mb-2 text-base font-semibold text-slate-900">The decisions that matter most</h2>
            <ul class="list-disc space-y-2 pl-5 text-sm leading-7 text-slate-600">
              <li>Choose 302 instead of 301 so mappings can still be updated or deleted later.</li>
              <li>Use index + cache + CDN to absorb the read-heavy redirect workload.</li>
              <li>Combine Base62 encoding with a UNIQUE constraint to balance short tokens and collision safety.</li>
            </ul>
          </div>
        </div>
      </a-card>
    </section>

    <section id="requirements">
      <div class="grid gap-4 xl:grid-cols-2">
        <a-card
          v-for="section in requirements"
          :key="section.title"
          :title="section.title"
        >
          <ul class="list-disc space-y-3 pl-5 text-sm leading-7 text-slate-600">
            <li v-for="point in section.points" :key="point">{{ point }}</li>
          </ul>
        </a-card>
      </div>
    </section>

    <section id="api-design">
      <a-card title="API Design">
        <div class="grid gap-4 xl:grid-cols-2">
          <a-card
            v-for="endpoint in apiEndpoints"
            :key="`${endpoint.method}-${endpoint.path}`"
            size="small"
            class="h-full"
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

              <p class="text-sm leading-7 text-slate-600">{{ endpoint.summary }}</p>

              <div
                v-if="endpoint.requestExample"
                class="overflow-x-auto rounded-xl bg-slate-950 p-4 text-xs leading-6 text-slate-100"
              >
                <p class="mb-2 text-slate-400">Request</p>
                <pre>{{ endpoint.requestExample }}</pre>
              </div>

              <div
                v-if="endpoint.responseExample"
                class="overflow-x-auto rounded-xl bg-slate-950 p-4 text-xs leading-6 text-slate-100"
              >
                <p class="mb-2 text-slate-400">Response</p>
                <pre>{{ endpoint.responseExample }}</pre>
              </div>

              <ul class="list-disc space-y-2 pl-5 text-sm leading-7 text-slate-600">
                <li v-for="note in endpoint.notes" :key="note">{{ note }}</li>
              </ul>
            </div>
          </a-card>
        </div>
      </a-card>
    </section>

    <section id="flows">
      <div class="grid gap-4 xl:grid-cols-2">
        <a-card
          v-for="flow in flows"
          :key="flow.title"
          :title="flow.title"
        >
          <p class="mb-4 rounded-lg bg-amber-50 px-4 py-3 text-sm leading-6 text-amber-700">
            {{ flow.emphasis }}
          </p>
          <ol class="list-decimal space-y-3 pl-5 text-sm leading-7 text-slate-600">
            <li v-for="step in flow.steps" :key="step">{{ step }}</li>
          </ol>
        </a-card>
      </div>

      <a-card title="Why 302 instead of 301?" class="mt-4">
        <div class="grid gap-4 md:grid-cols-2">
          <div class="rounded-xl border border-slate-200 p-4">
            <h3 class="mb-2 text-base font-semibold text-slate-900">301 Permanent Redirect</h3>
            <p class="text-sm leading-7 text-slate-600">
              Browsers tend to cache the result. If the mapping is later updated or deleted, future requests may
              bypass the server entirely.
            </p>
          </div>
          <div class="rounded-xl border border-blue-200 bg-blue-50 p-4">
            <h3 class="mb-2 text-base font-semibold text-blue-900">302 Temporary Redirect</h3>
            <p class="text-sm leading-7 text-blue-800">
              Every request still flows through the backend, so users see the latest mapping after an owner edits
              the destination.
            </p>
          </div>
        </div>
      </a-card>
    </section>

    <section id="deep-dives">
      <div class="grid gap-4">
        <a-card
          v-for="section in deepDives"
          :key="section.title"
          :title="section.title"
        >
          <div class="grid gap-4 xl:grid-cols-[1.2fr_1fr]">
            <div class="space-y-4">
              <div class="rounded-xl border border-slate-200 bg-slate-50 p-4">
                <p class="mb-2 text-sm font-semibold text-slate-900">Problem</p>
                <p class="text-sm leading-7 text-slate-600">{{ section.problem }}</p>
              </div>
              <div>
                <p class="mb-2 text-sm font-semibold text-slate-900">Design Decisions</p>
                <ul class="list-disc space-y-2 pl-5 text-sm leading-7 text-slate-600">
                  <li v-for="item in section.decisions" :key="item">{{ item }}</li>
                </ul>
              </div>
            </div>

            <div class="rounded-xl border border-slate-200 p-4">
              <p class="mb-2 text-sm font-semibold text-slate-900">Trade-offs</p>
              <ul class="list-disc space-y-2 pl-5 text-sm leading-7 text-slate-600">
                <li v-for="item in section.tradeoffs" :key="item">{{ item }}</li>
              </ul>
            </div>
          </div>
        </a-card>
      </div>
    </section>

    <section id="scaling">
      <a-card title="Scaling / Key Decisions">
        <div class="grid gap-4 xl:grid-cols-[1.1fr_0.9fr]">
          <div class="space-y-4">
            <div class="rounded-xl border border-slate-200 bg-slate-50 p-4">
              <p class="mb-2 text-sm font-semibold text-slate-900">Capacity Thinking</p>
              <p class="text-sm leading-7 text-slate-600">
                The document assumes a read-heavy workload and estimates that 500 million redirects per day is
                roughly 5,787 requests per second. That is why the redirect path becomes the center of the
                indexing, caching, and CDN discussion.
              </p>
            </div>

            <div class="overflow-x-auto rounded-xl bg-slate-950 p-4 text-xs leading-6 text-slate-100">
              <p class="mb-2 text-slate-400">Architecture Snapshot</p>
              <pre>Client -> QR Image / CDN
            -> https://myqrcode.com/{qr_token}
            -> App Server (stateless)
            -> Cache (hot mappings)
            -> DB / Read Replicas
            -> Original URL
            -> 302 Redirect</pre>
            </div>
          </div>

          <div class="rounded-xl border border-slate-200 p-4">
            <p class="mb-2 text-sm font-semibold text-slate-900">Key Takeaways</p>
            <ul class="list-disc space-y-3 pl-5 text-sm leading-7 text-slate-600">
              <li v-for="point in scalingPoints" :key="point">{{ point }}</li>
            </ul>
          </div>
        </div>
      </a-card>
    </section>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';

import { Page } from '@vben/common-ui';

import {
  Button,
  Card,
  Checkbox,
  Col,
  DatePicker,
  Descriptions,
  DescriptionsItem,
  Drawer,
  Form,
  FormItem,
  Input,
  InputNumber,
  message,
  Modal,
  Row,
  Select,
  SelectOption,
  Statistic,
  Table,
  Tabs,
  TabPane,
  Tag,
} from 'ant-design-vue';
import dayjs from 'dayjs';

import type {
  BuyStockRequest,
  SellStockRequest,
  StockHoldingResponse,
  StockProfitSummaryResponse,
  StockTransactionResponse,
} from '#/api/core';

import {
  buyStockApi,
  deleteStockTransactionApi,
  getHoldingsApi,
  getStockProfitSummaryApi,
  getStockTransactionsApi,
  sellStockApi,
} from '#/api/core';

defineOptions({ name: 'FinanceStocksPage' });

// ── State ─────────────────────────────────────────

const currentYear = new Date().getFullYear();
const selectedYear = ref(currentYear);
const summary = ref<StockProfitSummaryResponse | null>(null);
const holdings = ref<StockHoldingResponse[]>([]);
const transactions = ref<StockTransactionResponse[]>([]);
const loadingSummary = ref(false);
const loadingHoldings = ref(false);
const loadingTransactions = ref(false);
const filterSymbol = ref<string | undefined>(undefined);

// Drawer
const drawerOpen = ref(false);
const activeTab = ref('buy');
const submitting = ref(false);

// Buy form
const buyForm = ref<BuyStockRequest>({
  commissionDiscount: 0.6,
  note: '',
  pricePerShare: 0,
  shares: 0,
  stockName: '',
  stockSymbol: '',
  tradeDate: dayjs().format('YYYY-MM-DD'),
});

// Sell form
const sellForm = ref<SellStockRequest>({
  commissionDiscount: 0.6,
  isEtf: false,
  note: '',
  pricePerShare: 0,
  shares: 0,
  stockSymbol: '',
  tradeDate: dayjs().format('YYYY-MM-DD'),
});

// ── Computed ──────────────────────────────────────

const uniqueSymbols = computed(() => {
  const map = new Map<string, string>();
  for (const t of transactions.value) {
    if (!map.has(t.stockSymbol)) {
      map.set(t.stockSymbol, `${t.stockSymbol} ${t.stockName}`);
    }
  }
  return [...map.entries()].map(([value, label]) => ({ label, value }));
});

const filteredTransactions = computed(() => {
  if (!filterSymbol.value) return transactions.value;
  return transactions.value.filter(
    (t) => t.stockSymbol === filterSymbol.value,
  );
});

const sellableHoldings = computed(() =>
  holdings.value.filter((h) => h.shares > 0),
);

const selectedHoldingShares = computed(() => {
  const found = holdings.value.find(
    (h) => h.stockSymbol === sellForm.value.stockSymbol,
  );
  return found?.shares ?? 0;
});

// Buy calculations
const buyTotalAmount = computed(
  () => buyForm.value.shares * buyForm.value.pricePerShare,
);

const buyCommission = computed(() =>
  Math.max(
    Math.floor(
      buyTotalAmount.value * 0.001_425 * (buyForm.value.commissionDiscount ?? 0.6),
    ),
    20,
  ),
);

const buyTotalCost = computed(() => buyTotalAmount.value + buyCommission.value);

// Sell calculations
const sellTotalAmount = computed(
  () => sellForm.value.shares * sellForm.value.pricePerShare,
);

const sellCommission = computed(() =>
  Math.max(
    Math.floor(
      sellTotalAmount.value *
        0.001_425 *
        (sellForm.value.commissionDiscount ?? 0.6),
    ),
    20,
  ),
);

const sellTax = computed(() =>
  Math.floor(
    sellTotalAmount.value * (sellForm.value.isEtf ? 0.001 : 0.003),
  ),
);

const sellNetIncome = computed(
  () => sellTotalAmount.value - sellCommission.value - sellTax.value,
);

// ── Helpers ──────────────────────────────────────

function formatCurrency(value: number): string {
  return value.toLocaleString('zh-TW');
}

function plColor(value: number): string {
  if (value > 0) return '#cf1322';
  if (value < 0) return '#3f8600';
  return 'inherit';
}

// ── Columns ──────────────────────────────────────

const holdingColumns = [
  { dataIndex: 'stockSymbol', title: '股票代號', width: 100 },
  { dataIndex: 'stockName', title: '股票名稱', width: 120 },
  { align: 'right' as const, dataIndex: 'shares', title: '持有股數', width: 100 },
  {
    align: 'right' as const,
    customRender: ({ record }: { record: StockHoldingResponse }) =>
      record.averageCost.toFixed(2),
    dataIndex: 'averageCost',
    title: '平均成本',
    width: 110,
  },
  {
    align: 'right' as const,
    customRender: ({ record }: { record: StockHoldingResponse }) =>
      formatCurrency(record.totalCost),
    dataIndex: 'totalCost',
    title: '累計成本',
    width: 120,
  },
  {
    align: 'right' as const,
    dataIndex: 'totalRealizedPL',
    key: 'totalRealizedPL',
    title: '已實現損益',
    width: 130,
  },
];

const transactionColumns = [
  { dataIndex: 'tradeDate', title: '日期', width: 110 },
  {
    align: 'center' as const,
    dataIndex: 'tradeType',
    key: 'tradeType',
    title: '買/賣',
    width: 70,
  },
  { dataIndex: 'stockSymbol', title: '股票代號', width: 90 },
  { dataIndex: 'stockName', title: '股票名稱', width: 110 },
  { align: 'right' as const, dataIndex: 'shares', title: '股數', width: 80 },
  {
    align: 'right' as const,
    customRender: ({ record }: { record: StockTransactionResponse }) =>
      record.pricePerShare.toFixed(2),
    dataIndex: 'pricePerShare',
    title: '成交價',
    width: 90,
  },
  {
    align: 'right' as const,
    customRender: ({ record }: { record: StockTransactionResponse }) =>
      formatCurrency(record.commission),
    dataIndex: 'commission',
    title: '手續費',
    width: 90,
  },
  {
    align: 'right' as const,
    customRender: ({ record }: { record: StockTransactionResponse }) =>
      formatCurrency(record.tax),
    dataIndex: 'tax',
    title: '交易稅',
    width: 90,
  },
  {
    align: 'right' as const,
    customRender: ({ record }: { record: StockTransactionResponse }) =>
      formatCurrency(record.netAmount),
    dataIndex: 'netAmount',
    title: '淨額',
    width: 110,
  },
  {
    align: 'right' as const,
    dataIndex: 'realizedProfitLoss',
    key: 'realizedProfitLoss',
    title: '損益',
    width: 110,
  },
  {
    align: 'center' as const,
    fixed: 'right' as const,
    key: 'action',
    title: '操作',
    width: 70,
  },
];

// ── Fetch ────────────────────────────────────────

async function fetchSummary() {
  loadingSummary.value = true;
  try {
    summary.value = await getStockProfitSummaryApi(selectedYear.value);
  } finally {
    loadingSummary.value = false;
  }
}

async function fetchHoldings() {
  loadingHoldings.value = true;
  try {
    holdings.value = await getHoldingsApi();
  } finally {
    loadingHoldings.value = false;
  }
}

async function fetchTransactions() {
  loadingTransactions.value = true;
  try {
    transactions.value = await getStockTransactionsApi();
  } finally {
    loadingTransactions.value = false;
  }
}

async function fetchAll() {
  await Promise.all([fetchSummary(), fetchHoldings(), fetchTransactions()]);
}

// ── Actions ──────────────────────────────────────

function openDrawer() {
  activeTab.value = 'buy';
  resetBuyForm();
  resetSellForm();
  drawerOpen.value = true;
}

function resetBuyForm() {
  buyForm.value = {
    commissionDiscount: 0.6,
    note: '',
    pricePerShare: 0,
    shares: 0,
    stockName: '',
    stockSymbol: '',
    tradeDate: dayjs().format('YYYY-MM-DD'),
  };
}

function resetSellForm() {
  sellForm.value = {
    commissionDiscount: 0.6,
    isEtf: false,
    note: '',
    pricePerShare: 0,
    shares: 0,
    stockSymbol: '',
    tradeDate: dayjs().format('YYYY-MM-DD'),
  };
}

async function handleBuy() {
  const f = buyForm.value;
  if (!f.stockSymbol || !f.stockName || f.shares <= 0 || f.pricePerShare <= 0) {
    message.warning('請填寫完整的買入資訊');
    return;
  }
  submitting.value = true;
  try {
    await buyStockApi({
      commissionDiscount: f.commissionDiscount,
      note: f.note || undefined,
      pricePerShare: f.pricePerShare,
      shares: f.shares,
      stockName: f.stockName,
      stockSymbol: f.stockSymbol.toUpperCase(),
      tradeDate: f.tradeDate,
    });
    message.success('買入成功');
    drawerOpen.value = false;
    await fetchAll();
  } catch {
    message.error('買入失敗');
  } finally {
    submitting.value = false;
  }
}

async function handleSell() {
  const f = sellForm.value;
  if (!f.stockSymbol || f.shares <= 0 || f.pricePerShare <= 0) {
    message.warning('請填寫完整的賣出資訊');
    return;
  }
  if (f.shares > selectedHoldingShares.value) {
    message.warning('賣出股數不可超過持有股數');
    return;
  }
  submitting.value = true;
  try {
    await sellStockApi({
      commissionDiscount: f.commissionDiscount,
      isEtf: f.isEtf,
      note: f.note || undefined,
      pricePerShare: f.pricePerShare,
      shares: f.shares,
      stockSymbol: f.stockSymbol,
      tradeDate: f.tradeDate,
    });
    message.success('賣出成功');
    drawerOpen.value = false;
    await fetchAll();
  } catch {
    message.error('賣出失敗');
  } finally {
    submitting.value = false;
  }
}

function handleDelete(record: StockTransactionResponse) {
  Modal.confirm({
    content: `確定要刪除 ${record.tradeDate} ${record.tradeType === 'Buy' ? '買入' : '賣出'} ${record.stockSymbol} 的交易紀錄嗎？`,
    okText: '刪除',
    okType: 'danger',
    title: '確認刪除',
    async onOk() {
      try {
        await deleteStockTransactionApi(record.id);
        message.success('已刪除');
        await fetchAll();
      } catch {
        message.error('刪除失敗');
      }
    },
  });
}

function handleSellSymbolChange(symbol: string) {
  sellForm.value.stockSymbol = symbol;
  sellForm.value.shares = 0;
}

// ── Lifecycle ────────────────────────────────────

watch(selectedYear, fetchSummary);
onMounted(fetchAll);
</script>

<template>
  <Page auto-content-height>
    <!-- Section 1: Annual Profit Summary -->
    <Card class="mb-4">
      <div class="mb-4 flex items-center gap-4">
        <h3 class="text-lg font-medium">年度損益摘要</h3>
        <div class="flex items-center gap-2">
          <span class="text-sm text-gray-500">年度：</span>
          <InputNumber
            v-model:value="selectedYear"
            :max="currentYear + 1"
            :min="2020"
            style="width: 100px"
          />
        </div>
      </div>

      <Row :gutter="16">
        <Col :md="6" :xs="12">
          <Card :loading="loadingSummary">
            <Statistic
              :value="summary?.totalRealizedPL ?? 0"
              :value-style="{
                color: plColor(summary?.totalRealizedPL ?? 0),
              }"
              prefix="$"
              title="已實現損益"
            />
          </Card>
        </Col>
        <Col :md="6" :xs="12">
          <Card :loading="loadingSummary">
            <Statistic
              :value="summary?.totalCommission ?? 0"
              prefix="$"
              title="累計手續費"
            />
          </Card>
        </Col>
        <Col :md="6" :xs="12">
          <Card :loading="loadingSummary">
            <Statistic
              :value="summary?.totalTax ?? 0"
              prefix="$"
              title="累計交易稅"
            />
          </Card>
        </Col>
        <Col :md="6" :xs="12">
          <Card :loading="loadingSummary">
            <Statistic
              :value="summary?.transactionCount ?? 0"
              suffix="筆"
              title="交易次數"
            />
          </Card>
        </Col>
      </Row>
    </Card>

    <!-- Section 2: Holdings Table -->
    <Card class="mb-4">
      <div class="mb-4 flex items-center justify-between">
        <h3 class="text-lg font-medium">持股清單</h3>
      </div>

      <Table
        :columns="holdingColumns"
        :data-source="holdings"
        :loading="loadingHoldings"
        :pagination="false"
        row-key="stockSymbol"
        size="middle"
      >
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'totalRealizedPL'">
            <span
              :style="{
                color: plColor(
                  (record as unknown as StockHoldingResponse).totalRealizedPL,
                ),
              }"
            >
              {{
                formatCurrency(
                  (record as unknown as StockHoldingResponse).totalRealizedPL,
                )
              }}
            </span>
          </template>
        </template>

        <template #emptyText>
          <div class="py-8 text-center text-gray-400">尚無持股資料</div>
        </template>
      </Table>
    </Card>

    <!-- Section 3: Transaction History -->
    <Card>
      <div class="mb-4 flex items-center justify-between">
        <h3 class="text-lg font-medium">交易紀錄</h3>
        <Select
          v-model:value="filterSymbol"
          allow-clear
          placeholder="篩選股票"
          style="width: 200px"
        >
          <SelectOption
            v-for="opt in uniqueSymbols"
            :key="opt.value"
            :value="opt.value"
          >
            {{ opt.label }}
          </SelectOption>
        </Select>
      </div>

      <Table
        :columns="transactionColumns"
        :data-source="filteredTransactions"
        :loading="loadingTransactions"
        :pagination="{ pageSize: 20, showSizeChanger: true }"
        row-key="id"
        :scroll="{ x: 1100 }"
        size="middle"
      >
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'tradeType'">
            <Tag
              v-if="
                (record as unknown as StockTransactionResponse).tradeType ===
                'Buy'
              "
              color="red"
            >
              買入
            </Tag>
            <Tag v-else color="green">賣出</Tag>
          </template>

          <template v-if="column.key === 'realizedProfitLoss'">
            <span
              v-if="
                (record as unknown as StockTransactionResponse)
                  .realizedProfitLoss != null
              "
              :style="{
                color: plColor(
                  (record as unknown as StockTransactionResponse)
                    .realizedProfitLoss!,
                ),
              }"
            >
              {{
                formatCurrency(
                  (record as unknown as StockTransactionResponse)
                    .realizedProfitLoss!,
                )
              }}
            </span>
            <span v-else class="text-gray-300">—</span>
          </template>

          <template v-if="column.key === 'action'">
            <Button
              danger
              size="small"
              type="link"
              @click="
                handleDelete(
                  record as unknown as StockTransactionResponse,
                )
              "
            >
              刪除
            </Button>
          </template>
        </template>

        <template #emptyText>
          <div class="py-8 text-center text-gray-400">尚無交易紀錄</div>
        </template>
      </Table>
    </Card>

    <!-- Floating Action Button -->
    <Button
      class="!fixed bottom-8 right-8 !h-14 !w-14 !rounded-full !shadow-lg"
      size="large"
      type="primary"
      @click="openDrawer"
    >
      <span class="text-xl">+</span>
    </Button>

    <!-- Trade Drawer -->
    <Drawer
      :open="drawerOpen"
      title="新增交易"
      :width="480"
      @close="drawerOpen = false"
    >
      <Tabs v-model:activeKey="activeTab">
        <!-- Buy Tab -->
        <TabPane key="buy" tab="買入">
          <Form layout="vertical">
            <FormItem label="股票代號" required>
              <Input
                v-model:value="buyForm.stockSymbol"
                placeholder="例如 2330"
              />
            </FormItem>
            <FormItem label="股票名稱" required>
              <Input
                v-model:value="buyForm.stockName"
                placeholder="例如 台積電"
              />
            </FormItem>
            <FormItem label="股數" required>
              <InputNumber
                v-model:value="buyForm.shares"
                :min="1"
                :step="1000"
                style="width: 100%"
              />
            </FormItem>
            <FormItem label="成交價" required>
              <InputNumber
                v-model:value="buyForm.pricePerShare"
                :min="0.01"
                :step="0.5"
                style="width: 100%"
              />
            </FormItem>
            <FormItem label="券商手續費折扣">
              <InputNumber
                v-model:value="buyForm.commissionDiscount"
                :max="1"
                :min="0"
                :step="0.05"
                style="width: 100%"
              />
            </FormItem>
            <FormItem label="日期" required>
              <DatePicker
                :value="dayjs(buyForm.tradeDate)"
                style="width: 100%"
                value-format="YYYY-MM-DD"
                @change="
                  (_: unknown, dateStr: string | string[]) =>
                    (buyForm.tradeDate = Array.isArray(dateStr) ? dateStr[0]! : dateStr)
                "
              />
            </FormItem>
            <FormItem label="備註">
              <Input v-model:value="buyForm.note" placeholder="選填" />
            </FormItem>

            <!-- Auto-calculated Summary -->
            <Descriptions bordered :column="1" size="small">
              <DescriptionsItem label="成交金額">
                $ {{ formatCurrency(buyTotalAmount) }}
              </DescriptionsItem>
              <DescriptionsItem label="手續費">
                $ {{ formatCurrency(buyCommission) }}
              </DescriptionsItem>
              <DescriptionsItem label="買入總成本">
                <span class="font-semibold">
                  $ {{ formatCurrency(buyTotalCost) }}
                </span>
              </DescriptionsItem>
            </Descriptions>

            <Button
              block
              class="mt-4"
              :loading="submitting"
              type="primary"
              @click="handleBuy"
            >
              確認買入
            </Button>
          </Form>
        </TabPane>

        <!-- Sell Tab -->
        <TabPane key="sell" tab="賣出">
          <Form layout="vertical">
            <FormItem label="股票代號" required>
              <Select
                :value="sellForm.stockSymbol || undefined"
                placeholder="選擇持股"
                style="width: 100%"
                @change="(v: unknown) => handleSellSymbolChange(String(v))"
              >
                <SelectOption
                  v-for="h in sellableHoldings"
                  :key="h.stockSymbol"
                  :value="h.stockSymbol"
                >
                  {{ h.stockSymbol }} {{ h.stockName }}（{{ h.shares }} 股）
                </SelectOption>
              </Select>
            </FormItem>
            <FormItem label="股數" required>
              <InputNumber
                v-model:value="sellForm.shares"
                :max="selectedHoldingShares"
                :min="1"
                :step="1000"
                style="width: 100%"
              />
            </FormItem>
            <FormItem label="成交價" required>
              <InputNumber
                v-model:value="sellForm.pricePerShare"
                :min="0.01"
                :step="0.5"
                style="width: 100%"
              />
            </FormItem>
            <FormItem label="券商手續費折扣">
              <InputNumber
                v-model:value="sellForm.commissionDiscount"
                :max="1"
                :min="0"
                :step="0.05"
                style="width: 100%"
              />
            </FormItem>
            <FormItem label="是否 ETF">
              <Checkbox v-model:checked="sellForm.isEtf">
                ETF（交易稅 0.1%）
              </Checkbox>
            </FormItem>
            <FormItem label="日期" required>
              <DatePicker
                :value="dayjs(sellForm.tradeDate)"
                style="width: 100%"
                value-format="YYYY-MM-DD"
                @change="
                  (_: unknown, dateStr: string | string[]) =>
                    (sellForm.tradeDate = Array.isArray(dateStr) ? dateStr[0]! : dateStr)
                "
              />
            </FormItem>
            <FormItem label="備註">
              <Input v-model:value="sellForm.note" placeholder="選填" />
            </FormItem>

            <!-- Auto-calculated Summary -->
            <Descriptions bordered :column="1" size="small">
              <DescriptionsItem label="成交金額">
                $ {{ formatCurrency(sellTotalAmount) }}
              </DescriptionsItem>
              <DescriptionsItem label="手續費">
                $ {{ formatCurrency(sellCommission) }}
              </DescriptionsItem>
              <DescriptionsItem label="交易稅">
                $ {{ formatCurrency(sellTax) }}
              </DescriptionsItem>
              <DescriptionsItem label="賣出淨收入">
                <span class="font-semibold">
                  $ {{ formatCurrency(sellNetIncome) }}
                </span>
              </DescriptionsItem>
            </Descriptions>

            <Button
              block
              class="mt-4"
              :loading="submitting"
              type="primary"
              @click="handleSell"
            >
              確認賣出
            </Button>
          </Form>
        </TabPane>
      </Tabs>
    </Drawer>
  </Page>
</template>

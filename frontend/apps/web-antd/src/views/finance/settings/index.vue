<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';

import { Page } from '@vben/common-ui';

import {
  Button,
  Card,
  Drawer,
  Form,
  FormItem,
  Input,
  InputNumber,
  message,
  Popconfirm,
  Segmented,
  Select,
  SelectOption,
  Switch,
  Table,
  Tabs,
  TabPane,
  Tag,
} from 'ant-design-vue';

import type {
  AccountResponse,
  CategoryResponse,
  CreateAccountRequest,
  CreateCategoryRequest,
  UpdateAccountRequest,
  UpdateCategoryRequest,
} from '#/api';

import {
  createAccountApi,
  createCategoryApi,
  deleteAccountApi,
  deleteCategoryApi,
  getAccountsApi,
  getCategoriesApi,
  updateAccountApi,
  updateCategoryApi,
} from '#/api';

defineOptions({ name: 'FinanceSettingsPage' });

const activeTab = ref('accounts');

// ── 帳戶管理 ─────────────────────────────────────

const accountsLoading = ref(false);
const accounts = ref<AccountResponse[]>([]);
const accountDrawerOpen = ref(false);
const accountDrawerTitle = ref('新增帳戶');
const editingAccountId = ref<null | string>(null);
const accountSubmitting = ref(false);

const accountForm = ref<CreateAccountRequest & { isArchived?: boolean }>({
  accountType: 'Cash',
  icon: '',
  initialBalance: 0,
  name: '',
  sortOrder: 0,
});

const accountTypeMap: Record<string, string> = {
  Bank: '銀行帳戶',
  Cash: '現金',
  CreditCard: '信用卡',
  EPayment: '電子支付',
  Investment: '投資帳戶',
};

const accountColumns = [
  { dataIndex: 'name', title: '名稱', width: 180 },
  {
    customRender: ({ text }: { text: string }) => accountTypeMap[text] ?? text,
    dataIndex: 'accountType',
    title: '帳戶類型',
    width: 140,
  },
  {
    align: 'right' as const,
    customRender: ({ text }: { text: number }) =>
      text.toLocaleString('zh-TW'),
    dataIndex: 'initialBalance',
    title: '初始餘額',
    width: 140,
  },
  {
    align: 'center' as const,
    dataIndex: 'sortOrder',
    title: '排序',
    width: 80,
  },
  {
    align: 'center' as const,
    dataIndex: 'isArchived',
    key: 'isArchived',
    title: '封存',
    width: 100,
  },
  {
    fixed: 'right' as const,
    key: 'action',
    title: '操作',
    width: 140,
  },
];

async function fetchAccounts() {
  accountsLoading.value = true;
  try {
    accounts.value = await getAccountsApi();
  } finally {
    accountsLoading.value = false;
  }
}

function openCreateAccount() {
  editingAccountId.value = null;
  accountDrawerTitle.value = '新增帳戶';
  accountForm.value = {
    accountType: 'Cash',
    icon: '',
    initialBalance: 0,
    name: '',
    sortOrder: 0,
  };
  accountDrawerOpen.value = true;
}

function openEditAccount(record: AccountResponse) {
  editingAccountId.value = record.id;
  accountDrawerTitle.value = '編輯帳戶';
  accountForm.value = {
    accountType: record.accountType,
    icon: record.icon ?? '',
    initialBalance: record.initialBalance,
    isArchived: record.isArchived,
    name: record.name,
    sortOrder: record.sortOrder,
  };
  accountDrawerOpen.value = true;
}

async function handleAccountSubmit() {
  if (!accountForm.value.name.trim()) {
    message.warning('請輸入帳戶名稱');
    return;
  }
  accountSubmitting.value = true;
  try {
    if (editingAccountId.value) {
      const data: UpdateAccountRequest = {
        accountType: accountForm.value.accountType,
        icon: accountForm.value.icon || undefined,
        initialBalance: accountForm.value.initialBalance,
        isArchived: accountForm.value.isArchived,
        name: accountForm.value.name.trim(),
        sortOrder: accountForm.value.sortOrder,
      };
      await updateAccountApi(editingAccountId.value, data);
      message.success('帳戶已更新');
    } else {
      const data: CreateAccountRequest = {
        accountType: accountForm.value.accountType,
        icon: accountForm.value.icon || undefined,
        initialBalance: accountForm.value.initialBalance,
        name: accountForm.value.name.trim(),
        sortOrder: accountForm.value.sortOrder,
      };
      await createAccountApi(data);
      message.success('帳戶已新增');
    }
    accountDrawerOpen.value = false;
    await fetchAccounts();
  } catch {
    message.error(editingAccountId.value ? '帳戶更新失敗' : '帳戶新增失敗');
  } finally {
    accountSubmitting.value = false;
  }
}

async function handleDeleteAccount(id: string) {
  try {
    await deleteAccountApi(id);
    message.success('帳戶已刪除');
    await fetchAccounts();
  } catch {
    message.error('刪除失敗');
  }
}

// ── 分類管理 ─────────────────────────────────────

const categoriesLoading = ref(false);
const categories = ref<CategoryResponse[]>([]);
const categoryTypeFilter = ref<'Expense' | 'Income'>('Expense');
const categoryDrawerOpen = ref(false);
const categoryDrawerTitle = ref('新增分類');
const editingCategoryId = ref<null | string>(null);
const categorySubmitting = ref(false);

const categoryForm = ref<CreateCategoryRequest>({
  categoryType: 'Expense',
  icon: '',
  name: '',
  parentId: undefined,
  sortOrder: 0,
});

const filteredCategories = computed(() =>
  categories.value.filter((c) => c.categoryType === categoryTypeFilter.value),
);

// 將 children: null 轉為 undefined，避免 Table 顯示無意義的展開圖示
const categoryTableData = computed(() =>
  filteredCategories.value.map((c) => ({
    ...c,
    children: c.children?.length ? c.children : undefined,
  })),
);

const parentCategoryOptions = computed(() => filteredCategories.value);

const categoryColumns = [
  { dataIndex: 'name', title: '名稱', width: 200 },
  { dataIndex: 'icon', title: '圖示', width: 100 },
  {
    align: 'center' as const,
    dataIndex: 'sortOrder',
    title: '排序',
    width: 80,
  },
  {
    align: 'center' as const,
    dataIndex: 'isSystem',
    key: 'isSystem',
    title: '系統預設',
    width: 100,
  },
  {
    fixed: 'right' as const,
    key: 'action',
    title: '操作',
    width: 140,
  },
];

async function fetchCategories() {
  categoriesLoading.value = true;
  try {
    categories.value = await getCategoriesApi();
  } finally {
    categoriesLoading.value = false;
  }
}

function openCreateCategory() {
  editingCategoryId.value = null;
  categoryDrawerTitle.value = '新增分類';
  categoryForm.value = {
    categoryType: categoryTypeFilter.value,
    icon: '',
    name: '',
    parentId: undefined,
    sortOrder: 0,
  };
  categoryDrawerOpen.value = true;
}

function openEditCategory(record: CategoryResponse) {
  editingCategoryId.value = record.id;
  categoryDrawerTitle.value = '編輯分類';
  categoryForm.value = {
    categoryType: record.categoryType,
    icon: record.icon ?? '',
    name: record.name,
    sortOrder: record.sortOrder,
  };
  categoryDrawerOpen.value = true;
}

async function handleCategorySubmit() {
  if (!categoryForm.value.name.trim()) {
    message.warning('請輸入分類名稱');
    return;
  }
  categorySubmitting.value = true;
  try {
    if (editingCategoryId.value) {
      const data: UpdateCategoryRequest = {
        icon: categoryForm.value.icon || undefined,
        name: categoryForm.value.name.trim(),
        sortOrder: categoryForm.value.sortOrder,
      };
      await updateCategoryApi(editingCategoryId.value, data);
      message.success('分類已更新');
    } else {
      const data: CreateCategoryRequest = {
        categoryType: categoryForm.value.categoryType,
        icon: categoryForm.value.icon || undefined,
        name: categoryForm.value.name.trim(),
        parentId: categoryForm.value.parentId || undefined,
        sortOrder: categoryForm.value.sortOrder,
      };
      await createCategoryApi(data);
      message.success('分類已新增');
    }
    categoryDrawerOpen.value = false;
    await fetchCategories();
  } catch {
    message.error(
      editingCategoryId.value ? '分類更新失敗' : '分類新增失敗',
    );
  } finally {
    categorySubmitting.value = false;
  }
}

async function handleDeleteCategory(id: string) {
  try {
    await deleteCategoryApi(id);
    message.success('分類已刪除');
    await fetchCategories();
  } catch {
    message.error('刪除失敗');
  }
}

onMounted(() => {
  fetchAccounts();
  fetchCategories();
});
</script>

<template>
  <Page auto-content-height>
    <Card :body-style="{ padding: '16px 24px' }">
      <Tabs v-model:activeKey="activeTab">
        <!-- ── 帳戶管理 ── -->
        <TabPane key="accounts" tab="帳戶管理">
          <div class="mb-4 flex items-center justify-between">
            <h3 class="text-lg font-medium">帳戶管理</h3>
            <Button type="primary" @click="openCreateAccount">
              新增帳戶
            </Button>
          </div>

          <Table
            :columns="accountColumns"
            :data-source="accounts"
            :loading="accountsLoading"
            :pagination="false"
            row-key="id"
            size="middle"
          >
            <template #bodyCell="{ column, record }">
              <template v-if="column.key === 'isArchived'">
                <Tag :color="record.isArchived ? 'red' : 'green'">
                  {{ record.isArchived ? '已封存' : '使用中' }}
                </Tag>
              </template>
              <template v-if="column.key === 'action'">
                <div class="flex gap-2">
                  <Button
                    size="small"
                    type="link"
                    @click="
                      openEditAccount(
                        record as unknown as AccountResponse,
                      )
                    "
                  >
                    編輯
                  </Button>
                  <Popconfirm
                    cancel-text="取消"
                    ok-text="確認刪除"
                    ok-type="danger"
                    :title="`確定要刪除「${record.name}」嗎？`"
                    @confirm="handleDeleteAccount(record.id as string)"
                  >
                    <Button danger size="small" type="link">刪除</Button>
                  </Popconfirm>
                </div>
              </template>
            </template>

            <template #emptyText>
              <div class="py-8 text-center text-gray-400">
                尚無帳戶資料，請點擊「新增帳戶」按鈕建立
              </div>
            </template>
          </Table>
        </TabPane>

        <!-- ── 分類管理 ── -->
        <TabPane key="categories" tab="分類管理">
          <div class="mb-4 flex items-center justify-between">
            <div class="flex items-center gap-4">
              <h3 class="text-lg font-medium">分類管理</h3>
              <Segmented
                v-model:value="categoryTypeFilter"
                :options="[
                  { label: '支出分類', value: 'Expense' },
                  { label: '收入分類', value: 'Income' },
                ]"
              />
            </div>
            <Button type="primary" @click="openCreateCategory">
              新增分類
            </Button>
          </div>

          <Table
            children-column-name="children"
            :columns="categoryColumns"
            :data-source="categoryTableData"
            :default-expand-all-rows="true"
            :loading="categoriesLoading"
            :pagination="false"
            row-key="id"
            size="middle"
          >
            <template #bodyCell="{ column, record }">
              <template v-if="column.key === 'isSystem'">
                <Tag :color="record.isSystem ? 'blue' : 'default'">
                  {{ record.isSystem ? '系統' : '自訂' }}
                </Tag>
              </template>
              <template v-if="column.key === 'action'">
                <div v-if="!record.isSystem" class="flex gap-2">
                  <Button
                    size="small"
                    type="link"
                    @click="
                      openEditCategory(
                        record as unknown as CategoryResponse,
                      )
                    "
                  >
                    編輯
                  </Button>
                  <Popconfirm
                    cancel-text="取消"
                    ok-text="確認刪除"
                    ok-type="danger"
                    :title="`確定要刪除「${record.name}」嗎？`"
                    @confirm="handleDeleteCategory(record.id as string)"
                  >
                    <Button danger size="small" type="link">刪除</Button>
                  </Popconfirm>
                </div>
                <span v-else class="text-gray-400">—</span>
              </template>
            </template>

            <template #emptyText>
              <div class="py-8 text-center text-gray-400">
                尚無分類資料，請點擊「新增分類」按鈕建立
              </div>
            </template>
          </Table>
        </TabPane>
      </Tabs>
    </Card>

    <!-- 帳戶 Drawer -->
    <Drawer
      destroy-on-close
      :open="accountDrawerOpen"
      placement="right"
      :title="accountDrawerTitle"
      :width="400"
      @close="accountDrawerOpen = false"
    >
      <Form :model="accountForm" layout="vertical">
        <FormItem label="名稱" required>
          <Input
            v-model:value="accountForm.name"
            :maxlength="50"
            placeholder="請輸入帳戶名稱"
            show-count
          />
        </FormItem>
        <FormItem label="帳戶類型" required>
          <Select
            v-model:value="accountForm.accountType"
            placeholder="請選擇帳戶類型"
          >
            <SelectOption value="Cash">現金</SelectOption>
            <SelectOption value="Bank">銀行帳戶</SelectOption>
            <SelectOption value="CreditCard">信用卡</SelectOption>
            <SelectOption value="EPayment">電子支付</SelectOption>
            <SelectOption value="Investment">投資帳戶</SelectOption>
          </Select>
        </FormItem>
        <FormItem label="初始餘額">
          <InputNumber
            v-model:value="accountForm.initialBalance"
            :formatter="
              (v: string | number | undefined) =>
                v != null
                  ? `${v}`.replace(/\B(?=(\d{3})+(?!\d))/g, ',')
                  : ''
            "
            :parser="(v: string | undefined) => v?.replace(/,/g, '') ?? ''"
            placeholder="0"
            :style="{ width: '100%' }"
          />
        </FormItem>
        <FormItem label="圖示">
          <Input
            v-model:value="accountForm.icon"
            :maxlength="100"
            placeholder="例如：mdi:cash"
          />
        </FormItem>
        <FormItem label="排序">
          <InputNumber
            v-model:value="accountForm.sortOrder"
            :min="0"
            placeholder="0"
            :style="{ width: '100%' }"
          />
        </FormItem>
        <FormItem v-if="editingAccountId" label="封存">
          <Switch
            v-model:checked="accountForm.isArchived"
            checked-children="已封存"
            un-checked-children="使用中"
          />
        </FormItem>
      </Form>
      <template #extra>
        <Button
          :loading="accountSubmitting"
          type="primary"
          @click="handleAccountSubmit"
        >
          儲存
        </Button>
      </template>
    </Drawer>

    <!-- 分類 Drawer -->
    <Drawer
      destroy-on-close
      :open="categoryDrawerOpen"
      placement="right"
      :title="categoryDrawerTitle"
      :width="400"
      @close="categoryDrawerOpen = false"
    >
      <Form :model="categoryForm" layout="vertical">
        <FormItem label="名稱" required>
          <Input
            v-model:value="categoryForm.name"
            :maxlength="50"
            placeholder="請輸入分類名稱"
            show-count
          />
        </FormItem>
        <FormItem label="分類類型">
          <Select
            v-model:value="categoryForm.categoryType"
            :disabled="!!editingCategoryId"
          >
            <SelectOption value="Expense">支出</SelectOption>
            <SelectOption value="Income">收入</SelectOption>
          </Select>
        </FormItem>
        <FormItem v-if="!editingCategoryId" label="上層分類">
          <Select
            v-model:value="categoryForm.parentId"
            allow-clear
            placeholder="無（主分類）"
          >
            <SelectOption
              v-for="cat in parentCategoryOptions"
              :key="cat.id"
              :value="cat.id"
            >
              {{ cat.name }}
            </SelectOption>
          </Select>
        </FormItem>
        <FormItem label="圖示">
          <Input
            v-model:value="categoryForm.icon"
            :maxlength="100"
            placeholder="例如：mdi:food"
          />
        </FormItem>
        <FormItem label="排序">
          <InputNumber
            v-model:value="categoryForm.sortOrder"
            :min="0"
            placeholder="0"
            :style="{ width: '100%' }"
          />
        </FormItem>
      </Form>
      <template #extra>
        <Button
          :loading="categorySubmitting"
          type="primary"
          @click="handleCategorySubmit"
        >
          儲存
        </Button>
      </template>
    </Drawer>
  </Page>
</template>

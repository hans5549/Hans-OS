<script lang="ts" setup>
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
  Popconfirm,
  Select,
  SelectOption,
  Switch,
  Table,
  Tabs,
  TabPane,
  Tag,
  message,
} from 'ant-design-vue';

import type {
  ArticleBookmarkGroupResponse,
  ArticleBookmarkQueryRequest,
  ArticleBookmarkResponse,
  ArticleBookmarkSourceType,
  CreateArticleBookmarkGroupRequest,
  CreateArticleBookmarkRequest,
  UpdateArticleBookmarkGroupRequest,
  UpdateArticleBookmarkRequest,
} from '#/api';

import {
  createArticleBookmarkApi,
  createArticleBookmarkGroupApi,
  deleteArticleBookmarkApi,
  deleteArticleBookmarkGroupApi,
  getArticleBookmarkGroupsApi,
  getArticleBookmarksApi,
  patchArticleBookmarkStateApi,
  updateArticleBookmarkApi,
  updateArticleBookmarkGroupApi,
} from '#/api';

defineOptions({ name: 'ArticleCollectionPage' });

type PinFilter = 'all' | 'pinned' | 'unpinned';
type ReadFilter = 'all' | 'read' | 'unread';
type SourceTypeFilter = 'all' | ArticleBookmarkSourceType;

interface BookmarkFormState {
  sourceType: ArticleBookmarkSourceType;
  url?: string;
  title: string;
  customTitle?: string;
  excerptSnapshot?: string;
  coverImageUrl?: string;
  note?: string;
  groupId?: string;
  tags: string[];
  isPinned: boolean;
  isRead: boolean;
}

const activeTab = ref('bookmarks');

const filters = ref<{
  groupId?: string;
  keyword: string;
  pin: PinFilter;
  read: ReadFilter;
  sourceType: SourceTypeFilter;
}>({
  keyword: '',
  pin: 'all',
  read: 'all',
  sourceType: 'all',
});

const sourceTypeLabelMap: Record<ArticleBookmarkSourceType, string> = {
  ExternalUrl: '外部文章',
  InternalArticle: '內建文章',
};

const bookmarksLoading = ref(false);
const bookmarks = ref<ArticleBookmarkResponse[]>([]);
const bookmarkPagination = ref({
  page: 1,
  pageSize: 20,
  total: 0,
});
const bookmarkDrawerOpen = ref(false);
const bookmarkDrawerTitle = ref('新增收藏');
const editingBookmarkId = ref<null | string>(null);
const bookmarkSubmitting = ref(false);
const bookmarkForm = ref<BookmarkFormState>(createDefaultBookmarkForm());

const groupsLoading = ref(false);
const groups = ref<ArticleBookmarkGroupResponse[]>([]);
const groupDrawerOpen = ref(false);
const groupDrawerTitle = ref('新增群組');
const editingGroupId = ref<null | string>(null);
const groupSubmitting = ref(false);
const groupForm = ref<CreateArticleBookmarkGroupRequest>({
  name: '',
  sortOrder: 0,
});

const bookmarkColumns = [
  { dataIndex: 'title', title: '標題', width: 260 },
  { dataIndex: 'sourceType', title: '來源', width: 120 },
  { dataIndex: 'groupName', title: '群組', width: 140 },
  { dataIndex: 'tags', title: '標籤', width: 200 },
  { dataIndex: 'state', key: 'state', title: '狀態', width: 160 },
  { dataIndex: 'updatedAt', title: '更新時間', width: 180 },
  { fixed: 'right' as const, key: 'action', title: '操作', width: 250 },
];

const groupColumns = [
  { dataIndex: 'name', title: '名稱', width: 220 },
  { align: 'center' as const, dataIndex: 'sortOrder', title: '排序', width: 100 },
  { align: 'center' as const, dataIndex: 'bookmarkCount', title: '收藏數', width: 100 },
  { fixed: 'right' as const, key: 'action', title: '操作', width: 140 },
];

const hasActiveFilters = computed(
  () =>
    !!filters.value.keyword.trim() ||
    !!filters.value.groupId ||
    filters.value.sourceType !== 'all' ||
    filters.value.pin !== 'all' ||
    filters.value.read !== 'all',
);

function createDefaultBookmarkForm(): BookmarkFormState {
  return {
    sourceType: 'ExternalUrl',
    title: '',
    tags: [],
    isPinned: false,
    isRead: false,
  };
}

function buildBookmarkPayload():
  | CreateArticleBookmarkRequest
  | UpdateArticleBookmarkRequest {
  return {
    sourceType: bookmarkForm.value.sourceType,
    url: bookmarkForm.value.url?.trim() || undefined,
    title: bookmarkForm.value.title.trim(),
    customTitle: bookmarkForm.value.customTitle?.trim() || undefined,
    excerptSnapshot: bookmarkForm.value.excerptSnapshot?.trim() || undefined,
    coverImageUrl: bookmarkForm.value.coverImageUrl?.trim() || undefined,
    note: bookmarkForm.value.note?.trim() || undefined,
    groupId: bookmarkForm.value.groupId || undefined,
    tags: bookmarkForm.value.tags
      .map((tag) => tag.trim())
      .filter((tag) => tag.length > 0),
    isPinned: bookmarkForm.value.isPinned,
    isRead: bookmarkForm.value.isRead,
  };
}

function buildQueryParams(): ArticleBookmarkQueryRequest {
  return {
    keyword: filters.value.keyword.trim() || undefined,
    groupId: filters.value.groupId || undefined,
    sourceType:
      filters.value.sourceType === 'all' ? undefined : filters.value.sourceType,
    isPinned:
      filters.value.pin === 'all'
        ? undefined
        : filters.value.pin === 'pinned',
    isRead:
      filters.value.read === 'all'
        ? undefined
        : filters.value.read === 'read',
    page: bookmarkPagination.value.page,
    pageSize: bookmarkPagination.value.pageSize,
  };
}

function sourceTypeLabel(sourceType: ArticleBookmarkSourceType) {
  return sourceTypeLabelMap[sourceType] ?? sourceType;
}

function formatDate(value: string) {
  return new Date(value).toLocaleString('zh-TW');
}

async function fetchBookmarks() {
  bookmarksLoading.value = true;
  try {
    const response = await getArticleBookmarksApi(buildQueryParams());
    bookmarks.value = response.items;
    bookmarkPagination.value = {
      page: response.page,
      pageSize: response.pageSize,
      total: response.total,
    };
  } finally {
    bookmarksLoading.value = false;
  }
}

async function fetchGroups() {
  groupsLoading.value = true;
  try {
    groups.value = await getArticleBookmarkGroupsApi();
  } finally {
    groupsLoading.value = false;
  }
}

function resetFilters() {
  filters.value = {
    keyword: '',
    pin: 'all',
    read: 'all',
    sourceType: 'all',
  };
  bookmarkPagination.value.page = 1;
  fetchBookmarks();
}

function searchBookmarks() {
  bookmarkPagination.value.page = 1;
  fetchBookmarks();
}

function openCreateBookmark() {
  editingBookmarkId.value = null;
  bookmarkDrawerTitle.value = '新增收藏';
  bookmarkForm.value = createDefaultBookmarkForm();
  bookmarkDrawerOpen.value = true;
}

function openEditBookmark(record: ArticleBookmarkResponse) {
  editingBookmarkId.value = record.id;
  bookmarkDrawerTitle.value = '編輯收藏';
  bookmarkForm.value = {
    sourceType: 'ExternalUrl',
    url: record.url ?? undefined,
    title: record.title,
    customTitle: record.customTitle ?? undefined,
    excerptSnapshot: record.excerptSnapshot ?? undefined,
    coverImageUrl: record.coverImageUrl ?? undefined,
    note: record.note ?? undefined,
    groupId: record.groupId ?? undefined,
    tags: [...record.tags],
    isPinned: record.isPinned,
    isRead: record.isRead,
  };
  bookmarkDrawerOpen.value = true;
}

async function handleBookmarkSubmit() {
  if (!bookmarkForm.value.title.trim()) {
    message.warning('請輸入標題');
    return;
  }

  if (
    bookmarkForm.value.sourceType === 'ExternalUrl' &&
    !bookmarkForm.value.url?.trim()
  ) {
    message.warning('請輸入外部文章連結');
    return;
  }

  bookmarkSubmitting.value = true;
  try {
    const payload = buildBookmarkPayload();

    if (editingBookmarkId.value) {
      await updateArticleBookmarkApi(editingBookmarkId.value, payload);
      message.success('收藏已更新');
    } else {
      await createArticleBookmarkApi(payload);
      message.success('收藏已新增');
    }

    bookmarkDrawerOpen.value = false;
    await Promise.all([fetchBookmarks(), fetchGroups()]);
  } catch {
    message.error(editingBookmarkId.value ? '收藏更新失敗' : '收藏新增失敗');
  } finally {
    bookmarkSubmitting.value = false;
  }
}

async function handleDeleteBookmark(id: string) {
  try {
    await deleteArticleBookmarkApi(id);
    message.success('收藏已刪除');
    await Promise.all([fetchBookmarks(), fetchGroups()]);
  } catch {
    message.error('刪除失敗');
  }
}

async function handleToggleBookmarkState(record: ArticleBookmarkResponse) {
  try {
    await patchArticleBookmarkStateApi(record.id, {
      isPinned: !record.isPinned,
    });
    await fetchBookmarks();
  } catch {
    message.error('置頂狀態更新失敗');
  }
}

async function handleToggleReadState(record: ArticleBookmarkResponse) {
  try {
    await patchArticleBookmarkStateApi(record.id, {
      isRead: !record.isRead,
    });
    await fetchBookmarks();
  } catch {
    message.error('閱讀狀態更新失敗');
  }
}

function openBookmark(record: ArticleBookmarkResponse) {
  if (record.url) {
    window.open(record.url, '_blank', 'noopener,noreferrer');
  } else {
    message.info('目前尚未配置內建文章跳轉入口');
  }
}

function handleBookmarkTableChange(pageInfo: {
  current?: number;
  pageSize?: number;
}) {
  bookmarkPagination.value.page = pageInfo.current ?? 1;
  bookmarkPagination.value.pageSize =
    pageInfo.pageSize ?? bookmarkPagination.value.pageSize;
  fetchBookmarks();
}

function openCreateGroup() {
  editingGroupId.value = null;
  groupDrawerTitle.value = '新增群組';
  groupForm.value = {
    name: '',
    sortOrder: 0,
  };
  groupDrawerOpen.value = true;
}

function openEditGroup(record: ArticleBookmarkGroupResponse) {
  editingGroupId.value = record.id;
  groupDrawerTitle.value = '編輯群組';
  groupForm.value = {
    name: record.name,
    sortOrder: record.sortOrder,
  };
  groupDrawerOpen.value = true;
}

async function handleGroupSubmit() {
  if (!groupForm.value.name.trim()) {
    message.warning('請輸入群組名稱');
    return;
  }

  groupSubmitting.value = true;
  try {
    const payload:
      | CreateArticleBookmarkGroupRequest
      | UpdateArticleBookmarkGroupRequest = {
      name: groupForm.value.name.trim(),
      sortOrder: groupForm.value.sortOrder ?? 0,
    };

    if (editingGroupId.value) {
      await updateArticleBookmarkGroupApi(editingGroupId.value, payload);
      message.success('群組已更新');
    } else {
      await createArticleBookmarkGroupApi(payload);
      message.success('群組已新增');
    }

    groupDrawerOpen.value = false;
    await Promise.all([fetchGroups(), fetchBookmarks()]);
  } catch {
    message.error(editingGroupId.value ? '群組更新失敗' : '群組新增失敗');
  } finally {
    groupSubmitting.value = false;
  }
}

async function handleDeleteGroup(id: string) {
  try {
    await deleteArticleBookmarkGroupApi(id);
    message.success('群組已刪除');
    if (filters.value.groupId === id) {
      filters.value.groupId = undefined;
    }
    await Promise.all([fetchGroups(), fetchBookmarks()]);
  } catch {
    message.error('群組刪除失敗');
  }
}

onMounted(() => {
  fetchGroups();
  fetchBookmarks();
});
</script>

<template>
  <Page auto-content-height>
    <Card :body-style="{ padding: '16px 24px' }">
      <Tabs v-model:activeKey="activeTab">
        <TabPane key="bookmarks" tab="收藏項目">
          <div class="mb-4 flex flex-wrap items-center gap-3">
            <Input
              v-model:value="filters.keyword"
              allow-clear
              class="max-w-[260px]"
              placeholder="搜尋標題、網址、備註"
              @pressEnter="searchBookmarks"
            />
            <Select
              v-model:value="filters.groupId"
              allow-clear
              class="min-w-[160px]"
              placeholder="全部群組"
            >
              <SelectOption
                v-for="group in groups"
                :key="group.id"
                :value="group.id"
              >
                {{ group.name }}
              </SelectOption>
            </Select>
            <Select
              v-model:value="filters.sourceType"
              class="min-w-[140px]"
              placeholder="全部來源"
            >
              <SelectOption value="all">全部來源</SelectOption>
              <SelectOption value="ExternalUrl">外部文章</SelectOption>
              <SelectOption value="InternalArticle">內建文章</SelectOption>
            </Select>
            <Select v-model:value="filters.pin" class="min-w-[140px]">
              <SelectOption value="all">全部置頂狀態</SelectOption>
              <SelectOption value="pinned">只看已置頂</SelectOption>
              <SelectOption value="unpinned">只看未置頂</SelectOption>
            </Select>
            <Select v-model:value="filters.read" class="min-w-[140px]">
              <SelectOption value="all">全部閱讀狀態</SelectOption>
              <SelectOption value="read">只看已讀</SelectOption>
              <SelectOption value="unread">只看未讀</SelectOption>
            </Select>
            <Button type="primary" @click="searchBookmarks">搜尋</Button>
            <Button v-if="hasActiveFilters" @click="resetFilters">重設</Button>
            <div class="ml-auto">
              <Button type="primary" @click="openCreateBookmark">
                新增收藏
              </Button>
            </div>
          </div>

          <Table
            :columns="bookmarkColumns"
            :data-source="bookmarks"
            :loading="bookmarksLoading"
            :pagination="{
              current: bookmarkPagination.page,
              pageSize: bookmarkPagination.pageSize,
              total: bookmarkPagination.total,
              showSizeChanger: true,
            }"
            row-key="id"
            size="middle"
            @change="handleBookmarkTableChange"
          >
            <template #bodyCell="{ column, record }">
              <template v-if="column.dataIndex === 'title'">
                <div class="min-w-0">
                  <div class="truncate font-medium">
                    {{ record.customTitle || record.title }}
                  </div>
                  <div class="truncate text-xs text-muted-foreground">
                    {{ record.url || record.sourceId || '—' }}
                  </div>
                </div>
              </template>

              <template v-else-if="column.dataIndex === 'sourceType'">
                <Tag color="blue">
                  {{ sourceTypeLabel(record.sourceType) }}
                </Tag>
              </template>

              <template v-else-if="column.dataIndex === 'groupName'">
                {{ record.groupName || '未分組' }}
              </template>

              <template v-else-if="column.dataIndex === 'tags'">
                <div class="flex flex-wrap gap-1">
                  <Tag v-for="tag in record.tags" :key="tag">{{ tag }}</Tag>
                  <span v-if="record.tags.length === 0" class="text-muted-foreground">
                    —
                  </span>
                </div>
              </template>

              <template v-else-if="column.key === 'state'">
                <div class="flex flex-wrap gap-1">
                  <Tag :color="record.isPinned ? 'gold' : 'default'">
                    {{ record.isPinned ? '已置頂' : '未置頂' }}
                  </Tag>
                  <Tag :color="record.isRead ? 'green' : 'default'">
                    {{ record.isRead ? '已讀' : '未讀' }}
                  </Tag>
                </div>
              </template>

              <template v-else-if="column.dataIndex === 'updatedAt'">
                {{ formatDate(record.updatedAt) }}
              </template>

              <template v-else-if="column.key === 'action'">
                <div class="flex flex-wrap gap-2">
                  <Button
                    size="small"
                    type="link"
                    @click="openBookmark(record as ArticleBookmarkResponse)"
                  >
                    開啟
                  </Button>
                  <Button
                    size="small"
                    type="link"
                    @click="openEditBookmark(record as ArticleBookmarkResponse)"
                  >
                    編輯
                  </Button>
                  <Button
                    size="small"
                    type="link"
                    @click="
                      handleToggleBookmarkState(record as ArticleBookmarkResponse)
                    "
                  >
                    {{ record.isPinned ? '取消置頂' : '置頂' }}
                  </Button>
                  <Button
                    size="small"
                    type="link"
                    @click="handleToggleReadState(record as ArticleBookmarkResponse)"
                  >
                    {{ record.isRead ? '標記未讀' : '標記已讀' }}
                  </Button>
                  <Popconfirm
                    cancel-text="取消"
                    ok-text="確認刪除"
                    ok-type="danger"
                    :title="`確定要刪除「${record.customTitle || record.title}」嗎？`"
                    @confirm="handleDeleteBookmark(record.id as string)"
                  >
                    <Button danger size="small" type="link">刪除</Button>
                  </Popconfirm>
                </div>
              </template>
            </template>

            <template #emptyText>
              <div class="py-8 text-center text-muted-foreground">
                尚無收藏資料，請點擊「新增收藏」開始建立
              </div>
            </template>
          </Table>
        </TabPane>

        <TabPane key="groups" tab="群組管理">
          <div class="mb-4 flex items-center justify-between">
            <h3 class="text-lg font-medium">群組管理</h3>
            <Button type="primary" @click="openCreateGroup">新增群組</Button>
          </div>

          <Table
            :columns="groupColumns"
            :data-source="groups"
            :loading="groupsLoading"
            :pagination="false"
            row-key="id"
            size="middle"
          >
            <template #bodyCell="{ column, record }">
              <template v-if="column.key === 'action'">
                <div class="flex gap-2">
                  <Button
                    size="small"
                    type="link"
                    @click="openEditGroup(record as ArticleBookmarkGroupResponse)"
                  >
                    編輯
                  </Button>
                  <Popconfirm
                    cancel-text="取消"
                    ok-text="確認刪除"
                    ok-type="danger"
                    :title="`確定要刪除群組「${record.name}」嗎？`"
                    @confirm="handleDeleteGroup(record.id as string)"
                  >
                    <Button danger size="small" type="link">刪除</Button>
                  </Popconfirm>
                </div>
              </template>
            </template>

            <template #emptyText>
              <div class="py-8 text-center text-muted-foreground">
                尚無群組資料，請點擊「新增群組」建立
              </div>
            </template>
          </Table>
        </TabPane>
      </Tabs>
    </Card>

    <Drawer
      destroy-on-close
      :open="bookmarkDrawerOpen"
      placement="right"
      :title="bookmarkDrawerTitle"
      :width="480"
      @close="bookmarkDrawerOpen = false"
    >
      <Form :model="bookmarkForm" layout="vertical">
        <FormItem label="文章連結" required>
          <Input
            v-model:value="bookmarkForm.url"
            placeholder="https://example.com/article"
          />
        </FormItem>

        <FormItem label="標題" required>
          <Input
            v-model:value="bookmarkForm.title"
            :maxlength="300"
            placeholder="請輸入文章標題"
            show-count
          />
        </FormItem>

        <FormItem label="自訂標題">
          <Input
            v-model:value="bookmarkForm.customTitle"
            :maxlength="300"
            placeholder="可選，覆蓋顯示標題"
            show-count
          />
        </FormItem>

        <FormItem label="群組">
          <Select
            v-model:value="bookmarkForm.groupId"
            allow-clear
            placeholder="未分組"
          >
            <SelectOption
              v-for="group in groups"
              :key="group.id"
              :value="group.id"
            >
              {{ group.name }}
            </SelectOption>
          </Select>
        </FormItem>

        <FormItem label="標籤">
          <Select
            v-model:value="bookmarkForm.tags"
            mode="tags"
            placeholder="輸入標籤後按 Enter"
            :token-separators="[',']"
          />
        </FormItem>

        <FormItem label="摘要">
          <Input.TextArea
            v-model:value="bookmarkForm.excerptSnapshot"
            :rows="3"
            placeholder="可選，保留收藏時的摘要"
          />
        </FormItem>

        <FormItem label="封面圖片網址">
          <Input
            v-model:value="bookmarkForm.coverImageUrl"
            placeholder="https://example.com/cover.jpg"
          />
        </FormItem>

        <FormItem label="備註">
          <Input.TextArea
            v-model:value="bookmarkForm.note"
            :rows="3"
            placeholder="可選，紀錄閱讀脈絡"
          />
        </FormItem>

        <div class="grid grid-cols-2 gap-4">
          <FormItem label="置頂">
            <Switch
              v-model:checked="bookmarkForm.isPinned"
              checked-children="已置頂"
              un-checked-children="未置頂"
            />
          </FormItem>
          <FormItem label="已讀">
            <Switch
              v-model:checked="bookmarkForm.isRead"
              checked-children="已讀"
              un-checked-children="未讀"
            />
          </FormItem>
        </div>
      </Form>

      <template #extra>
        <Button
          :loading="bookmarkSubmitting"
          type="primary"
          @click="handleBookmarkSubmit"
        >
          儲存
        </Button>
      </template>
    </Drawer>

    <Drawer
      destroy-on-close
      :open="groupDrawerOpen"
      placement="right"
      :title="groupDrawerTitle"
      :width="400"
      @close="groupDrawerOpen = false"
    >
      <Form :model="groupForm" layout="vertical">
        <FormItem label="群組名稱" required>
          <Input
            v-model:value="groupForm.name"
            :maxlength="100"
            placeholder="請輸入群組名稱"
            show-count
          />
        </FormItem>
        <FormItem label="排序">
          <InputNumber
            v-model:value="groupForm.sortOrder"
            :min="0"
            placeholder="0"
            :style="{ width: '100%' }"
          />
        </FormItem>
      </Form>

      <template #extra>
        <Button
          :loading="groupSubmitting"
          type="primary"
          @click="handleGroupSubmit"
        >
          儲存
        </Button>
      </template>
    </Drawer>
  </Page>
</template>

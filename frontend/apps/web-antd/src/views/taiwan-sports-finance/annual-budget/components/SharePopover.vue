<script setup lang="ts">
import { ref, watch } from 'vue';

import {
  Button,
  Input,
  message,
  Popconfirm,
  Popover,
  Space,
  Spin,
  Switch,
  Tag,
  Tooltip,
} from 'ant-design-vue';

import type { DepartmentShareInfoResponse } from '#/api';

import {
  getOrCreateShareApi,
  regenerateShareApi,
  revokeShareApi,
  updateShareApi,
} from '#/api';

const props = defineProps<{
  departmentId: string;
  departmentName: string;
}>();

const open = ref(false);
const loading = ref(false);
const shareInfo = ref<DepartmentShareInfoResponse | null>(null);

const shareUrl = () => {
  if (!shareInfo.value) return '';
  const base = window.location.origin;
  return `${base}/public/department-budget/${shareInfo.value.token}`;
};

async function fetchShareInfo() {
  loading.value = true;
  try {
    shareInfo.value = await getOrCreateShareApi(props.departmentId);
  } catch {
    shareInfo.value = null;
  } finally {
    loading.value = false;
  }
}

async function handleRegenerate() {
  loading.value = true;
  try {
    shareInfo.value = await regenerateShareApi(props.departmentId);
    message.success('連結已重新產生（舊連結已失效）');
  } catch {
    message.error('重新產生失敗');
  } finally {
    loading.value = false;
  }
}

async function handleToggleActive(active: boolean) {
  if (!shareInfo.value) return;
  try {
    shareInfo.value = await updateShareApi(props.departmentId, {
      isActive: active,
    });
    message.success(active ? '連結已啟用' : '連結已停用');
  } catch {
    message.error('更新失敗');
  }
}

async function handleTogglePermission() {
  if (!shareInfo.value) return;
  const newPerm =
    shareInfo.value.permission === 'Editable' ? 'ReadOnly' : 'Editable';
  try {
    shareInfo.value = await updateShareApi(props.departmentId, {
      permission: newPerm,
    });
    message.success(`已切換為${newPerm === 'Editable' ? '可編輯' : '唯讀'}`);
  } catch {
    message.error('更新失敗');
  }
}

async function handleRevoke() {
  try {
    await revokeShareApi(props.departmentId);
    shareInfo.value = null;
    message.success('分享連結已撤銷');
  } catch {
    message.error('撤銷失敗');
  }
}

function handleCopy() {
  navigator.clipboard.writeText(shareUrl());
  message.success('已複製連結');
}

watch(open, (val) => {
  if (val) fetchShareInfo();
});
</script>

<template>
  <Popover
    v-model:open="open"
    :destroy-tooltip-on-hide="false"
    placement="left"
    title="分享連結管理"
    trigger="click"
  >
    <template #content>
      <Spin :spinning="loading">
        <div style="width: 380px">
          <!-- 已有分享連結（自動建立，所以一打開就有） -->
          <template v-if="shareInfo">
            <!-- 連結 + 複製 -->
            <div class="mb-3">
              <label class="mb-1 block text-xs text-gray-500">分享網址</label>
              <Space.Compact block>
                <Input :value="shareUrl()" readonly size="small" />
                <Tooltip title="複製連結">
                  <Button size="small" type="primary" @click="handleCopy">
                    複製
                  </Button>
                </Tooltip>
              </Space.Compact>
              <p class="mt-1 text-xs text-gray-400">
                此連結為「{{ props.departmentName }}」的永久入口，部門人員可自行選擇年度填寫
              </p>
            </div>

            <!-- 權限 -->
            <div class="mb-3 flex items-center justify-between">
              <span class="text-sm">權限</span>
              <Space>
                <Tag
                  :color="
                    shareInfo.permission === 'Editable' ? 'green' : 'orange'
                  "
                >
                  {{ shareInfo.permission === 'Editable' ? '可編輯' : '唯讀' }}
                </Tag>
                <Button size="small" @click="handleTogglePermission">
                  切換
                </Button>
              </Space>
            </div>

            <!-- 啟用/停用 -->
            <div class="mb-3 flex items-center justify-between">
              <span class="text-sm">啟用狀態</span>
              <Switch
                :checked="shareInfo.isActive"
                checked-children="啟用"
                un-checked-children="停用"
                @change="(v: boolean | string | number) => handleToggleActive(Boolean(v))"
              />
            </div>

            <!-- 操作按鈕 -->
            <div class="flex justify-between border-t pt-3">
              <Popconfirm
                cancel-text="取消"
                ok-text="重新產生"
                title="舊連結將立即失效，確定要重新產生？"
                @confirm="handleRegenerate"
              >
                <Button size="small">重新產生</Button>
              </Popconfirm>
              <Popconfirm
                cancel-text="取消"
                ok-text="撤銷"
                ok-type="danger"
                title="確定要撤銷此分享連結？"
                @confirm="handleRevoke"
              >
                <Button danger size="small">撤銷連結</Button>
              </Popconfirm>
            </div>
          </template>

          <!-- 載入中或錯誤 -->
          <template v-else-if="!loading">
            <p class="text-gray-500">無法載入分享資訊</p>
          </template>
        </div>
      </Spin>
    </template>

    <!-- 觸發按鈕 -->
    <Tooltip title="分享連結">
      <Button size="small" type="link">分享</Button>
    </Tooltip>
  </Popover>
</template>

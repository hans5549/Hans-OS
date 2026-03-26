<script lang="ts" setup>
import { onMounted, ref } from 'vue';

import {
  Button,
  Form,
  FormItem,
  Input,
  message,
  Modal,
  Popconfirm,
  Table,
  Textarea,
} from 'ant-design-vue';

import type {
  CreateDepartmentRequest,
  DepartmentResponse,
  UpdateDepartmentRequest,
} from '#/api';

import {
  createDepartmentApi,
  deleteDepartmentApi,
  getDepartmentsApi,
  updateDepartmentApi,
} from '#/api';

const loading = ref(false);
const departments = ref<DepartmentResponse[]>([]);
const modalVisible = ref(false);
const modalTitle = ref('新增部門');
const editingId = ref<null | string>(null);
const submitting = ref(false);

const formState = ref<CreateDepartmentRequest>({
  name: '',
  note: '',
});

const columns = [
  {
    dataIndex: 'name',
    title: '部門名稱',
    width: 200,
  },
  {
    dataIndex: 'note',
    title: '備註',
    ellipsis: true,
  },
  {
    fixed: 'right' as const,
    key: 'action',
    title: '操作',
    width: 160,
  },
];

async function fetchDepartments() {
  loading.value = true;
  try {
    departments.value = await getDepartmentsApi();
  } finally {
    loading.value = false;
  }
}

function openCreateModal() {
  editingId.value = null;
  modalTitle.value = '新增部門';
  formState.value = { name: '', note: '' };
  modalVisible.value = true;
}

function openEditModal(record: DepartmentResponse) {
  editingId.value = record.id;
  modalTitle.value = '編輯部門';
  formState.value = { name: record.name, note: record.note ?? '' };
  modalVisible.value = true;
}

async function handleSubmit() {
  if (!formState.value.name.trim()) {
    message.warning('請輸入部門名稱');
    return;
  }

  submitting.value = true;
  try {
    if (editingId.value) {
      const data: UpdateDepartmentRequest = {
        name: formState.value.name.trim(),
        note: formState.value.note || undefined,
      };
      await updateDepartmentApi(editingId.value, data);
      message.success('部門已更新');
    } else {
      const data: CreateDepartmentRequest = {
        name: formState.value.name.trim(),
        note: formState.value.note || undefined,
      };
      await createDepartmentApi(data);
      message.success('部門已新增');
    }
    modalVisible.value = false;
    await fetchDepartments();
  } finally {
    submitting.value = false;
  }
}

async function handleDelete(id: string) {
  try {
    await deleteDepartmentApi(id);
    message.success('部門已刪除');
    await fetchDepartments();
  } catch {
    message.error('刪除失敗');
  }
}

onMounted(fetchDepartments);
</script>

<template>
  <div>
    <div class="mb-4 flex items-center justify-between">
      <h3 class="text-lg font-medium">體育部門管理</h3>
      <Button type="primary" @click="openCreateModal">
        <template #icon>
          <span class="i-lucide-plus" />
        </template>
        新增部門
      </Button>
    </div>

    <Table
      :columns="columns"
      :data-source="departments"
      :loading="loading"
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
              @click="openEditModal(record as unknown as DepartmentResponse)"
            >
              編輯
            </Button>
            <Popconfirm
              cancel-text="取消"
              ok-text="確認刪除"
              ok-type="danger"
              :title="`確定要刪除「${record.name}」嗎？`"
              @confirm="handleDelete(record.id as string)"
            >
              <Button danger size="small" type="link"> 刪除 </Button>
            </Popconfirm>
          </div>
        </template>
      </template>

      <template #emptyText>
        <div class="py-8 text-center text-gray-400">
          尚無部門資料，請點擊「新增部門」按鈕建立
        </div>
      </template>
    </Table>

    <Modal
      :confirm-loading="submitting"
      destroy-on-close
      ok-text="儲存"
      :title="modalTitle"
      :open="modalVisible"
      @cancel="modalVisible = false"
      @ok="handleSubmit"
    >
      <Form :model="formState" layout="vertical" class="mt-4">
        <FormItem label="部門名稱" required>
          <Input
            v-model:value="formState.name"
            :maxlength="100"
            placeholder="請輸入部門名稱"
            show-count
          />
        </FormItem>
        <FormItem label="備註">
          <Textarea
            v-model:value="formState.note"
            :auto-size="{ minRows: 2, maxRows: 4 }"
            :maxlength="500"
            placeholder="選填備註"
            show-count
          />
        </FormItem>
      </Form>
    </Modal>
  </div>
</template>

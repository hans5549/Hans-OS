import { computed, ref } from 'vue';

/**
 * Todo 多選狀態管理（模組內 module-singleton）。
 *
 * 提供 toggle / range select / clear，給 TodoListPanel 的批次操作 toolbar
 * 與 TodoItemRow 的 hover-checkbox 共用。
 */

const selectedIds = ref<Set<string>>(new Set());

/** 當前依視覺順序排好的 itemId list（每次 list render 時由 panel 寫入，作為 shift+click range 的基準） */
const orderedIds = ref<string[]>([]);

let lastClickedId: null | string = null;

export function useTodoSelection() {
  const count = computed(() => selectedIds.value.size);
  const selected = computed(() => selectedIds.value);
  const hasSelection = computed(() => selectedIds.value.size > 0);

  function isSelected(id: string): boolean {
    return selectedIds.value.has(id);
  }

  function toggle(id: string) {
    const next = new Set(selectedIds.value);
    if (next.has(id)) next.delete(id);
    else next.add(id);
    selectedIds.value = next;
    lastClickedId = id;
  }

  function selectRange(toId: string) {
    if (!lastClickedId || !orderedIds.value.includes(lastClickedId)) {
      toggle(toId);
      return;
    }
    const fromIdx = orderedIds.value.indexOf(lastClickedId);
    const toIdx = orderedIds.value.indexOf(toId);
    if (fromIdx === -1 || toIdx === -1) {
      toggle(toId);
      return;
    }
    const [start, end] =
      fromIdx < toIdx ? [fromIdx, toIdx] : [toIdx, fromIdx];
    const next = new Set(selectedIds.value);
    for (let i = start; i <= end; i++) {
      const id = orderedIds.value[i];
      if (id) next.add(id);
    }
    selectedIds.value = next;
    lastClickedId = toId;
  }

  function clear() {
    selectedIds.value = new Set();
    lastClickedId = null;
  }

  function setOrderedIds(ids: string[]) {
    orderedIds.value = ids;
    // 自動清掉已不存在於目前清單的選取
    if (selectedIds.value.size > 0) {
      const idSet = new Set(ids);
      const next = new Set<string>();
      for (const id of selectedIds.value) {
        if (idSet.has(id)) next.add(id);
      }
      selectedIds.value = next;
    }
  }

  return {
    clear,
    count,
    hasSelection,
    isSelected,
    selected,
    selectRange,
    setOrderedIds,
    toggle,
  };
}

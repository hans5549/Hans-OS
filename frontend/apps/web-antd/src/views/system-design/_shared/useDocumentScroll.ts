import {
  computed,
  onBeforeUnmount,
  onMounted,
  ref,
  type Ref,
} from 'vue';

import type { DocSection } from './types';

/**
 * 追蹤捲動進度 + 當前可見章節，用於進度條與 TOC 高亮
 */
export function useDocumentScroll(sections: Ref<DocSection[]>) {
  const activeSectionId = ref<null | string>(null);

  const activeSectionIndex = computed(() => {
    if (!activeSectionId.value) return -1;
    return sections.value.findIndex((s) => s.id === activeSectionId.value);
  });

  const progressPercent = computed(() => {
    if (activeSectionIndex.value === -1 || sections.value.length === 0)
      return 0;
    return Math.round(
      ((activeSectionIndex.value + 1) / sections.value.length) * 100,
    );
  });

  let observer: IntersectionObserver | null = null;
  const visibilityMap = new Map<string, number>();

  const observerOptions: IntersectionObserverInit = {
    rootMargin: '-18% 0px -58% 0px',
    threshold: [0.2, 0.35, 0.5, 0.65],
  };

  function updateActiveSection(entries: IntersectionObserverEntry[]) {
    for (const entry of entries) {
      visibilityMap.set(
        entry.target.id,
        entry.isIntersecting ? entry.intersectionRatio : 0,
      );
    }

    const next = [...visibilityMap.entries()]
      .filter(([, ratio]) => ratio > 0)
      .sort((a, b) => b[1] - a[1])[0]?.[0];

    if (next) {
      activeSectionId.value = next;
    }
  }

  function observeSections() {
    const elements = sections.value
      .map((s) => document.getElementById(s.id))
      .filter((el): el is HTMLElement => el !== null);

    if (elements.length === 0) return;

    if (typeof IntersectionObserver === 'undefined') {
      activeSectionId.value = sections.value[0]?.id ?? null;
      return;
    }

    observer = new IntersectionObserver(updateActiveSection, observerOptions);
    for (const el of elements) {
      observer.observe(el);
    }
  }

  function scrollToSection(sectionId: string) {
    document.getElementById(sectionId)?.scrollIntoView({ behavior: 'smooth' });
  }

  onMounted(observeSections);

  onBeforeUnmount(() => {
    observer?.disconnect();
    visibilityMap.clear();
  });

  return {
    activeSectionId,
    activeSectionIndex,
    progressPercent,
    scrollToSection,
  };
}

import { mount } from '@vue/test-utils';

import { describe, expect, it, vi } from 'vitest';

vi.mock('@vben/common-ui', () => ({
  Page: {
    props: ['title', 'contentClass'],
    template: '<main :class="contentClass"><slot /></main>',
  },
}));

import TsfGlassPage from '../TsfGlassPage.vue';
import TsfMetricCard from '../TsfMetricCard.vue';

describe('taiwan sports finance glass components', () => {
  it('renders the glass page title, subtitle, icon, actions, filters and content', () => {
    const wrapper = mount(TsfGlassPage, {
      props: {
        icon: 'i-lucide-landmark',
        subtitle: '年度預算與活動費控管',
        title: '台灣體育部財務',
      },
      slots: {
        actions: '<button>新增</button>',
        default: '<section>主要內容</section>',
        filters: '<div>年度篩選</div>',
      },
    });

    expect(wrapper.text()).toContain('台灣體育部財務');
    expect(wrapper.text()).toContain('年度預算與活動費控管');
    expect(wrapper.text()).toContain('新增');
    expect(wrapper.text()).toContain('年度篩選');
    expect(wrapper.text()).toContain('主要內容');
    expect(wrapper.find('.i-lucide-landmark').exists()).toBe(true);
  });

  it('renders metric values with tone, icon, prefix and suffix', () => {
    const wrapper = mount(TsfMetricCard, {
      props: {
        icon: 'i-lucide-wallet',
        label: '期末餘額',
        prefix: '$',
        suffix: 'TWD',
        tone: 'info',
        value: '106,022',
      },
    });

    expect(wrapper.text()).toContain('期末餘額');
    expect(wrapper.text()).toContain('$');
    expect(wrapper.text()).toContain('106,022');
    expect(wrapper.text()).toContain('TWD');
    expect(wrapper.classes()).toContain('tsf-metric-card--info');
    expect(wrapper.find('.i-lucide-wallet').exists()).toBe(true);
  });
});

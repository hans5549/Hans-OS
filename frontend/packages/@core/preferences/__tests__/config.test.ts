import { describe, expect, it } from 'vitest';

import { defaultPreferences } from '../src/config';

describe('defaultPreferences immutability test', () => {
  // 建立快照，確保默认配置物件不被修改
  it('should not modify the config object', () => {
    expect(defaultPreferences).toMatchSnapshot();
  });
});

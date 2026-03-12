/**
 * 根據指定字段对物件数组进行去重
 * @param arr 要去重的物件数组
 * @param key 去重依据的字段名
 * @returns 去重后的物件数组
 */
function uniqueByField<T>(arr: T[], key: keyof T): T[] {
  const seen = new Map<any, T>();
  return arr.filter((item) => {
    const value = item[key];
    return seen.has(value) ? false : (seen.set(value, item), true);
  });
}

export { uniqueByField };

import { posix } from 'node:path';

/**
 * 将给定的文件路徑转换为 POSIX 风格。
 * @param {string} pathname - 原始文件路徑。
 */
function toPosixPath(pathname: string) {
  return pathname.split(`\\`).join(posix.sep);
}

export { toPosixPath };

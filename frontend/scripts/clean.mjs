import { promises as fs } from 'node:fs';
import { join, normalize } from 'node:path';

const rootDir = process.cwd();

// 控制並發數量，避免建立過多的並發任務
const CONCURRENCY_LIMIT = 10;

// 需要跳過的目錄，避免進入這些目錄進行清理
const SKIP_DIRS = new Set(['.DS_Store', '.git', '.idea', '.vscode']);

/**
 * 處理單一檔案/目錄項目
 * @param {string} currentDir - 目前目錄路徑
 * @param {string} item - 檔案/目錄名稱
 * @param {string[]} targets - 要刪除的目標列表
 * @param {number} _depth - 目前遞迴深度
 * @returns {Promise<boolean>} - 是否需要進一步遞迴處理
 */
async function processItem(currentDir, item, targets, _depth) {
  // 跳過特殊目錄
  if (SKIP_DIRS.has(item)) {
    return false;
  }

  try {
    const itemPath = normalize(join(currentDir, item));

    if (targets.includes(item)) {
      // 匹配到目標目錄或檔案時直接刪除
      await fs.rm(itemPath, { force: true, recursive: true });
      console.log(`✅ Deleted: ${itemPath}`);
      return false; // 已刪除，不需遞迴
    }

    // 使用 readdir 的 withFileTypes 選項，避免額外的 lstat 呼叫
    return true; // 可能需要遞迴，由呼叫端決定
  } catch (error) {
    // 更詳細的錯誤資訊
    if (error.code === 'ENOENT') {
      // 檔案不存在，可能已被刪除，這是正常情況
      return false;
    } else if (error.code === 'EPERM' || error.code === 'EACCES') {
      console.error(`❌ Permission denied: ${item} in ${currentDir}`);
    } else {
      console.error(
        `❌ Error handling item ${item} in ${currentDir}: ${error.message}`,
      );
    }
    return false;
  }
}

/**
 * 遞迴查找並刪除目標目錄（並發最佳化版本）
 * @param {string} currentDir - 目前遍歷的目錄路徑
 * @param {string[]} targets - 要刪除的目標列表
 * @param {number} depth - 目前遞迴深度，避免過深遞迴
 */
async function cleanTargetsRecursively(currentDir, targets, depth = 0) {
  // 限制遞迴深度，避免無限遞迴
  if (depth > 10) {
    console.warn(`Max recursion depth reached at: ${currentDir}`);
    return;
  }

  let dirents;
  try {
    // 使用 withFileTypes 選項，一次取得檔案類型資訊，避免後續 lstat 呼叫
    dirents = await fs.readdir(currentDir, { withFileTypes: true });
  } catch (error) {
    // 如果無法讀取目錄，可能已被刪除或權限不足
    console.warn(`Cannot read directory ${currentDir}: ${error.message}`);
    return;
  }

  // 分批處理，控制並發數量
  for (let i = 0; i < dirents.length; i += CONCURRENCY_LIMIT) {
    const batch = dirents.slice(i, i + CONCURRENCY_LIMIT);

    const tasks = batch.map(async (dirent) => {
      const item = dirent.name;
      const shouldRecurse = await processItem(currentDir, item, targets, depth);

      // 如果是目錄且尚未被刪除，則遞迴處理
      if (shouldRecurse && dirent.isDirectory()) {
        const itemPath = normalize(join(currentDir, item));
        return cleanTargetsRecursively(itemPath, targets, depth + 1);
      }

      return null;
    });

    // 並發執行目前批次的任務
    const results = await Promise.allSettled(tasks);

    // 檢查是否有失敗的任務（可選：用於除錯）
    const failedTasks = results.filter(
      (result) => result.status === 'rejected',
    );
    if (failedTasks.length > 0) {
      console.warn(
        `${failedTasks.length} tasks failed in batch starting at index ${i} in directory: ${currentDir}`,
      );
    }
  }
}

(async function startCleanup() {
  // 要刪除的目錄與檔案名稱
  const targets = ['node_modules', 'dist', '.turbo', 'dist.zip'];
  const deleteLockFile = process.argv.includes('--del-lock');
  const cleanupTargets = [...targets];

  if (deleteLockFile) {
    cleanupTargets.push('pnpm-lock.yaml');
  }

  console.log(
    `🚀 Starting cleanup of targets: ${cleanupTargets.join(', ')} from root: ${rootDir}`,
  );

  const startTime = Date.now();

  try {
    // 先統計要刪除的目標數量
    console.log('📊 Scanning for cleanup targets...');

    await cleanTargetsRecursively(rootDir, cleanupTargets);

    const endTime = Date.now();
    const duration = (endTime - startTime) / 1000;

    console.log(
      `✨ Cleanup process completed successfully in ${duration.toFixed(2)}s`,
    );
  } catch (error) {
    console.error(`💥 Unexpected error during cleanup: ${error.message}`);
    process.exit(1);
  }
})();

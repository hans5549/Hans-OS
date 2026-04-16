<script setup lang="ts">
import { Card, Empty } from 'ant-design-vue';

defineOptions({ name: 'QrCodeGeneratorPracticeTab' });
</script>

<template>
  <div class="mx-auto flex w-full max-w-7xl flex-col items-center justify-center gap-6 p-4 md:p-6 lg:p-8">
    <Card :bordered="false" class="practice-placeholder w-full">
      <div class="flex flex-col items-center gap-4 py-16">
        <Empty description="">
          <template #description>
            <div class="space-y-2 text-center">
              <p class="text-lg font-semibold text-foreground">
                實作練習區域
              </p>
              <p class="text-sm text-muted-foreground">
                這裡將用來實作 QR Code 生成器的練習
              </p>
            </div>
          </template>
        </Empty>
      </div>
    </Card>
  </div>
</template>

<style scoped>
.practice-placeholder {
  border: 1px solid hsl(var(--border));
  background:
    linear-gradient(180deg, hsl(var(--card)) 0%, hsl(var(--background)) 100%);
  box-shadow: 0 24px 64px -56px hsl(var(--foreground) / 0.35);
}
</style>

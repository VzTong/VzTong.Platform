<script setup lang="ts">
/**
 * Placeholder Phase 2.
 * Users: list/search/khóa (isActive)/gán role — sẽ gọi /api/identity/users.
 * Roles: list + permission grants — /api/identity/roles.
 * Hiện chỉ tiêu đề i18n để hoàn thiện shell navigation.
 */
import { useI18n } from 'vue-i18n'

const { t } = useI18n()
</script>

<template>
  <div>
    <header class="page-head">
      <h1>{{ t('roles.title') }}</h1>
      <p>{{ t('roles.subtitle') }}</p>
    </header>
  </div>
</template>

<style scoped>
.page-head h1 {
  margin: 0;
  font-family: var(--font-display);
  font-size: 1.5rem;
  letter-spacing: 0.06em;
  text-transform: uppercase;
  color: var(--cyan);
}

.page-head p {
  margin: 0.4rem 0 0;
  color: var(--mute);
}
</style>
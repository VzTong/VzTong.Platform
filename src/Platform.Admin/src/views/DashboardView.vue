<script setup lang="ts">
/**
 * Dashboard MVP.
 *
 * Gọi GET /api/identity/users?MaxResultCount=1 chỉ để lấy totalCount
 * (không cần load full list). Cần Bearer admin + permission Users.
 *
 * Lỗi mạng/403: hiện message i18n, không crash trang.
 * Stagger fade các ô số liệu bằng animejs (optional UX).
 */
import { ref, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import http from '@/api/http'
import { animate, stagger } from 'animejs'

const { t } = useI18n()
const totalUsers = ref<number | null>(null)
const error = ref('')

onMounted(async () => {
  try {
    const { data } = await http.get('/api/identity/users', {
      params: { MaxResultCount: 1 },
    })
    totalUsers.value = data.totalCount ?? data.items?.length ?? 0
  } catch {
    error.value = t('dashboard.loadError')
    totalUsers.value = null
  }

  const cards = document.querySelectorAll('.stat')
  if (cards.length && !window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
    animate(cards, {
      opacity: [0, 1],
      translateY: [10, 0],
      delay: stagger(80),
      duration: 420,
      ease: 'outCubic',
    })
  }
})
</script>

<template>
  <div>
    <header class="page-head">
      <h1>{{ t('dashboard.title') }}</h1>
      <p>{{ t('dashboard.subtitle') }}</p>
    </header>

    <p v-if="error" class="err">{{ error }}</p>

    <div class="stats">
      <div class="stat">
        <span class="label">{{ t('dashboard.users') }}</span>
        <span class="value">{{ totalUsers === null ? '—' : totalUsers }}</span>
      </div>
      <div class="stat">
        <span class="label">{{ t('dashboard.portal') }}</span>
        <span class="value mono">Admin</span>
      </div>
    </div>
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
  margin: 0.4rem 0 1.5rem;
  color: var(--mute);
}

.stats {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(160px, 1fr));
  gap: 1rem;
}

.stat {
  background: var(--panel);
  border: 1px solid var(--line);
  border-radius: var(--radius);
  padding: 1.1rem 1.25rem;
  box-shadow: var(--glow);
}

.label {
  display: block;
  font-family: var(--font-mono);
  font-size: 0.7rem;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: var(--mute);
  margin-bottom: 0.35rem;
}

.value {
  font-family: var(--font-mono);
  font-size: 1.75rem;
  font-weight: 600;
  color: var(--text);
}

.mono {
  font-size: 1.2rem;
  color: var(--cyan);
}

.err {
  color: var(--danger);
  font-family: var(--font-mono);
  font-size: 0.85rem;
  margin-bottom: 1rem;
}
</style>
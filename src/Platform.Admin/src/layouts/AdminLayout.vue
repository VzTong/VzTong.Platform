<script setup lang="ts">
/**
 * Layout khung portal sau khi đăng nhập.
 *
 * - Sidebar: brand + nav (Dashboard / Users / Roles) + user + đổi ngôn ngữ + logout
 * - Strip gradient trên cùng: signature UI "authority"
 * - RouterView: render child (dashboard, users, roles)
 *
 * Logout chỉ xóa token local rồi về /login; session server (refresh) có thể còn
 * đến khi hết hạn hoặc revoke — đủ cho MVP.
 */
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth'
import { useRouter, useRoute } from 'vue-router'
import { setLocale } from '@/i18n'

const { t, locale } = useI18n()
const auth = useAuthStore()
const router = useRouter()
const route = useRoute()

function logout() {
  auth.logout()
  router.push({ name: 'login' })
}

function toggleLang() {
  setLocale(locale.value === 'vi' ? 'en' : 'vi')
}
</script>

<template>
  <div class="shell">
    <div class="strip" aria-hidden="true" />

    <aside class="side">
      <div class="brand">
        <span class="brand-mark">P</span>
        <div>
          <div class="brand-title">{{ t('app.name') }}</div>
          <div class="brand-sub">{{ t('app.admin') }}</div>
        </div>
      </div>

      <nav>
        <RouterLink to="/" :class="{ active: route.name === 'dashboard' }">
          {{ t('nav.dashboard') }}
        </RouterLink>
        <RouterLink to="/users" :class="{ active: route.name === 'users' }">
          {{ t('nav.users') }}
        </RouterLink>
        <RouterLink to="/roles" :class="{ active: route.name === 'roles' }">
          {{ t('nav.roles') }}
        </RouterLink>
      </nav>

      <div class="side-foot">
        <span class="user">{{ auth.userName }}</span>
        <button type="button" class="linkish" @click="toggleLang">
          {{ locale === 'vi' ? 'EN' : 'VI' }}
        </button>
        <button type="button" class="linkish" @click="logout">
          {{ t('nav.signOut') }}
        </button>
      </div>
    </aside>

    <main class="main">
      <RouterView />
    </main>
  </div>
</template>

<style scoped>
.shell {
  display: grid;
  grid-template-columns: 220px 1fr;
  min-height: 100vh;
}

.strip {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  height: 2px;
  background: linear-gradient(90deg, var(--cyan), var(--magenta));
  z-index: 20;
}

.side {
  background: var(--panel);
  border-right: 1px solid var(--line);
  display: flex;
  flex-direction: column;
  padding: 1.25rem 0.85rem;
  gap: 1.5rem;
}

.brand {
  display: flex;
  gap: 0.75rem;
  align-items: center;
  padding: 0 0.4rem;
}

.brand-mark {
  width: 34px;
  height: 34px;
  border: 1px solid var(--cyan);
  color: var(--cyan);
  display: grid;
  place-items: center;
  font-family: var(--font-mono);
  font-weight: 600;
  box-shadow: var(--glow);
}

.brand-title {
  font-family: var(--font-mono);
  font-weight: 600;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  font-size: 0.85rem;
}

.brand-sub {
  font-family: var(--font-mono);
  font-size: 0.65rem;
  color: var(--mute);
  letter-spacing: 0.1em;
  text-transform: uppercase;
}

nav {
  display: flex;
  flex-direction: column;
  gap: 0.2rem;
  flex: 1;
}

nav a {
  color: var(--mute);
  padding: 0.55rem 0.75rem;
  border-left: 2px solid transparent;
  font-family: var(--font-mono);
  font-size: 0.8rem;
  letter-spacing: 0.04em;
}

nav a:hover,
nav a.active {
  color: var(--cyan);
  border-left-color: var(--cyan);
  background: rgba(0, 229, 255, 0.06);
}

.side-foot {
  border-top: 1px solid var(--line);
  padding: 0.75rem 0.4rem 0;
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
}

.user {
  font-family: var(--font-mono);
  font-size: 0.75rem;
  color: var(--mute);
}

.linkish {
  background: none;
  border: none;
  color: var(--mute);
  text-align: left;
  cursor: pointer;
  padding: 0;
  font-family: var(--font-mono);
  font-size: 0.75rem;
}

.linkish:hover {
  color: var(--cyan);
}

.main {
  padding: 1.75rem 2rem 2.5rem;
  background:
    linear-gradient(var(--grid) 1px, transparent 1px),
    linear-gradient(90deg, var(--grid) 1px, transparent 1px);
  background-size: 48px 48px;
  background-color: var(--void);
}
</style>
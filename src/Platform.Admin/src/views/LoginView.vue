<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth'
import { setLocale } from '@/i18n'
import { animate } from 'animejs'

const { t, locale } = useI18n()
const auth = useAuthStore()
const router = useRouter()
const route = useRoute()

const username = ref('admin')
const password = ref('')
const error = ref('')
const loading = ref(false)

onMounted(() => {
  if (route.query.error === 'forbidden') {
    error.value = t('login.forbidden')
  }
  const card = document.querySelector('.login-card')
  if (card && !window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
    animate(card, {
      opacity: [0, 1],
      translateY: [12, 0],
      duration: 450,
      ease: 'outCubic',
    })
  }
})

function toggleLang() {
  setLocale(locale.value === 'vi' ? 'en' : 'vi')
}

async function onSubmit() {
  error.value = ''
  loading.value = true
  try {
    await auth.login(username.value.trim(), password.value)
    if (!auth.isAdmin) {
      auth.logout()
      error.value = t('login.forbidden')
      return
    }
    const redirect = (route.query.redirect as string) || '/'
    await router.replace(redirect)
  } catch {
    error.value = t('login.failed')
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="login-page">
    <div class="login-card">
      <p class="eyebrow">{{ t('login.eyebrow') }}</p>
      <h1>{{ t('login.title') }}</h1>
      <p class="lede">{{ t('login.lede') }}</p>

      <form @submit.prevent="onSubmit">
        <label>
          {{ t('login.username') }}
          <input v-model="username" autocomplete="username" required />
        </label>
        <label>
          {{ t('login.password') }}
          <input
            v-model="password"
            type="password"
            autocomplete="current-password"
            required
          />
        </label>

        <p v-if="error" class="err" role="alert">{{ error }}</p>

        <button type="submit" class="btn" :disabled="loading">
          {{ loading ? t('login.submitting') : t('login.submit') }}
        </button>
      </form>

      <div class="lang">
        <button type="button" @click="toggleLang">
          {{ locale === 'vi' ? 'EN' : 'VI' }}
        </button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.login-page {
  min-height: 100vh;
  display: grid;
  place-items: center;
  padding: 1.5rem;
  background-color: var(--void);
  background-image:
    linear-gradient(var(--grid) 1px, transparent 1px),
    linear-gradient(90deg, var(--grid) 1px, transparent 1px),
    radial-gradient(ellipse 70% 50% at 50% -10%, rgba(0, 229, 255, 0.12), transparent);
  background-size: 48px 48px, 48px 48px, 100% 100%;
}

.login-card {
  width: min(420px, 100%);
  background: var(--panel);
  border: 1px solid var(--line);
  border-radius: var(--radius);
  padding: 1.75rem 1.5rem 1.5rem;
  box-shadow: var(--glow);
  position: relative;
}

.login-card::before,
.login-card::after {
  content: '';
  position: absolute;
  width: 12px;
  height: 12px;
  border-color: var(--cyan);
  border-style: solid;
}

.login-card::before {
  top: -1px;
  left: -1px;
  border-width: 2px 0 0 2px;
}

.login-card::after {
  bottom: -1px;
  right: -1px;
  border-width: 0 2px 2px 0;
}

.eyebrow {
  margin: 0 0 0.6rem;
  font-family: var(--font-mono);
  font-size: 0.7rem;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  color: var(--cyan);
}

h1 {
  margin: 0;
  font-family: var(--font-display);
  font-size: 1.6rem;
  font-weight: 600;
  letter-spacing: 0.04em;
  text-transform: uppercase;
}

.lede {
  margin: 0.5rem 0 1.4rem;
  color: var(--mute);
  font-size: 0.9rem;
  line-height: 1.45;
}

label {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
  font-family: var(--font-mono);
  font-size: 0.72rem;
  letter-spacing: 0.06em;
  text-transform: uppercase;
  color: var(--mute);
  margin-bottom: 1rem;
}

input {
  border: 1px solid var(--line);
  border-radius: var(--radius);
  padding: 0.7rem 0.75rem;
  font: 500 0.95rem var(--font-mono);
  background: var(--panel-2);
  color: var(--text);
}

input:focus {
  outline: none;
  border-color: var(--cyan);
  box-shadow: 0 0 0 1px var(--cyan);
}

.err {
  color: var(--danger);
  font-family: var(--font-mono);
  font-size: 0.8rem;
  margin: 0 0 0.75rem;
}

.btn {
  width: 100%;
  border: 1px solid var(--cyan);
  border-radius: var(--radius);
  padding: 0.75rem;
  background: rgba(0, 229, 255, 0.12);
  color: var(--cyan);
  font-family: var(--font-mono);
  font-weight: 600;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  cursor: pointer;
}

.btn:hover:not(:disabled) {
  background: rgba(0, 229, 255, 0.22);
  box-shadow: var(--glow);
}

.btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.lang {
  margin-top: 1rem;
  text-align: right;
}

.lang button {
  background: transparent;
  border: 1px solid var(--line);
  color: var(--mute);
  font-family: var(--font-mono);
  font-size: 0.7rem;
  padding: 0.25rem 0.5rem;
  cursor: pointer;
  border-radius: var(--radius);
}

.lang button:hover {
  color: var(--cyan);
  border-color: var(--cyan);
}
</style>
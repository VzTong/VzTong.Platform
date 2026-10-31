<script setup lang="ts">
/**
 * Màn đăng nhập Super Admin (cyber).
 *
 * - BootScreen: chỉ lần đầu trong session (sessionStorage)
 * - Hiện/ẩn mật khẩu (type toggle) — không chặn paste
 * - Login password grant; Google/GitHub = UI disabled (Phase 4)
 * - i18n VI/EN; animejs orb + card enter
 */
import { ref, onMounted, computed } from "vue";
import { useRouter, useRoute } from "vue-router";
import { useI18n } from "vue-i18n";
import { useAuthStore } from "@/stores/auth";
import { setLocale } from "@/i18n";
import { animate } from "animejs";
import BootScreen from "@/components/BootScreen.vue";
import CyberPet from "@/components/CyberPet.vue";

const { t, locale } = useI18n();
const auth = useAuthStore();
const router = useRouter();
const route = useRoute();

const username = ref('admin')
const password = ref('')
const showPassword = ref(false)
const error = ref('')
const loading = ref(false)
const booting = ref(!sessionStorage.getItem('admin_booted'))
const passwordInput = ref<HTMLInputElement | null>(null)
/** Field đang focus — quyết định cover/peek */
const focusField = ref<'user' | 'pass' | null>(null)

onMounted(() => {
  if (route.query.error === "forbidden") {
    error.value = t("login.forbidden");
  }
});

/**
 * CodePen-style: độ dài username → nghiêng đầu (-18° … 18°).
 * Chỉ khi đang gõ username; không theo chuột.
 */
const headTiltDeg = computed(() => {
  if (focusField.value === 'pass' && showPassword.value) return -8 // nghiêng về mắt trái
  if (focusField.value !== 'user') return 0
  // map 0–20 ký tự → -18 … 18
  const t = Math.min(username.value.length, 20) / 20
  return -18 + t * 36
})

function togglePassword() {
  showPassword.value = !showPassword.value
  // Giữ context pet = pass (cover/peek)
  focusField.value = 'pass'
  // Trả focus về ô mật khẩu sau khi bấm nút
  requestAnimationFrame(() => passwordInput.value?.focus())
}

const petMode = computed(() => {
  if (focusField.value === 'pass') {
    return showPassword.value ? ('peek' as const) : ('cover' as const)
  }
  return 'idle' as const
})

function onBootDone() {
  sessionStorage.setItem("admin_booted", "1");
  booting.value = false;
  requestAnimationFrame(() => {
    const card = document.querySelector(".login-card");
    const reduce = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    if (card && !reduce) {
      animate(card, {
        opacity: [0, 1],
        translateY: [16, 0],
        duration: 500,
        ease: "outCubic",
      });
    }
  });
}

function onPassBlur() {
  // Delay nhẹ: nếu focus chuyển sang nút toggle thì không về idle
  window.setTimeout(() => {
    const active = document.activeElement
    if (active?.closest?.('.toggle-pass')) return
    if (active === passwordInput.value) return
    if (focusField.value === 'pass') focusField.value = null
  }, 0)
}

function toggleLang() {
  setLocale(locale.value === "vi" ? "en" : "vi");
}

async function onSubmit() {
  error.value = "";
  loading.value = true;
  try {
    await auth.login(username.value.trim(), password.value);
    if (!auth.isAdmin) {
      auth.logout();
      error.value = t("login.forbidden");
      return;
    }
    const redirect = (route.query.redirect as string) || "/";
    await router.replace(redirect);
  } catch {
    error.value = t("login.failed");
  } finally {
    loading.value = false;
  }
}
</script>

<template>
  <BootScreen v-if="booting" @done="onBootDone" />

  <div v-show="!booting" class="login-page">
    <!-- Lớp trang trí: orbs + scanline (CSS), không chặn click -->
    <div class="fx" aria-hidden="true">
      <span class="orb orb-a" />
      <span class="orb orb-b" />
      <div class="scan" />
    </div>

    <div class="login-card">
      <p class="eyebrow">{{ t("login.eyebrow") }}</p>
      <h1 class="title-row">
        <span>{{ t('login.title') }}</span>
        <CyberPet :mode="petMode" :head-tilt-deg="headTiltDeg" />
      </h1>
      <p class="lede">{{ t("login.lede") }}</p>

      <form @submit.prevent="onSubmit">
        <label>
          {{ t('login.username') }}
          <input
            v-model="username"
            autocomplete="username"
            required
            @focus="focusField = 'user'"
            @blur="focusField = null"
          />
        </label>

        <label>
          {{ t("login.password") }}
          <!-- pass-row: nút mắt SVG -->
          <div class="pass-row">
            <input
              ref="passwordInput"
              v-model="password"
              :type="showPassword ? 'text' : 'password'"
              autocomplete="current-password"
              required
              @focus="focusField = 'pass'"
              @blur="onPassBlur"
            />
            <button
              type="button"
              class="toggle-pass"
              :aria-label="showPassword ? t('login.hidePass') : t('login.showPass')"
              :aria-pressed="showPassword"
              @mousedown.prevent
              @click="togglePassword"
            >
              <!-- Đang ẩn pass → icon mắt mở (bấm để hiện) -->
              <svg
                v-if="!showPassword"
                viewBox="0 0 24 24"
                width="18"
                height="18"
                aria-hidden="true"
              >
                <path
                  fill="currentColor"
                  d="M12 5c-5.5 0-9.9 3.4-11.5 7 1.6 3.6 6 7 11.5 7s9.9-3.4 11.5-7C21.9 8.4 17.5 5 12 5zm0 12a5 5 0 1 1 0-10 5 5 0 0 1 0 10zm0-2a3 3 0 1 0 0-6 3 3 0 0 0 0 6z"
                />
              </svg>
              <!-- Đang hiện pass → icon mắt gạch (bấm để ẩn) -->
              <svg
                v-else
                viewBox="0 0 24 24"
                width="18"
                height="18"
                aria-hidden="true"
              >
                <path
                  fill="currentColor"
                  d="M3.3 2 2 3.3l3.1 3.1C3.2 8 1.8 9.8 1 12c1.6 3.6 6 7 11 7 1.9 0 3.7-.4 5.3-1.1L19.7 21 21 19.7 3.3 2zM12 17c-3.9 0-7.2-2.3-8.8-5 .7-1.5 1.9-2.9 3.4-3.9l1.6 1.6A5 5 0 0 0 12 17zm0-10c3.9 0 7.2 2.3 8.8 5-.5 1.1-1.2 2.1-2.1 3l1.5 1.5c1.2-1.2 2.1-2.7 2.6-4.5-1.6-3.6-6-7-10.8-7-1.1 0-2.2.2-3.2.5L9.6 7.2C10.3 7.1 11.1 7 12 7zm-1.1 3.3 2.9 2.9c.1-.2.2-.5.2-.8a3 3 0 0 0-3-3c-.3 0-.6.1-.8.2z"
                />
              </svg>
            </button>
          </div>
        </label>

        <p v-if="error" class="err" role="alert">{{ error }}</p>

        <button type="submit" class="btn" :disabled="loading">
          {{ loading ? t("login.submitting") : t("login.submit") }}
        </button>
      </form>

      <div class="oauth">
        <p class="oauth-label">{{ t("login.orSocial") }}</p>
        <button type="button" class="btn-ghost" disabled :title="t('login.socialSoon')">
          Google
        </button>
        <button type="button" class="btn-ghost" disabled :title="t('login.socialSoon')">
          GitHub
        </button>
      </div>

      <div class="lang">
        <button type="button" @click="toggleLang">
          {{ locale === "vi" ? "EN" : "VI" }}
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
  position: relative;
  overflow: hidden;
  background-color: var(--void);
  background-image: linear-gradient(var(--grid) 1px, transparent 1px),
    linear-gradient(90deg, var(--grid) 1px, transparent 1px);
  background-size: 48px 48px;
}

.fx {
  pointer-events: none;
  position: absolute;
  inset: 0;
}

.orb {
  position: absolute;
  border-radius: 50%;
  filter: blur(40px);
  opacity: 0.45;
  animation: drift 12s ease-in-out infinite alternate;
}

.orb-a {
  width: 280px;
  height: 280px;
  background: rgba(0, 229, 255, 0.35);
  top: 10%;
  left: 15%;
}

.orb-b {
  width: 220px;
  height: 220px;
  background: rgba(255, 43, 214, 0.25);
  bottom: 15%;
  right: 12%;
  animation-delay: -4s;
}

@keyframes drift {
  from {
    transform: translate(0, 0);
  }
  to {
    transform: translate(30px, -24px);
  }
}

.scan {
  position: absolute;
  left: 0;
  right: 0;
  height: 20%;
  background: linear-gradient(transparent, rgba(0, 229, 255, 0.04), transparent);
  animation: scan 6s linear infinite;
}

@keyframes scan {
  from {
    top: -20%;
  }
  to {
    top: 100%;
  }
}

@media (prefers-reduced-motion: reduce) {
  .orb,
  .scan {
    animation: none;
  }
}

.login-card {
  width: min(420px, 100%);
  background: var(--panel);
  border: 1px solid var(--line);
  border-radius: var(--radius);
  padding: 1.75rem 1.5rem 1.5rem;
  box-shadow: var(--glow);
  position: relative;
  z-index: 1;
}

.login-card::before,
.login-card::after {
  content: "";
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
  font-size: 1.5rem;
  font-weight: 600;
  letter-spacing: 0.06em;
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
  width: 100%;
}

input:focus {
  outline: none;
  border-color: var(--cyan);
  box-shadow: 0 0 0 1px var(--cyan);
}

.pass-row {
  display: flex;
  gap: 0.5rem;
  align-items: stretch;
}

.pass-row input {
  flex: 1;
}

.toggle-pass {
  flex-shrink: 0;
  border: 1px solid var(--line);
  background: var(--panel-2);
  color: var(--cyan);
  font-family: var(--font-mono);
  font-size: 0.65rem;
  letter-spacing: 0.04em;
  text-transform: uppercase;
  padding: 0 0.75rem;
  cursor: pointer;
  border-radius: var(--radius);
}

.toggle-pass:hover {
  border-color: var(--cyan);
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

.oauth {
  margin-top: 1.25rem;
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 0.5rem;
}

.oauth-label {
  grid-column: 1 / -1;
  margin: 0 0 0.25rem;
  font-family: var(--font-mono);
  font-size: 0.65rem;
  color: var(--mute);
  letter-spacing: 0.06em;
  text-transform: uppercase;
}

.btn-ghost {
  border: 1px dashed var(--line);
  background: transparent;
  color: var(--mute);
  font-family: var(--font-mono);
  font-size: 0.75rem;
  padding: 0.55rem;
  border-radius: var(--radius);
  cursor: not-allowed;
  opacity: 0.7;
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

.toggle-pass {
  display: grid;
  place-items: center;
  width: 42px;
  border: 1px solid var(--line);
  background: var(--panel-2);
  color: var(--cyan);
  cursor: pointer;
  border-radius: var(--radius);
}
.toggle-pass:hover {
  border-color: var(--cyan);
  box-shadow: var(--glow);
}
</style>

<script setup lang="ts">
/**
 * Màn Người dùng — list + khóa tạm / vĩnh viễn / mở khóa.
 *
 * API:
 * - GET  /api/identity/users
 * - POST /api/platform/users/{id}/lock
 * - POST /api/platform/users/{id}/unlock
 *
 * UX:
 * - Nút "Mở khóa" chỉ khi đang khóa (tắt hoặc còn lockoutEnd)
 * - Modal giữa màn + LockDatePicker (lịch tháng, không datetime-local)
 */
import { ref, watch, onMounted, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  getUsers,
  lockUser,
  unlockUser,
  type IdentityUserDto,
} from '@/api/users'
import LockDatePicker from '@/components/LockDatePicker.vue'

const { t } = useI18n()

const items = ref<IdentityUserDto[]>([])
const total = ref(0)
const filter = ref('')
const page = ref(1)
const pageSize = 20
const loading = ref(false)
const error = ref('')
const busyId = ref<string | null>(null)

const lockTarget = ref<IdentityUserDto | null>(null)
/** Ngày/giờ local do lịch chọn */
const lockDate = ref<Date | null>(null)

async function load() {
  loading.value = true
  error.value = ''
  try {
    const data = await getUsers({
      filter: filter.value.trim() || undefined,
      maxResultCount: pageSize,
      skipCount: (page.value - 1) * pageSize,
    })
    items.value = data.items ?? []
    total.value = data.totalCount ?? 0
  } catch {
    error.value = t('users.loadError')
    items.value = []
    total.value = 0
  } finally {
    loading.value = false
  }
}

onMounted(load)

let searchTimer: ReturnType<typeof setTimeout> | undefined
watch(filter, () => {
  page.value = 1
  clearTimeout(searchTimer)
  searchTimer = setTimeout(load, 300)
})

function totalPages() {
  return Math.max(1, Math.ceil(total.value / pageSize))
}
function prev() {
  if (page.value > 1) {
    page.value--
    load()
  }
}
function next() {
  if (page.value < totalPages()) {
    page.value++
    load()
  }
}

function isAdminRow(u: IdentityUserDto) {
  return u.userName.toLowerCase() === 'admin'
}

function isTemporarilyLocked(u: IdentityUserDto) {
  return !!(u.lockoutEnd && new Date(u.lockoutEnd) > new Date())
}

/** Chỉ hiện nút Mở khóa khi thực sự đang bị khóa */
function isLocked(u: IdentityUserDto) {
  return !u.isActive || isTemporarilyLocked(u)
}

function statusLabel(u: IdentityUserDto) {
  if (!u.isActive) return t('users.inactive')
  if (isTemporarilyLocked(u)) {
    return t('users.lockedUntil', {
      d: new Date(u.lockoutEnd!).toLocaleString(),
    })
  }
  return t('users.active')
}

function statusClass(u: IdentityUserDto) {
  return isLocked(u) ? 'off' : 'on'
}

const lockSummary = computed(() => {
  if (!lockDate.value) return ''
  return lockDate.value.toLocaleString()
})

function openLockPanel(u: IdentityUserDto) {
  if (isAdminRow(u)) {
    error.value = t('users.cannotDisableAdmin')
    return
  }
  lockTarget.value = u
  const d = new Date()
  d.setDate(d.getDate() + 1)
  d.setHours(23, 59, 0, 0)
  lockDate.value = d
  error.value = ''
}

function closeLockPanel() {
  lockTarget.value = null
  lockDate.value = null
}

async function confirmLockTemporary() {
  if (!lockTarget.value || !lockDate.value) {
    error.value = t('users.lockEndMustBeFuture')
    return
  }
  if (lockDate.value.getTime() <= Date.now()) {
    error.value = t('users.lockEndMustBeFuture')
    return
  }
  busyId.value = lockTarget.value.id
  error.value = ''
  try {
    await lockUser(lockTarget.value.id, {
      mode: 'temporary',
      lockoutEnd: lockDate.value.toISOString(),
    })
    closeLockPanel()
    await load()
  } catch {
    error.value = t('users.toggleError')
  } finally {
    busyId.value = null
  }
}

async function confirmLockPermanent() {
  if (!lockTarget.value) return
  busyId.value = lockTarget.value.id
  error.value = ''
  try {
    await lockUser(lockTarget.value.id, { mode: 'permanent' })
    closeLockPanel()
    await load()
  } catch {
    error.value = t('users.toggleError')
  } finally {
    busyId.value = null
  }
}

async function doUnlock(u: IdentityUserDto) {
  if (isAdminRow(u) || !isLocked(u)) return
  busyId.value = u.id
  error.value = ''
  try {
    await unlockUser(u.id)
    await load()
  } catch {
    error.value = t('users.toggleError')
  } finally {
    busyId.value = null
  }
}
</script>

<template>
  <div class="users-page">
    <header class="page-head">
      <p class="eyebrow">Identity</p>
      <h1>{{ t('users.title') }}</h1>
      <p class="lede">{{ t('users.subtitle') }}</p>
    </header>

    <div class="toolbar">
      <input
        v-model="filter"
        class="search"
        type="search"
        :placeholder="t('users.searchPlaceholder')"
      />
      <span class="meta">{{ t('users.total', { n: total }) }}</span>
    </div>

    <p v-if="error" class="err" role="alert">{{ error }}</p>
    <p v-if="loading" class="muted">{{ t('users.loading') }}</p>

    <div v-else class="table-wrap">
      <table>
        <thead>
          <tr>
            <th>{{ t('users.colUser') }}</th>
            <th>{{ t('users.colEmail') }}</th>
            <th>{{ t('users.colActive') }}</th>
            <th>{{ t('users.colActions') }}</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="u in items" :key="u.id">
            <td class="mono">{{ u.userName }}</td>
            <td>{{ u.email }}</td>
            <td>
              <span class="pill" :class="statusClass(u)">
                {{ statusLabel(u) }}
              </span>
            </td>
            <td class="actions">
              <!-- Chưa khóa → chỉ Khóa -->
              <button
                v-if="!isLocked(u)"
                type="button"
                class="btn-row"
                :disabled="busyId === u.id || isAdminRow(u)"
                @click="openLockPanel(u)"
              >
                {{ t('users.lock') }}
              </button>

              <!-- Đang khóa → chỉ Mở khóa -->
              <button
                v-if="isLocked(u)"
                type="button"
                class="btn-row btn-unlock"
                :disabled="busyId === u.id || isAdminRow(u)"
                @click="doUnlock(u)"
              >
                {{ t('users.unlock') }}
              </button>
            </td>
          </tr>
          <tr v-if="!items.length">
            <td colspan="4" class="muted">{{ t('users.empty') }}</td>
          </tr>
        </tbody>
      </table>
    </div>

    <div class="pager">
      <button type="button" :disabled="page <= 1" @click="prev">
        {{ t('users.prev') }}
      </button>
      <span class="mono">{{ page }} / {{ totalPages() }}</span>
      <button type="button" :disabled="page >= totalPages()" @click="next">
        {{ t('users.next') }}
      </button>
    </div>

    <Teleport to="body">
      <div
        v-if="lockTarget"
        class="modal-root"
        role="presentation"
        @click.self="closeLockPanel"
      >
        <div
          class="modal"
          role="dialog"
          aria-modal="true"
          :aria-label="t('users.lockTitle', { name: lockTarget.userName })"
        >
          <header class="modal-head">
            <p class="eyebrow">Lock control</p>
            <h2>{{ t('users.lockTitle', { name: lockTarget.userName }) }}</h2>
            <p class="hint">{{ t('users.lockHint') }}</p>
          </header>

          <div class="modal-body">
            <p class="field-label">{{ t('users.lockUntil') }}</p>
            <LockDatePicker v-model="lockDate" />
            <p v-if="lockSummary" class="summary mono">
              → {{ lockSummary }}
            </p>
          </div>

          <footer class="modal-foot">
            <button
              type="button"
              class="btn-primary"
              :disabled="!!busyId || !lockDate"
              @click="confirmLockTemporary"
            >
              {{ t('users.lockTemporary') }}
            </button>
            <button
              type="button"
              class="btn-danger"
              :disabled="!!busyId"
              @click="confirmLockPermanent"
            >
              {{ t('users.lockPermanent') }}
            </button>
            <button type="button" class="btn-ghost" @click="closeLockPanel">
              {{ t('users.cancel') }}
            </button>
          </footer>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<style scoped>
.users-page {
  max-width: 960px;
}

.eyebrow {
  margin: 0 0 0.35rem;
  font-family: var(--font-mono);
  font-size: 0.65rem;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  color: var(--cyan);
}

.page-head h1 {
  margin: 0;
  font-family: var(--font-display);
  font-size: 1.45rem;
  letter-spacing: 0.05em;
  text-transform: uppercase;
  color: var(--text);
}

.lede {
  margin: 0.4rem 0 1.35rem;
  color: var(--mute);
  font-size: 0.9rem;
}

.toolbar {
  display: flex;
  flex-wrap: wrap;
  gap: 0.75rem;
  align-items: center;
  margin-bottom: 1rem;
}

.search {
  flex: 1;
  min-width: 180px;
  border: 1px solid var(--line);
  border-radius: var(--radius);
  padding: 0.6rem 0.8rem;
  background: var(--panel);
  color: var(--text);
  font-family: var(--font-mono);
  font-size: 0.85rem;
}

.search:focus {
  outline: none;
  border-color: var(--cyan);
  box-shadow: 0 0 0 1px rgba(0, 229, 255, 0.25);
}

.meta {
  font-family: var(--font-mono);
  font-size: 0.75rem;
  color: var(--mute);
}

.table-wrap {
  border: 1px solid var(--line);
  border-radius: var(--radius);
  overflow: auto;
  background: var(--panel);
}

table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.9rem;
}

th,
td {
  padding: 0.7rem 0.9rem;
  text-align: left;
  border-bottom: 1px solid var(--line);
}

th {
  font-family: var(--font-mono);
  font-size: 0.66rem;
  letter-spacing: 0.07em;
  text-transform: uppercase;
  color: var(--mute);
  background: rgba(0, 0, 0, 0.25);
}

.mono {
  font-family: var(--font-mono);
}

.pill {
  display: inline-block;
  font-family: var(--font-mono);
  font-size: 0.68rem;
  padding: 0.22rem 0.5rem;
  border-radius: 999px;
  border: 1px solid var(--line);
}

.pill.on {
  color: var(--cyan);
  border-color: rgba(0, 229, 255, 0.4);
  background: rgba(0, 229, 255, 0.06);
}

.pill.off {
  color: var(--danger);
  border-color: rgba(255, 77, 109, 0.4);
  background: rgba(255, 77, 109, 0.06);
}

.actions {
  display: flex;
  flex-wrap: wrap;
  gap: 0.4rem;
}

.btn-row {
  font-family: var(--font-mono);
  font-size: 0.68rem;
  letter-spacing: 0.04em;
  text-transform: uppercase;
  border: 1px solid var(--line);
  background: transparent;
  color: var(--cyan);
  padding: 0.3rem 0.55rem;
  border-radius: var(--radius);
  cursor: pointer;
}

.btn-row:hover:not(:disabled) {
  border-color: var(--cyan);
  background: rgba(0, 229, 255, 0.08);
}

.btn-unlock {
  color: var(--text);
  border-color: var(--line);
}

.btn-unlock:hover:not(:disabled) {
  border-color: var(--mute);
  color: var(--cyan);
}

.btn-row:disabled {
  opacity: 0.35;
  cursor: not-allowed;
}

.err {
  color: var(--danger);
  font-family: var(--font-mono);
  font-size: 0.85rem;
}

.muted {
  color: var(--mute);
  font-size: 0.9rem;
}

.pager {
  display: flex;
  gap: 0.75rem;
  align-items: center;
  margin-top: 1rem;
}

.pager button {
  border: 1px solid var(--line);
  background: var(--panel);
  color: var(--text);
  font-family: var(--font-mono);
  font-size: 0.75rem;
  padding: 0.4rem 0.7rem;
  cursor: pointer;
  border-radius: var(--radius);
}

.pager button:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}

.pager button:not(:disabled):hover {
  border-color: var(--cyan);
  color: var(--cyan);
}

/* ---- Modal ---- */
.modal-root {
  position: fixed;
  inset: 0;
  z-index: 90;
  display: grid;
  place-items: center;
  padding: 1rem;
  background: rgba(2, 6, 12, 0.78);
  backdrop-filter: blur(4px);
  animation: fade-in 0.18s ease;
}

.modal {
  width: min(420px, 100%);
  max-height: min(92vh, 720px);
  overflow: auto;
  background: var(--panel);
  border: 1px solid var(--line);
  border-radius: calc(var(--radius) + 2px);
  box-shadow:
    0 0 0 1px rgba(0, 229, 255, 0.08),
    0 24px 64px rgba(0, 0, 0, 0.55);
  display: flex;
  flex-direction: column;
  animation: rise 0.22s ease;
}

.modal-head {
  padding: 1.25rem 1.25rem 0.75rem;
  border-bottom: 1px solid var(--line);
}

.modal-head h2 {
  margin: 0.25rem 0 0.45rem;
  font-family: var(--font-display);
  font-size: 1.1rem;
  font-weight: 600;
  color: var(--text);
}

.hint {
  margin: 0;
  font-size: 0.82rem;
  line-height: 1.45;
  color: var(--mute);
}

.modal-body {
  padding: 1rem 1.25rem;
}

.field-label {
  margin: 0 0 0.5rem;
  font-family: var(--font-mono);
  font-size: 0.66rem;
  letter-spacing: 0.1em;
  text-transform: uppercase;
  color: var(--mute);
}

.summary {
  margin: 0.65rem 0 0;
  font-size: 0.78rem;
  color: var(--cyan);
}

.modal-foot {
  padding: 0.85rem 1.25rem 1.25rem;
  display: flex;
  flex-direction: column;
  gap: 0.45rem;
  border-top: 1px solid var(--line);
}

.btn-primary,
.btn-danger,
.btn-ghost {
  font-family: var(--font-mono);
  font-size: 0.72rem;
  letter-spacing: 0.06em;
  text-transform: uppercase;
  padding: 0.7rem 0.85rem;
  border-radius: var(--radius);
  cursor: pointer;
  border: 1px solid var(--line);
  background: transparent;
  color: var(--text);
}

.btn-primary {
  border-color: var(--cyan);
  color: var(--cyan);
  background: rgba(0, 229, 255, 0.1);
}

.btn-primary:hover:not(:disabled) {
  background: rgba(0, 229, 255, 0.18);
}

.btn-danger {
  border-color: rgba(255, 77, 109, 0.45);
  color: var(--danger);
}

.btn-ghost {
  color: var(--mute);
}

.btn-primary:disabled,
.btn-danger:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}

@keyframes fade-in {
  from {
    opacity: 0;
  }
  to {
    opacity: 1;
  }
}

@keyframes rise {
  from {
    opacity: 0.85;
    transform: translateY(10px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

@media (prefers-reduced-motion: reduce) {
  .modal-root,
  .modal {
    animation: none;
  }
}
</style>
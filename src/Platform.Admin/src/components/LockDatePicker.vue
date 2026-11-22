<script setup lang="ts">
/**
 * Lịch chọn ngày + giờ (không dùng datetime-local).
 * - Lưới tháng, prev/next tháng-năm
 * - Chọn ngày; giờ/phút bằng select
 * - v-model: Date | null (local time, parent convert sang UTC ISO)
 */
import { computed, ref, watch } from 'vue'

const model = defineModel<Date | null>({ default: null })

const view = ref(new Date())
const hour = ref(23)
const minute = ref(59)

watch(
  model,
  (v) => {
    if (!v) return
    view.value = new Date(v.getFullYear(), v.getMonth(), 1)
    hour.value = v.getHours()
    minute.value = v.getMinutes()
  },
  { immediate: true },
)

const year = computed(() => view.value.getFullYear())
const month = computed(() => view.value.getMonth())

const monthLabel = computed(() =>
  view.value.toLocaleString(undefined, { month: 'long', year: 'numeric' }),
)

/** Ô lịch: null = padding tháng trước/sau */
const cells = computed(() => {
  const first = new Date(year.value, month.value, 1)
  const startPad = (first.getDay() + 6) % 7 // tuần bắt đầu Thứ 2
  const daysInMonth = new Date(year.value, month.value + 1, 0).getDate()
  const out: (number | null)[] = []
  for (let i = 0; i < startPad; i++) out.push(null)
  for (let d = 1; d <= daysInMonth; d++) out.push(d)
  while (out.length % 7 !== 0) out.push(null)
  return out
})

const selectedDay = computed(() => {
  if (!model.value) return null
  if (
    model.value.getFullYear() === year.value &&
    model.value.getMonth() === month.value
  ) {
    return model.value.getDate()
  }
  return null
})

function prevMonth() {
  view.value = new Date(year.value, month.value - 1, 1)
}
function nextMonth() {
  view.value = new Date(year.value, month.value + 1, 1)
}

function pickDay(day: number | null) {
  if (day == null) return
  const d = new Date(year.value, month.value, day, hour.value, minute.value, 0, 0)
  model.value = d
}

function onTimeChange() {
  if (!model.value) {
    // chưa chọn ngày → gắn ngày đang xem (hoặc hôm nay trong tháng)
    const day = selectedDay.value ?? Math.min(
      new Date().getDate(),
      new Date(year.value, month.value + 1, 0).getDate(),
    )
    model.value = new Date(year.value, month.value, day, hour.value, minute.value, 0, 0)
    return
  }
  const d = new Date(model.value)
  d.setHours(hour.value, minute.value, 0, 0)
  model.value = d
}

const weekdays = ['Mo', 'Tu', 'We', 'Th', 'Fr', 'Sa', 'Su']
const hours = Array.from({ length: 24 }, (_, i) => i)
const minutes = [0, 15, 30, 45, 59]
</script>

<template>
  <div class="cal">
    <div class="cal-nav">
      <button type="button" class="nav-btn" aria-label="Previous month" @click="prevMonth">
        ‹
      </button>
      <span class="cal-label">{{ monthLabel }}</span>
      <button type="button" class="nav-btn" aria-label="Next month" @click="nextMonth">
        ›
      </button>
    </div>

    <div class="cal-grid head">
      <span v-for="w in weekdays" :key="w" class="wd">{{ w }}</span>
    </div>

    <div class="cal-grid">
      <button
        v-for="(day, i) in cells"
        :key="i"
        type="button"
        class="day"
        :class="{
          empty: day == null,
          selected: day != null && day === selectedDay,
          today:
            day != null &&
            year === new Date().getFullYear() &&
            month === new Date().getMonth() &&
            day === new Date().getDate(),
        }"
        :disabled="day == null"
        @click="pickDay(day)"
      >
        {{ day ?? '' }}
      </button>
    </div>

    <div class="cal-time">
      <label>
        <span>H</span>
        <select v-model.number="hour" @change="onTimeChange">
          <option v-for="h in hours" :key="h" :value="h">
            {{ String(h).padStart(2, '0') }}
          </option>
        </select>
      </label>
      <span class="colon">:</span>
      <label>
        <span>M</span>
        <select v-model.number="minute" @change="onTimeChange">
          <option v-for="m in minutes" :key="m" :value="m">
            {{ String(m).padStart(2, '0') }}
          </option>
        </select>
      </label>
    </div>
  </div>
</template>

<style scoped>
.cal {
  border: 1px solid var(--line);
  border-radius: var(--radius);
  background: var(--panel-2, #0a0e16);
  padding: 0.85rem;
}

.cal-nav {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 0.75rem;
}

.cal-label {
  font-family: var(--font-mono);
  font-size: 0.8rem;
  letter-spacing: 0.06em;
  text-transform: capitalize;
  color: var(--text);
}

.nav-btn {
  width: 32px;
  height: 32px;
  border: 1px solid var(--line);
  border-radius: var(--radius);
  background: transparent;
  color: var(--cyan);
  font-size: 1.1rem;
  cursor: pointer;
  line-height: 1;
}

.nav-btn:hover {
  border-color: var(--cyan);
  background: rgba(0, 229, 255, 0.08);
}

.cal-grid {
  display: grid;
  grid-template-columns: repeat(7, 1fr);
  gap: 4px;
}

.cal-grid.head {
  margin-bottom: 4px;
}

.wd {
  text-align: center;
  font-family: var(--font-mono);
  font-size: 0.62rem;
  color: var(--mute);
  letter-spacing: 0.04em;
  padding: 0.2rem 0;
}

.day {
  aspect-ratio: 1;
  border: 1px solid transparent;
  border-radius: 6px;
  background: transparent;
  color: var(--text);
  font-family: var(--font-mono);
  font-size: 0.78rem;
  cursor: pointer;
  padding: 0;
}

.day:hover:not(:disabled):not(.selected) {
  border-color: var(--line);
  background: rgba(255, 255, 255, 0.04);
}

.day.empty {
  cursor: default;
  visibility: hidden;
}

.day.today:not(.selected) {
  border-color: rgba(0, 229, 255, 0.35);
}

.day.selected {
  background: rgba(0, 229, 255, 0.18);
  border-color: var(--cyan);
  color: var(--cyan);
  font-weight: 600;
}

.cal-time {
  display: flex;
  align-items: flex-end;
  gap: 0.4rem;
  margin-top: 0.85rem;
  padding-top: 0.75rem;
  border-top: 1px solid var(--line);
}

.cal-time label {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
  font-family: var(--font-mono);
  font-size: 0.62rem;
  color: var(--mute);
  letter-spacing: 0.08em;
}

.cal-time select {
  border: 1px solid var(--line);
  border-radius: var(--radius);
  background: var(--panel);
  color: var(--text);
  font-family: var(--font-mono);
  font-size: 0.85rem;
  padding: 0.4rem 0.5rem;
  min-width: 4rem;
}

.cal-time select:focus {
  outline: none;
  border-color: var(--cyan);
}

.colon {
  color: var(--mute);
  padding-bottom: 0.45rem;
  font-family: var(--font-mono);
}
</style>
<script setup lang="ts">
/**
 * Pet cyber cạnh tiêu đề login.
 * - eyesMode: 'cover' | 'peek' | 'track'
 *   cover  = ẩn pass → 2 tay che mắt
 *   peek   = hiện pass → hé 1 mắt, 1 tay vẫn gần
 *   track  = focus username → mắt nhìn theo vị trí con trỏ (px relative)
 * - lookX / lookY: offset đồng tử khi track (truyền từ LoginView)
 */
import { computed } from 'vue'

const props = withDefaults(
  defineProps<{
    eyesMode: 'cover' | 'peek' | 'track'
    lookX?: number
    lookY?: number
  }>(),
  { lookX: 0, lookY: 0 },
)

/** Giới hạn đồng tử trong hốc mắt */
const pupil = computed(() => {
  const x = Math.max(-2.2, Math.min(2.2, props.lookX))
  const y = Math.max(-1.8, Math.min(1.8, props.lookY))
  return { x, y }
})
</script>

<template>
  <span
    class="pet"
    :class="eyesMode"
    title="Platform watcher"
    aria-hidden="true"
  >
    <svg viewBox="0 0 48 40" width="44" height="36">
      <!-- thân -->
      <rect
        x="6"
        y="8"
        width="36"
        height="24"
        rx="5"
        fill="#0c111c"
        stroke="#00e5ff"
        stroke-width="1.4"
      />
      <!-- anten -->
      <line x1="24" y1="8" x2="24" y2="3" stroke="#00e5ff" stroke-width="1.2" />
      <circle cx="24" cy="2.5" r="1.6" fill="#ff2bd6" />

      <!-- hốc mắt -->
      <ellipse cx="17" cy="20" rx="5" ry="5.5" fill="#05070d" stroke="#00e5ff" stroke-width="0.8" />
      <ellipse cx="31" cy="20" rx="5" ry="5.5" fill="#05070d" stroke="#00e5ff" stroke-width="0.8" />

      <!-- mắt sáng + đồng tử (track / peek) -->
      <g class="gaze">
        <ellipse class="glow-l" cx="17" cy="20" rx="3.5" ry="4" fill="#00e5ff" opacity="0.9" />
        <ellipse class="glow-r" cx="31" cy="20" rx="3.5" ry="4" fill="#00e5ff" opacity="0.9" />
        <circle
          class="pupil-l"
          :cx="17 + pupil.x"
          :cy="20 + pupil.y"
          r="1.4"
          fill="#05070d"
        />
        <circle
          class="pupil-r"
          :cx="31 + pupil.x"
          :cy="20 + pupil.y"
          r="1.4"
          fill="#05070d"
        />
      </g>

      <!-- tay trái / phải (che hoặc hé) -->
      <g class="hands">
        <!-- tay trái -->
        <path
          class="hand-l"
          d="M4 28 Q8 18 14 17 Q16 19 14 22 Q10 24 6 30 Z"
          fill="#121826"
          stroke="#00e5ff"
          stroke-width="1"
        />
        <!-- tay phải -->
        <path
          class="hand-r"
          d="M44 28 Q40 18 34 17 Q32 19 34 22 Q38 24 42 30 Z"
          fill="#121826"
          stroke="#00e5ff"
          stroke-width="1"
        />
      </g>
    </svg>
  </span>
</template>

<style scoped>
.pet {
  display: inline-flex;
  vertical-align: middle;
  margin-left: 0.5rem;
  filter: drop-shadow(0 0 6px rgba(0, 229, 255, 0.45));
}

.pet svg {
  display: block;
}

.hand-l,
.hand-r,
.glow-l,
.glow-r,
.pupil-l,
.pupil-r {
  transition:
    transform 0.25s ease,
    opacity 0.25s ease;
  transform-origin: center;
  transform-box: fill-box;
}

/* Ẩn pass: 2 tay che kín mắt */
.cover .hand-l {
  transform: translate(6px, -8px) scale(1.15);
}
.cover .hand-r {
  transform: translate(-6px, -8px) scale(1.15);
}
.cover .glow-l,
.cover .glow-r,
.cover .pupil-l,
.cover .pupil-r {
  opacity: 0.15;
}

/* Hiện pass: hé 1 mắt (mở trái, tay phải vẫn hơi che) */
.peek .hand-l {
  transform: translate(-2px, 4px);
}
.peek .hand-r {
  transform: translate(-4px, -6px) scale(1.05);
}
.peek .glow-r,
.peek .pupil-r {
  opacity: 0.2;
}
.peek .glow-l,
.peek .pupil-l {
  opacity: 1;
}

/* Focus username: mắt mở, theo con trỏ */
.track .hand-l,
.track .hand-r {
  transform: translate(0, 6px);
}
.track .glow-l,
.track .glow-r,
.track .pupil-l,
.track .pupil-r {
  opacity: 1;
}
</style>
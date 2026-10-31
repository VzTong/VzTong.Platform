<script setup lang="ts">
import { computed } from 'vue'

const props = withDefaults(
  defineProps<{
    mode: 'idle' | 'cover' | 'peek'
    headTiltDeg?: number
  }>(),
  { headTiltDeg: 0 },
)

const faceStyle = computed(() => ({
  transform: `rotate(${props.headTiltDeg}deg)`,
}))
</script>

<template>
  <span class="pet" :class="mode" aria-hidden="true">
    <svg viewBox="0 0 64 56" width="52" height="46">
      <g class="face" :style="faceStyle">
        <rect
          x="10"
          y="12"
          width="44"
          height="32"
          rx="10"
          fill="#0c111c"
          stroke="#00e5ff"
          stroke-width="1.5"
        />
        <line x1="32" y1="12" x2="32" y2="5" stroke="#00e5ff" stroke-width="1.2" />
        <circle cx="32" cy="4" r="2" fill="#00e5ff" opacity="0.9" />

        <!-- Mắt trái / phải tách class để peek chỉ mở một bên -->
        <g class="eye eye-l">
          <ellipse cx="24" cy="26" rx="6" ry="6.5" fill="#05070d" />
          <ellipse class="sclera" cx="24" cy="26" rx="4" ry="4.2" fill="#00e5ff" opacity="0.9" />
          <circle class="pupil" cx="25" cy="27" r="1.6" fill="#05070d" />
        </g>
        <g class="eye eye-r">
          <ellipse cx="40" cy="26" rx="6" ry="6.5" fill="#05070d" />
          <ellipse class="sclera" cx="40" cy="26" rx="4" ry="4.2" fill="#00e5ff" opacity="0.9" />
          <circle class="pupil" cx="41" cy="27" r="1.6" fill="#05070d" />
        </g>

        <path
          d="M28 36 Q32 39 36 36"
          fill="none"
          stroke="#00e5ff"
          stroke-width="1.2"
          stroke-linecap="round"
          opacity="0.55"
        />
      </g>

      <g class="hands">
        <ellipse
          class="hand hand-l"
          cx="16"
          cy="44"
          rx="8"
          ry="5.5"
          fill="#121826"
          stroke="#00e5ff"
          stroke-width="1"
        />
        <ellipse
          class="hand hand-r"
          cx="48"
          cy="44"
          rx="8"
          ry="5.5"
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
  margin-left: 0.45rem;
  line-height: 0;
}

.pet svg {
  display: block;
  overflow: visible;
}

.face {
  transform-origin: 32px 20px;
  transition: transform 0.22s ease-out;
}

.hand {
  transition: transform 0.45s cubic-bezier(0.34, 1.2, 0.64, 1);
}

.eye .sclera,
.eye .pupil {
  transition: opacity 0.25s ease;
}

/* —— COVER: hai tay lên ngang mắt, che đều —— */
.cover .hand-l {
  transform: translate(7px, -18px) rotate(-12deg);
}
.cover .hand-r {
  transform: translate(-7px, -18px) rotate(12deg);
}
.cover .eye .sclera,
.cover .eye .pupil {
  opacity: 0.12;
}

/* —— PEEK: tay trái hạ hẳn (mở mắt trái), tay phải vẫn che mắt phải —— */
.peek .hand-l {
  transform: translate(0, 0); /* về vị trí nghỉ */
}
.peek .hand-r {
  transform: translate(-5px, -17px) rotate(10deg);
}
.peek .eye-l .sclera,
.peek .eye-l .pupil {
  opacity: 1;
}
.peek .eye-r .sclera,
.peek .eye-r .pupil {
  opacity: 0.15;
}

/* idle: tay thấp, mắt mở */
.idle .hand-l,
.idle .hand-r {
  transform: translate(0, 0);
}
</style>
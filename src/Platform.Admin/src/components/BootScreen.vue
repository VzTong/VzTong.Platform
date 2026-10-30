<script setup lang="ts">
/**
 * Màn boot lần đầu mở tab Admin.
 * Hiệu ứng gõ chữ mono + thanh progress; emit "done" sau ~1.6s
 * (hoặc ngay nếu prefers-reduced-motion).
 */
import { ref, onMounted } from 'vue'
import { animate } from 'animejs'

const emit = defineEmits<{ done: [] }>()
const line = ref('')
const full = 'PLATFORM_ADMIN // INIT SEQUENCE'

onMounted(() => {
  const reduce = window.matchMedia('(prefers-reduced-motion: reduce)').matches
  if (reduce) {
    emit('done')
    return
  }

  let i = 0
  const timer = window.setInterval(() => {
    line.value = full.slice(0, i)
    i++
    if (i > full.length) {
      window.clearInterval(timer)
      const bar = document.querySelector('.boot-bar-fill')
      if (bar) {
        animate(bar, {
          width: ['0%', '100%'],
          duration: 700,
          ease: 'inOutQuad',
          onComplete: () => emit('done'),
        })
      } else {
        emit('done')
      }
    }
  }, 28)
})
</script>

<template>
  <div class="boot" aria-busy="true" aria-label="Loading">
    <div class="boot-inner">
      <p class="boot-line">{{ line }}<span class="cursor">▌</span></p>
      <div class="boot-bar">
        <div class="boot-bar-fill" />
      </div>
    </div>
  </div>
</template>

<style scoped>
.boot {
  position: fixed;
  inset: 0;
  z-index: 100;
  background: var(--void);
  display: grid;
  place-items: center;
}

.boot-inner {
  width: min(360px, 90vw);
}

.boot-line {
  font-family: var(--font-mono);
  font-size: 0.8rem;
  letter-spacing: 0.08em;
  color: var(--cyan);
  min-height: 1.5em;
  margin: 0 0 1rem;
}

.cursor {
  animation: blink 0.8s step-end infinite;
}

@keyframes blink {
  50% {
    opacity: 0;
  }
}

.boot-bar {
  height: 3px;
  background: rgba(0, 229, 255, 0.15);
  border: 1px solid var(--line);
}

.boot-bar-fill {
  height: 100%;
  width: 0;
  background: var(--cyan);
  box-shadow: var(--glow);
}
</style>
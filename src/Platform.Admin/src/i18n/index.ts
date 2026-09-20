import { createI18n } from 'vue-i18n'
import en from '@/locales/en.json'
import vi from '@/locales/vi.json'

const saved = localStorage.getItem('locale')
const locale = saved === 'en' || saved === 'vi' ? saved : 'vi'

export const i18n = createI18n({
  legacy: false,
  locale,
  fallbackLocale: 'en',
  messages: { en, vi },
})

export function setLocale(next: 'en' | 'vi') {
  i18n.global.locale.value = next
  localStorage.setItem('locale', next)
  document.documentElement.lang = next
}
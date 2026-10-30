/**
 * Đa ngôn ngữ (vue-i18n).
 *
 * Cách dùng trong component:
 *   const { t, locale } = useI18n()
 *   t('login.title')  → chuỗi theo locale hiện tại
 *
 * Đổi ngôn ngữ: setLocale('en' | 'vi') — lưu localStorage để F5 vẫn giữ.
 * Thêm ngôn ngữ mới: tạo locales/xx.json + import vào messages.
 */
import { createI18n } from 'vue-i18n'
import en from '@/locales/en.json'
import vi from '@/locales/vi.json'

const saved = localStorage.getItem('locale')
// Chỉ chấp nhận en|vi; mặc định tiếng Việt cho team nội địa
const locale = saved === 'en' || saved === 'vi' ? saved : 'vi'

export const i18n = createI18n({
  legacy: false, // Composition API (useI18n), không dùng $t kiểu Options API cũ
  locale,
  fallbackLocale: 'en', // thiếu key VI → hiện EN, tránh ô trống
  messages: { en, vi },
})

/** Đổi locale runtime + đồng bộ <html lang> cho a11y / SEO local. */
export function setLocale(next: 'en' | 'vi') {
  i18n.global.locale.value = next
  localStorage.setItem('locale', next)
  document.documentElement.lang = next
}
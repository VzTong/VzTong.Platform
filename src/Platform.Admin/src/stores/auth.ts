/**
 * Auth store (Pinia) — cầu nối Admin SPA ↔ OpenIddict /connect/token.
 *
 * Luồng login:
 * 1) POST form-urlencoded grant_type=password
 * 2) Nhận access_token (+ refresh_token nếu có offline_access)
 * 3) Lưu localStorage để F5 không mất session (MVP; production có thể cân httpOnly cookie)
 *
 * Phân quyền portal (MVP):
 * - Decode JWT payload (base64) lấy claim "role"
 * - isAdmin khi role === "admin" (string hoặc mảng)
 * - Không verify chữ ký RSA phía browser — chỉ UX gate; API vẫn enforce permission
 */
import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import axios from 'axios'

// post `${IDENTITY}/connect/token` → "/connect/token" khi IDENTITY = ""
const IDENTITY = (import.meta.env.VITE_IDENTITY_URL as string) || ''
const CLIENT_ID = import.meta.env.VITE_CLIENT_ID as string
const SCOPE = import.meta.env.VITE_SCOPE as string

/**
 * Parse phần payload JWT (segment thứ 2).
 * Trả null nếu token hỏng — caller coi như chưa login.
 */
function parseJwt(token: string): Record<string, unknown> | null {
  try {
    const part = token.split('.')[1]
    // JWT dùng base64url; atob cần chuyển -/_ về +/
    return JSON.parse(atob(part.replace(/-/g, '+').replace(/_/g, '/')))
  } catch {
    return null
  }
}

export const useAuthStore = defineStore('auth', () => {
  // Khôi phục session từ lần login trước (cùng tab/browser)
  const accessToken = ref(localStorage.getItem('access_token') || '')
  const refreshToken = ref(localStorage.getItem('refresh_token') || '')

  const claims = computed(() =>
    accessToken.value ? parseJwt(accessToken.value) : null,
  )

  // preferred_username là claim OIDC chuẩn; unique_name là fallback ABP/Identity
  const userName = computed(
    () =>
      (claims.value?.preferred_username as string) ||
      (claims.value?.unique_name as string) ||
      '',
  )

  /**
   * Gate vào Admin shell.
   * OpenIddict có thể trả role dạng string ("admin") hoặc array khi multi-role.
   */
  const isAdmin = computed(() => {
    const role = claims.value?.role
    if (Array.isArray(role)) {
      return role.some((r) => String(r).toLowerCase() === 'admin')
    }
    if (typeof role === 'string') {
      return role.toLowerCase() === 'admin'
    }
    return false
  })

  const isAuthenticated = computed(() => Boolean(accessToken.value))

  /**
   * Password grant — client public (DocForge_Web / Platform_Admin).
   * Ném lỗi axios nếu 400 invalid_grant; LoginView bắt và hiện message i18n.
   */
  async function login(username: string, password: string) {
    const body = new URLSearchParams({
      grant_type: 'password',
      client_id: CLIENT_ID,
      username,
      password,
      scope: SCOPE,
    })

    const { data } = await axios.post(`${IDENTITY}/connect/token`, body, {
      headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
    })

    accessToken.value = data.access_token
    refreshToken.value = data.refresh_token || ''
    localStorage.setItem('access_token', data.access_token)
    if (data.refresh_token) {
      localStorage.setItem('refresh_token', data.refresh_token)
    }
  }

  /** Xóa token local — không gọi revocation server (có thể bổ sung sau). */
  function logout() {
    accessToken.value = ''
    refreshToken.value = ''
    localStorage.removeItem('access_token')
    localStorage.removeItem('refresh_token')
  }

  return {
    accessToken,
    refreshToken,
    claims,
    userName,
    isAdmin,
    isAuthenticated,
    login,
    logout,
  }
})
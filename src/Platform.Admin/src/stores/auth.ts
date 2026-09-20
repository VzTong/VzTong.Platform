import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import axios from 'axios'

const IDENTITY = import.meta.env.VITE_IDENTITY_URL as string
const CLIENT_ID = import.meta.env.VITE_CLIENT_ID as string
const SCOPE = import.meta.env.VITE_SCOPE as string

function parseJwt(token: string): Record<string, unknown> | null {
  try {
    const part = token.split('.')[1]
    return JSON.parse(atob(part.replace(/-/g, '+').replace(/_/g, '/')))
  } catch {
    return null
  }
}

export const useAuthStore = defineStore('auth', () => {
  const accessToken = ref(localStorage.getItem('access_token') || '')
  const refreshToken = ref(localStorage.getItem('refresh_token') || '')

  const claims = computed(() =>
    accessToken.value ? parseJwt(accessToken.value) : null,
  )

  const userName = computed(
    () =>
      (claims.value?.preferred_username as string) ||
      (claims.value?.unique_name as string) ||
      '',
  )

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
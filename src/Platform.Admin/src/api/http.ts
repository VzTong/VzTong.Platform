import axios from 'axios'

/** Client HTTP tới Identity; gắn Bearer từ localStorage. */
const http = axios.create({
  baseURL: import.meta.env.VITE_IDENTITY_URL,
  headers: { 'Content-Type': 'application/json' },
})

http.interceptors.request.use((config) => {
  const token = localStorage.getItem('access_token')
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

export default http
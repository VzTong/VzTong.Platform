/**
 * HTTP client dùng chung cho mọi gọi Identity API từ Admin SPA.
 *
 * Vì sao axios instance riêng?
 * - Gắn sẵn baseURL (tránh hard-code host trong từng page)
 * - Interceptor request: tự gắn Bearer sau khi user login
 * - Sau này có thể thêm interceptor 401 → refresh token / logout
 */
import axios from 'axios'

const http = axios.create({
  // Lấy từ .env.development — đổi môi trường không sửa code
  baseURL: import.meta.env.VITE_IDENTITY_URL || '',
  headers: { 'Content-Type': 'application/json' },
})

http.interceptors.request.use((config) => {
  // Token do auth store ghi vào localStorage sau /connect/token
  const token = localStorage.getItem('access_token')
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

export default http
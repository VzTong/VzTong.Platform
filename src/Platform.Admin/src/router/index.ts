/**
 * Định tuyến Admin Portal.
 *
 * Cấu trúc:
 * - /login: public, không cần token
 * - / : AdminLayout + children (dashboard, users, roles) — bắt buộc auth + admin
 *
 * beforeEach chạy trước mọi navigation: quyết định cho vào hay đá về login.
 */
import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/login',
      name: 'login',
      component: () => import('@/views/LoginView.vue'),
      meta: { public: true },
    },
    {
      path: '/',
      component: () => import('@/layouts/AdminLayout.vue'),
      meta: { requiresAuth: true },
      children: [
        {
          path: '',
          name: 'dashboard',
          component: () => import('@/views/DashboardView.vue'),
        },
        {
          path: 'users',
          name: 'users',
          component: () => import('@/views/users/UsersView.vue'),
        },
        {
          path: 'roles',
          name: 'roles',
          component: () => import('@/views/roles/RolesView.vue'),
        },
      ],
    },
  ],
})

router.beforeEach((to) => {
  const auth = useAuthStore()

  // Trang public: không kiểm tra token
  if (to.meta.public) return true

  // Chưa có access_token → login, giữ redirect để vào lại đúng trang
  if (!auth.isAuthenticated) {
    return { name: 'login', query: { redirect: to.fullPath } }
  }

  // Có token nhưng không phải admin (vd. user01) → không cho vào portal
  if (!auth.isAdmin) {
    auth.logout()
    return { name: 'login', query: { error: 'forbidden' } }
  }

  return true
})

export default router
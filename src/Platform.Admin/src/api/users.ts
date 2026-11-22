/**
 * API Identity Users cho Admin SPA.
 *
 * Endpoint ABP:
 * - GET    /api/identity/users
 * - GET    /api/identity/users/{id}
 * - PUT    /api/identity/users/{id}
 * - GET    /api/identity/users/{id}/roles
 *
 * Khóa/mở (Community): không có /lock Pro → đổi isActive bằng PUT.
 * PUT cần concurrencyStamp mới nhất (lấy từ GET trước khi ghi).
 */
import http from './http'

/** DTO user trả về từ Identity (các field Admin đang dùng). */
export interface IdentityUserDto {
  id: string
  userName: string
  email: string
  name?: string | null
  surname?: string | null
  phoneNumber?: string | null
  isActive: boolean
  lockoutEnabled: boolean
  /** ABP thường trả camelCase lockoutEnd */
  lockoutEnd?: string | null
  concurrencyStamp: string
}
/** Kết quả phân trang chuẩn ABP. */
export interface PagedResultDto<T> {
  items: T[]
  totalCount: number
}

/** Query list users. */
export interface GetUsersInput {
  filter?: string
  maxResultCount?: number
  skipCount?: number
  sorting?: string
}

/**
 * Danh sách user (search + phân trang server-side).
 * Query name PascalCase theo convention ABP.
 */
export async function getUsers(params: GetUsersInput = {}) {
  const { data } = await http.get<PagedResultDto<IdentityUserDto>>(
    '/api/identity/users',
    {
      params: {
        Filter: params.filter || undefined,
        MaxResultCount: params.maxResultCount ?? 20,
        SkipCount: params.skipCount ?? 0,
        Sorting: params.sorting || 'userName',
      },
    },
  )
  return data
}

/** Chi tiết 1 user — dùng trước khi PUT để lấy concurrencyStamp. */
export async function getUser(id: string) {
  const { data } = await http.get<IdentityUserDto>(`/api/identity/users/${id}`)
  return data
}

/**
 * Cập nhật user.
 * Body gần với IdentityUserUpdateDto; password null = không đổi MK.
 */
export async function updateUser(
  id: string,
  body: {
    userName: string
    email: string
    name?: string | null
    surname?: string | null
    phoneNumber?: string | null
    isActive: boolean
    lockoutEnabled: boolean
    roleNames?: string[]
    concurrencyStamp: string
    password?: string | null
  },
) {
  const { data } = await http.put<IdentityUserDto>(
    `/api/identity/users/${id}`,
    body,
  )
  return data
}

/**
 * Đọc tên role hiện tại của user.
 * ABP có thể trả { items: [{ name }] } hoặc { roleNames: string[] } tùy version —
 * parse linh hoạt để tránh vỡ khi nâng package.
 */
export async function getUserRoleNames(id: string): Promise<string[]> {
  try {
    const { data } = await http.get<unknown>(`/api/identity/users/${id}/roles`)

    if (Array.isArray(data)) {
      return data.map((x) =>
        typeof x === 'string' ? x : String((x as { name?: string }).name ?? ''),
      ).filter(Boolean)
    }

    if (data && typeof data === 'object') {
      const obj = data as {
        items?: { name?: string }[]
        roleNames?: string[]
      }
      if (Array.isArray(obj.roleNames)) return obj.roleNames
      if (Array.isArray(obj.items)) {
        return obj.items.map((r) => r.name ?? '').filter(Boolean)
      }
    }
  } catch {
    // Không có role / lỗi quyền → PUT với [] (có thể gỡ hết role — chỉ dùng khi API bắt buộc roleNames)
  }
  return []
}

/**
 * Chỉ đổi isActive — body tối thiểu như curl đã test OK.
 * Không gửi password (null dễ 400).
 */
export async function setUserActive(id: string, isActive: boolean) {
  const u = await getUser(id)
  let roleNames = await getUserRoleNames(id)

  // Fallback: user thường seed role "User"
  if (!roleNames.length && u.userName.toLowerCase() !== 'admin') {
    roleNames = ['User']
  }
  if (!roleNames.length && u.userName.toLowerCase() === 'admin') {
    roleNames = ['admin']
  }

  const { data } = await http.put<IdentityUserDto>(`/api/identity/users/${id}`, {
    userName: u.userName,
    email: u.email,
    name: u.name ?? null,
    surname: u.surname ?? null,
    phoneNumber: u.phoneNumber ?? null,
    isActive,
    lockoutEnabled: u.lockoutEnabled,
    roleNames,
    concurrencyStamp: u.concurrencyStamp,
    // không có password
  })
  return data
}

/** Body khóa user — khớp PlatformUserLockController. */
export type LockUserMode = 'temporary' | 'permanent'

export interface LockUserInput {
  mode: LockUserMode
  /** ISO UTC, bắt buộc khi mode=temporary */
  lockoutEnd?: string | null
}

/**
 * Khóa tạm (đến lockoutEnd) hoặc vĩnh viễn (isActive=false).
 * POST /api/platform/users/{id}/lock → 204
 */
export async function lockUser(id: string, input: LockUserInput) {
  await http.post(`/api/platform/users/${id}/lock`, {
    mode: input.mode,
    lockoutEnd: input.lockoutEnd ?? null,
  })
}

/** Mở khóa: isActive=true + clear LockoutEnd. POST .../unlock → 204 */
export async function unlockUser(id: string) {
  await http.post(`/api/platform/users/${id}/unlock`)
}
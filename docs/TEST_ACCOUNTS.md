# Tài khoản test & cách tạo user (Identity)

> File **riêng** — không commit mật khẩu production.  
> Cập nhật khi đổi pass / thêm user test.  
> Môi trường: **local + Neon dev**, client OpenIddict `DocForge_Web` (Admin SPA tạm dùng client này).

---

## 1. Tài khoản đang dùng

| Username | Password (dev) | Role | Ghi chú |
|----------|----------------|------|---------|
| `admin` | `1q2w3E*` | `admin` | Seed ABP mặc định; đã test login Admin SPA + `/connect/token` |
| `user01` | `1q2w3E*` (nếu chưa đổi) | `User` | Tạo lúc Phase 1 (register API); dùng test user thường / isActive |

> Nếu login fail: thử lại `2w3e4R*` (từng đổi pass admin một lần rồi đổi về `1q2w3E*`).  
> **Không** lưu pass production vào git.

---

## 2. `admin` đến từ đâu?

1. Chạy **DbMigrator** lần đầu trên Neon → ABP seed user `admin` + role admin + permissions.
2. Commit liên quan seed / migrator (tham chiếu):
   - `feat: seed roles, permissions and DocForge_Web client` (`780b5c7`)
   - `fix: load OpenIddict config from appsettings/env in DbMigrator` (`e05252f`)
3. Đổi mật khẩu qua API (Phase 1.8) → revoke refresh token:
   - `feat: register assigns User role; revoke tokens on password change` (`0204276`)

### Kiểm tra login admin (curl)

Identity Host phải đang chạy (`https://localhost:44322`).

```powershell
curl.exe -k -s -w "`nHTTP:%{http_code}`n" -X POST "https://localhost:44322/connect/token" `
  -H "Content-Type: application/x-www-form-urlencoded" `
  -d "grant_type=password&client_id=DocForge_Web&username=admin&password=1q2w3E*&scope=openid profile email roles Identity offline_access"
```

Kỳ vọng: **HTTP 200** + JSON có `access_token`, claim/role admin.

---

## 3. Tạo user mới (register)

### 3.1. API (khuyến nghị ghi lại)

Tùy endpoint thực tế trên Host (Swagger `https://localhost:44322/swagger`):

- Thường ABP: đăng ký qua module Account, hoặc custom register đã làm Phase 1.5.
- Sau register: handler gán role **`User`**  
  → commit `0204276` (`AssignUserRoleOnCreateHandler`).

### 3.2. Kiểm tra DB (Neon SQL)

```sql
SELECT "UserName", "Email", "IsActive" FROM "AbpUsers" ORDER BY "CreationTime";
SELECT r."Name", u."UserName"
FROM "AbpUserRoles" ur
JOIN "AbpRoles" r ON r."Id" = ur."RoleId"
JOIN "AbpUsers" u ON u."Id" = ur."UserId";
```

### 3.3. User test đã từng tạo

| User | Mục đích |
|------|----------|
| `user01` / `user01@test.com` | Test list users, khóa `isActive: false` → login `account_inactive` |

---

## 4. Khóa / mở user (ABP Community — không có `/lock` Pro)

Admin (Bearer token admin):

```http
GET  /api/identity/users/{id}
PUT  /api/identity/users/{id}
```

Body PUT cần đủ field + `concurrencyStamp` mới nhất; set `"isActive": false` hoặc `true`.

Đã verify Phase 1.11: inactive → `/connect/token` trả `account_inactive`.

---

## 5. Admin SPA login

| Mục | Giá trị |
|-----|---------|
| URL | `http://localhost:5174` |
| User | `admin` |
| Pass | `1q2w3E*` |
| Client | `DocForge_Web` (`.env.development`) cho đến khi seed `Platform_Admin` |
| Proxy | Vite proxy `/connect`, `/api` → `https://localhost:44322` (`secure: false`) |

User thường (`user01`) **không** vào portal (guard `isAdmin`).

---

## 6. Khi quên mật khẩu

1. Curl thử `1q2w3E*` và `2w3e4R*`.
2. Không đọc được hash từ DB → reset bằng user admin khác hoặc tạo user mới + gán role admin (SQL `AbpUserRoles`).
3. Cập nhật bảng mục 1 trong file này sau khi đổi pass dev.

---

## 7. Commit tham chiếu (accounts / identity core)

| SHA ngắn | Message |
|----------|---------|
| `780b5c7` | seed roles, permissions, DocForge_Web |
| `e05252f` | OpenIddict config từ appsettings/env DbMigrator |
| `0204276` | register → role User; change-password revoke tokens |
| `80577ca` / `32f88ec` | Admin SPA shell (login dùng account trên) |

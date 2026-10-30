# Nhật ký hoàn thành phase + checklist test

> Đọc file này khi quay lại project sau thời gian dài.
> Mỗi phase: **đã làm gì** → **commit tham chiếu** → **cách test lại** → **còn thiếu**.
> Tài khoản: xem `docs/TEST_ACCOUNTS.md`.
> Thiết kế tổng: `docs/DOCS.md`, kế hoạch: `docs/PLAN.md`.
> Matrix sau deploy: `docs/PLATFORM_MATRIX.md`.

---

## Bản đồ commit (dev-main) — mới → cũ

| SHA | Title (EN) | Phase |
|-----|------------|-------|
| `f3b8224` | fix: remove focus event handler from password input in LoginView | 2 (polish) |
| `32f88ec` | feat(admin): Vue Admin SPA shell with cyber UI, i18n, and matrix design note | 2 shell |
| `80577ca` | feat: scaffold Platform.Admin Vue SPA with i18n and cyber UI | 2 shell |
| `0204276` | feat: register assigns User role; revoke tokens on password change | 1 |
| `e05252f` | fix: load OpenIddict config from appsettings/env in DbMigrator | 1 |
| `780b5c7` | feat: seed roles, permissions and DocForge_Web client | 1 |
| `561a1f0` … `f63e448` | CI, Docker, Neon, init skeleton | 0 |

---

## Phase 0 — Repo, Neon, CI, Docker

### Đã xong

- Repo GitHub, solution ABP Identity, `.gitignore` / `.env` / `.env.example`
- PostgreSQL **Neon**, load `.env`, DbMigrator
- Dockerfile multi-stage Identity Host
- GitHub Actions CI + push image GHCR
- Branch `dev-main`; docs chuyển vào `docs/`

### Commit tham chiếu

`f63e448`, `bef50af`, `7879e0c`, `63cfd09`, `285f9a2`, `561a1f0`

### Test lại nhanh

```powershell
cd E:\code\Project\PjCaNhan\VzTong.Platform
dotnet build
dotnet run --project src/Platform.Identity.DbMigrator
# CI: mở GitHub Actions — workflow xanh trên push/PR
```

### Còn thiếu (Phase 0 DoD)

- [ ] CD deploy Identity lên Render (hoặc tương đương free) — xem PLAN §2.5
- [ ] Confirm image GHCR pull được từ môi trường deploy

---

## Phase 1 — Identity core (API)

### Đã xong (đã test bằng curl / SQL)

| # | Việc | Kết quả test |
|---|------|----------------|
| 1.1–1.4 | Seed role User/Admin, permission Platform/DocForge, client `DocForge_Web` | DB có role + OpenIddict apps |
| 1.5 | Register + gán role User | `AssignUserRoleOnCreateHandler` |
| 1.6 | Login password + refresh | `/connect/token` 200 |
| 1.7 | Profile GET | `/api/account/my-profile` 200 |
| 1.8 | Change-password + revoke tokens | 204; refresh cũ 400 `invalid_grant` |
| 1.9 | Discovery + JWKS | `/.well-known/openid-configuration` |
| 1.10 | CORS (DocForge origin + sau Admin 5174) | OPTIONS preflight OK |
| 1.11 | Lock/unlock CE | `PUT` user `isActive` false/true |
| 1.12 | Smoke vòng đời | Đã làm rải rác bằng curl |

### Commit tham chiếu

`780b5c7`, `e05252f`, `0204276`

### Test lại vòng đời (copy khi cần)

1. **Host chạy:** `dotnet run --project src/Platform.Identity.HttpApi.Host`
2. **Token admin** — xem `TEST_ACCOUNTS.md` §2
3. **Change-password** → refresh cũ phải fail
4. **isActive false** trên user01 → login fail `account_inactive`
5. **isActive true** → login lại OK

### Còn thiếu Phase 1

- [ ] (Optional) Seed client riêng `Platform_Admin` thay vì dùng tạm `DocForge_Web` cho Admin SPA
- [ ] Ghi checkbox DoD trong `docs/PLAN.md` nếu muốn đồng bộ tài liệu

---

## Phase 2 — Super Admin Portal (đang làm)

### Đã xong (shell)

- Chọn: Vue 3 + Vite SPA (`src/Platform.Admin`), port **5174**
- Cyber UI, i18n en/vi, BootScreen, CyberPet, hiện/ẩn pass
- Vite **proxy** `/connect` + `/api` → Identity (tránh `ERR_CERT_AUTHORITY_INVALID`)
- Login admin → Dashboard hiện **số user** (đã thấy 3)
- Placeholder: Users, Roles
- `docs/PLATFORM_MATRIX.md` (thiết kế sau — không code matrix trong MVP Users)

### Commit tham chiếu

`80577ca`, `32f88ec`, `f3b8224`

### Test lại shell Admin

```powershell
# Terminal 1
dotnet run --project src/Platform.Identity.HttpApi.Host

# Terminal 2
cd src\Platform.Admin
npm run dev
```

1. Mở `http://localhost:5174` → boot (lần đầu session) → login `admin` / `1q2w3E*`
2. Vào **Tổng quan** — card Users có số > 0
3. Đổi VI/EN
4. User không phải admin không vào được (logout + forbidden)
5. F12 Network: token request qua `localhost:5174/connect/token` (proxy), không hit trực tiếp 44322 nếu đã cấu hình proxy

### Việc tiếp theo (theo PLAN 2.4 → 2.11)

| Ưu tiên | Việc | Output khi xong |
|---------|------|------------------|
| **2.4** | Users: list + search | Bảng gọi `GET /api/identity/users` |
| **2.5** | Users: gán/gỡ role | API roles của user |
| **2.6** | Users: khóa/mở (`isActive`) | PUT user như Phase 1.11, từ UI |
| 2.7 | (Optional) admin reset password + revoke | |
| 2.8–2.9 | Roles list + xem/sửa permission | |
| 2.10 | Dashboard thêm số liệu (user mới 7 ngày…) | |
| 2.11 | Super Admin seed — đã có `admin` | Document trong TEST_ACCOUNTS |

**Bước code ngay:** implement **UsersView** (list + filter + toggle isActive).

### Test khi xong 2.4–2.6 (điền khi làm)

- [ ] List hiển thị `admin`, `user01`, …
- [ ] Search theo username/email
- [ ] Tắt isActive trên user01 → curl login user01 fail
- [ ] Bật lại → login OK
- [ ] (2.5) Gán/gỡ role phản ánh trên JWT hoặc API get roles

---

## Phase 3+ (chưa bắt đầu)

- Phase 3: tích hợp DocForge FE
- Phase 4: PKCE, clients UI, matrix health (`PLATFORM_MATRIX.md`), Google login…

---

## Quy ước cập nhật file này

Sau mỗi milestone (ví dụ xong Users table):

1. Đánh dấu checkbox test trong phase tương ứng
2. Thêm 1 dòng commit SHA + message vào bảng commit
3. Không xóa lịch sử test cũ — chỉ thêm “Còn thiếu” → chuyển sang “Đã xong”

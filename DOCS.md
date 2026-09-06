# Platform — Tài liệu thiết kế tổng thể

> **Platform** là hệ thống nền tảng dùng chung cho nhiều sản phẩm (DocForge, BookingHotels, …).  
> Gồm hai phần gắn chặt:
>
> 1. **Identity** — OIDC / user / role / permission / JWT  
> 2. **Admin Portal** — UI dành cho **Super Admin** quản trị toàn hệ sinh thái  
>
> Không gắn tên một sản phẩm con. DocForge và Hotel chỉ là **client** của Platform.

---

## Mục lục

1. [Tầm nhìn & phạm vi](#1-tầm-nhìn--phạm-vi)
2. [Repo & cấu trúc sản phẩm](#2-repo--cấu-trúc-sản-phẩm)
3. [Kiến trúc tổng thể](#3-kiến-trúc-tổng-thể)
4. [Hai thành phần cốt lõi](#4-hai-thành-phần-cốt-lõi)
5. [Mô hình phân quyền](#5-mô-hình-phân-quyền)
6. [Super Admin UI — chức năng](#6-super-admin-ui--chức-năng)
7. [Tích hợp sản phẩm con](#7-tích-hợp-sản-phẩm-con)
8. [Stack công nghệ](#8-stack-công-nghệ)
9. [API chính](#9-api-chính)
10. [Bảo mật](#10-bảo-mật)
11. [Cấu hình](#11-cấu-hình)
12. [Ranh giới rõ ràng](#12-ranh-giới-rõ-ràng)

---

## 1. Tầm nhìn & phạm vi

### 1.1. Tầm nhìn

- **Một tài khoản** đăng nhập nhiều sản phẩm (DocForge, BookingHotels, …) — giống Azure AD trong một tổ chức.
- **Một Super Admin** (hoặc nhóm Admin platform) nhìn và điều khiển:
  - Người dùng toàn hệ thống  
  - Role / permission  
  - Client OIDC (ứng dụng được phép login)  
  - (Sau) cấu hình chung, audit log, khóa user  
- Sản phẩm con **không** tự dựng hệ user/login riêng cho SSO.

### 1.2. Trong phạm vi Platform

| Có | Không (để sản phẩm con) |
|----|-------------------------|
| Đăng ký / đăng nhập / đổi MK / logout | Convert PDF, transcribe (DocForge) |
| User, role, permission toàn cục | Đặt phòng, phòng, hóa đơn (Hotel) |
| Super Admin UI | Admin nghiệp vụ khách sạn / tool convert |
| OIDC clients, JWT, JWKS | Lưu file media, job convert |
| Khóa user, gán role | UI người dùng cuối của từng app |

### 1.3. Ai dùng gì

| Đối tượng | Dùng |
|-----------|------|
| End-user | Login qua từng app (DocForge FE, Hotel Web) → token từ Platform Identity |
| Super Admin | **Platform Admin Portal** (web riêng) |
| Dev / ops | Deploy Identity + Admin; cấu hình client khi thêm app mới |

---

## 2. Repo & cấu trúc sản phẩm

### 2.1. Khuyến nghị: **một repo Platform** (hoặc solution chung)

```
Platform/                          # Git repo riêng (tên trung lập)
├── DOCS.md                        # File này
├── PLAN.md                        # Kế hoạch triển khai
├── README.md
│
├── src/
│   ├── Platform.Identity.*/       # ABP + OpenIddict (API authority)
│   └── Platform.Admin/            # Admin Portal UI (SPA hoặc ABP MVC/Blazor)
│
└── ...
```

**Vì sao gộp Identity + Admin trong một repo Platform?**

- Cùng domain “nền tảng”, cùng team vận hành.  
- Admin gọi thẳng API Identity / Application layer.  
- Vẫn **deploy tách process** nếu cần (Identity API port A, Admin static/host port B).

**Không** nhét vào repo DocForge hay BookingHotels.

### 2.2. Tên

- Repo: `Platform` / `VzTong.Platform` / `YourOrg.Platform`  
- Tránh `DocForge.Platform` nếu dùng đa sản phẩm.

### 2.3. Quan hệ với repo khác

| Repo | Nội dung |
|------|----------|
| **Platform** | Identity + Super Admin UI |
| **DocForge** | FE + PyService (client) |
| **BookingHotels** (sau) | Web/API khách sạn (client) |

---

## 3. Kiến trúc tổng thể

```
                    ┌────────────────────────────────────────────┐
                    │              PLATFORM                      │
                    │                                            │
                    │  ┌──────────────┐    ┌─────────────────┐   │
                    │  │ Identity API │◄───│  Admin Portal   │   │
                    │  │ OIDC + Users │    │  (Super Admin)  │   │
                    │  └──────┬───────┘    └─────────────────┘   │
                    │         │ JWT / JWKS                         │
                    └─────────┼────────────────────────────────────┘
                              │
         ┌────────────────────┼────────────────────┐
         │                    │                    │
         ▼                    ▼                    ▼
  DocForge_FE           DocForge_PyService    BookingHotels
  (client SPA)          (verify JWT)          (client + API)
```

- **Identity**: authority duy nhất.  
- **Admin Portal**: chỉ user có role/permission platform mới vào được.  
- **Sản phẩm con**: login qua Identity; business DB riêng.

---

## 4. Hai thành phần cốt lõi

### 4.1. Platform.Identity (API)

- OAuth2 / OIDC (OpenIddict): `/connect/token`, discovery, JWKS  
- User lifecycle: register, profile, **change-password** (revoke mọi session)  
- Role & permission (ABP)  
- Quản lý OpenIddict **Applications** (clients) — qua API hoặc Admin UI  
- PostgreSQL (+ Redis optional)

Chi tiết kỹ thuật Identity tham khảo cấu trúc **QuickBite.Identity**:
<https://github.com/ayana0409/QuickBite/tree/main/src/quick-bite-identity/QuickBite.Identity>
(ABP + OpenIddict, chia module Domain/Application/EntityFrameworkCore/HttpApi/Web theo chuẩn ABP). Copy **tinh thần cấu trúc module**, không copy nguyên khối — Platform không có nghiệp vụ đơn hàng/canteen của QuickBite.

> ⚠️ **Bản quyền / chi phí — bắt buộc đọc trước khi code:**
> - Chỉ dùng **ABP Framework Community** (MIT, miễn phí) — **không** dùng ABP Commercial/Business template (trả phí theo license).
> - Chỉ dùng **OpenIddict OSS** (thư viện gốc của Kévin Chalet, Apache-2.0, miễn phí) để phát hành token — **không** dùng module **"OpenIddict Pro" / "OpenIddict UI"** của ABP.IO, đây là module **trả phí** (nằm trong ABP Commercial). UI quản lý OpenIddict Applications ở mục 6.1 (Clients) **tự viết** (CRUD đơn giản gọi `IOpenIddictApplicationManager`), không cài module Pro.
> - Không dùng bất kỳ NuGet/npm package nào ghi rõ "Pro", "Commercial", "License required" trong tài liệu của nó.

Xem thêm mục API & bảo mật bên dưới.

### 4.2. Platform.Admin (UI)

Web dành **Super Admin**, tham khảo bề mặt quản trị kiểu BookingHotels Admin (user, role, khóa…) nhưng **phạm vi toàn platform**, không phải CRUD phòng/khách sạn.

**Công nghệ UI (chọn một khi implement):**

| Lựa chọn | Ưu | Hợp khi |
|----------|----|--------|
| **ABP MVC / Blazor** trong cùng solution | Nhanh với permission ABP, một stack .NET | Muốn ít FE riêng |
| **Vue/React SPA** gọi Identity API | Thống nhất với DocForge FE | Team mạnh SPA |

Khuyến nghị mặc định trong PLAN: **ABP UI (MVC hoặc Blazor)** cho Admin để gắn permission sẵn; có thể đổi SPA sau.

---

## 5. Mô hình phân quyền

### 5.1. Role

| Role | Phạm vi | Ghi chú |
|------|---------|--------|
| `User` | End-user mọi app | Gán khi đăng ký |
| `Admin` | Super Admin platform | Vào Admin Portal; quản user/role/client |
| (Sau) `Hotel.Staff` … | Theo sản phẩm | Optional; hoặc chỉ dùng permission |

User **nhiều role** được (ABP).

### 5.2. Permission (namespace)

**Platform / Identity**

| Permission | Ai cần |
|------------|--------|
| `Identity.Users.View` | Xem danh sách user |
| `Identity.Users.Manage` | Sửa, khóa, gán role |
| `Identity.Roles.Manage` | CRUD role, gán permission |
| `Identity.Clients.Manage` | Quản OpenIddict clients |
| `Platform.Admin.Access` | Vào được Admin Portal |
| `Platform.Dashboard` | Xem dashboard tổng |

**DocForge** (gán cho user dùng tool; Admin có thể có thêm)

| Permission | |
|------------|--|
| `DocForge.Convert` | |
| `DocForge.Transcribe` | |
| `DocForge.Translate` | |
| `DocForge.History` | |
| `DocForge.Admin.*` | Nếu sau có admin riêng trong DocForge |

**Hotel** (khi tích hợp)

| Permission | |
|------------|--|
| `Hotel.Booking.Manage` | |
| `Hotel.Room.Edit` | |
| `Hotel.Admin` | |

Super Admin (`Admin` role) được seed đủ `Identity.*` + `Platform.*`.  
Không copy hàng trăm permission theo từng bảng như BookingHotels ngay ngày đầu; mở rộng theo module khi cần.

### 5.3. Đổi mật khẩu & session

- `POST .../change-password`  
- Đổi hash → **revoke mọi refresh token** của user  
- Client **xóa token** và **bắt đăng nhập lại** bằng mật khẩu mới  
- Admin reset mật khẩu hộ user: cùng chính sách revoke session

---

## 6. Super Admin UI — chức năng

Tham khảo nhóm việc Admin bên BookingHotels (user, role, khóa), nâng lên **tầng platform**.

### 6.1. MVP Admin Portal

| Module | Chức năng |
|--------|-----------|
| **Đăng nhập** | Login qua cùng Identity (chỉ user có `Platform.Admin.Access`) |
| **Dashboard** | Số user, số đăng ký gần đây, (sau) số client |
| **Users** | Danh sách, tìm kiếm, xem chi tiết, khóa/mở khóa, gán role, (optional) reset password |
| **Roles** | Danh sách role, xem permission của role, sửa gán permission |
| **Clients** | Xem / thêm / sửa OpenIddict applications (client_id, redirect URIs, bật/tắt) — hoặc phase 2 nếu seed tay lúc đầu |

### 6.2. Phase sau (Admin)

| Module | Chức năng |
|--------|-----------|
| Audit log | Ai khóa user, ai đổi role |
| Email / template | Reset password, thông báo |
| Thống kê theo app | Bao nhiêu user đụng DocForge vs Hotel (nếu có telemetry) |
| Cấu hình hệ thống | Feature flag đơn giản |

### 6.3. Không làm trên Platform Admin

- Quản lý phòng, đơn đặt phòng → BookingHotels Admin  
- Quản lý job convert, file upload → DocForge (nếu có)  
- Soạn nội dung marketing sản phẩm con  

---

## 7. Tích hợp sản phẩm con

### 7.1. DocForge

| Phía | Việc |
|------|------|
| FE | Login/register/change-password UI; Bearer; guest vẫn convert |
| PyService | Verify JWT qua JWKS; optional auth |
| Platform | Client `DocForge_Web`; permission `DocForge.*` |

### 7.2. BookingHotels

| Phía | Việc |
|------|------|
| Web/API | Client `BookingHotels_Web`; verify JWKS |
| Platform | Permission `Hotel.*`; Super Admin gán role cho staff |
| Admin nghiệp vụ | Vẫn có thể có Admin Hotel riêng (phòng, đơn) — **khác** Platform Admin |

### 7.3. Thêm app mới

1. Super Admin (hoặc seed) tạo client OIDC.  
2. Thêm CORS origin.  
3. (Optional) permission group `NewApp.*`.  
4. App cấu hình Authority + client_id + JWKS.

---

## 8. Stack công nghệ

| Thành phần | Công nghệ | Ghi chú license |
|------------|-----------|------------------|
| Identity API | .NET 8/10, **ABP Community**, **OpenIddict OSS**, EF Core, PostgreSQL | Free/OSS |
| Admin Portal | ABP MVC/Blazor **hoặc** SPA (Vue) | Free/OSS |
| Cache | Redis optional | Free tier (Upstash/Redis Cloud) nếu cần |
| Log | Serilog (console + file, JSON) | Free/OSS — xem §11.2 |
| DocForge FE (client) | Vue 3 (giữ nguyên) | Free/OSS |
| DocForge API (client) | FastAPI (giữ nguyên) | Free/OSS |

Nguyên tắc chọn stack: **mọi thư viện/dịch vụ dùng trong Platform phải free hoặc open-source**, không phát sinh chi phí license hay vi phạm điều khoản sử dụng khi build/deploy. Danh sách dịch vụ hạ tầng free cụ thể (hosting, DB, registry) nằm trong `PLAN.md` §0.

---

## 9. API chính (Identity)

### OIDC

| Method | Endpoint |
|--------|----------|
| POST | `/connect/token` |
| GET | `/.well-known/openid-configuration` |
| GET | `/.well-known/jwks.json` |

### Application / Admin API

| Method | Endpoint | Auth | Mô tả |
|--------|----------|------|-------|
| POST | `/api/app/auth/register` | Public | Đăng ký |
| GET/PUT | `/api/app/auth/profile` | Bearer | Profile |
| POST | `/api/app/auth/change-password` | Bearer | Đổi MK + revoke sessions |
| GET | `/api/identity/users` | Admin | Danh sách user |
| PUT | `/api/identity/users/{id}/roles` | Admin | Gán role |
| POST | `/api/identity/users/{id}/lock` | Admin | Khóa user |
| GET/PUT | `/api/identity/roles` | Admin | Role & permission |
| CRUD | `/api/openiddict/applications` (hoặc tương đương) | Admin | Clients — phase tùy chọn |

Admin Portal chỉ là consumer có quyền của các API trên (+ UI).

---

## 10. Bảo mật

- RS256 + JWKS; HTTPS production  
- Access token ngắn; refresh rotation  
- Đổi MK → revoke mọi refresh; FE bắt login lại  
- Admin Portal chỉ role có `Platform.Admin.Access`  
- CORS whitelist từng FE (DocForge, Hotel, Admin origin)  
- Không log password/token  
- Certificate ký OpenIddict thật trên production  

---

## 11. Cấu hình

**Identity**

- Connection string PostgreSQL  
- `SelfUrl` / Authority  
- `CorsOrigins`: DocForge FE, Admin Portal, Hotel…  
- Google (phase sau)  

**Admin Portal**

- Authority = Identity URL  
- Chỉ deploy nội bộ / VPN / IP allowlist nếu cần  

**DocForge FE / PyService**

- `IDENTITY_URL`, `client_id=DocForge_Web`  
- `IDENTITY_JWKS_URL`  

**Quy ước biến môi trường / secret (local & CI):**

- `.env.example` (commit vào git) liệt kê **tên** biến, không có giá trị thật.
- `.env` (gitignored, **không** commit) chứa giá trị thật dùng khi chạy local/docker-compose.
- Trên staging/production: không dùng file `.env` — set qua secret store của nền tảng deploy (Render env vars, GitHub Actions secrets…). Xem `PLAN.md` §0.6b.

### 11.1. CI/CD (bắt buộc khi deploy)

Platform **phải** có pipeline từ sớm (chi tiết trong `PLAN.md` §2.5):

| Tầng | Yêu cầu tối thiểu |
|------|-------------------|
| **CI** | Mỗi PR/push: restore + build (+ test); fail thì không merge |
| **CD** | Build image/artifact → staging → smoke (health, OIDC discovery, JWKS) → production (có approve) |
| **DB** | Migrate có kiểm soát (DbMigrator job), backup trước production |
| **Secret** | Env / secret store; không commit connection string, `.pfx` |

Authority production dùng **HTTPS** và URL ổn định để mọi client (DocForge, Hotel) trỏ một lần.

Toàn bộ hạ tầng CI/CD (registry, hosting, DB) **phải dùng gói free/không thẻ tín dụng hoặc free-tier hợp pháp** — danh sách dịch vụ cụ thể + hướng dẫn tạo tài khoản nằm trong `PLAN.md` §0.

### 11.2. Logging & Observability (bắt buộc — để debug được sau khi deploy)

> Nguyên tắc: **làm gì cũng phải log ra được theo lô CI/CD**, để khi có bug trên staging/production, nhìn log là biết build nào, commit nào, request nào gây lỗi — không phải SSH mò.

| Yêu cầu | Chi tiết |
|---------|---------|
| **Structured logging** | Dùng Serilog, format **JSON**, ghi ra console (stdout) — mọi nền tảng hosting free (Render, Fly.io, Docker log driver…) đều capture được stdout mà không cần cấu hình thêm |
| **Correlation với bản deploy** | Mỗi log line có field `git_sha` (commit đang chạy) + `build_id`/`run_id` (CI run) — set qua biến môi trường lúc build image, đọc vào `Serilog.Enrichers` |
| **Correlation theo request** | Mỗi request có `trace_id`/`correlation_id` (middleware sinh hoặc lấy từ header `X-Request-Id`) — log xuyên suốt từ Identity đến response |
| **Không log dữ liệu nhạy cảm** | Không log password, token, connection string (giữ nguyên §10) |
| **Log CI/CD tự thân** | Mỗi bước CI (build/test) và CD (deploy/migrate/smoke test) log rõ **pass/fail + thời gian**, lưu lại trong CI provider (GitHub Actions giữ log free, không giới hạn thời gian với repo public) để tra cứu khi rollback |
| **Xem log sau deploy** | Bước đầu: dùng log viewer có sẵn của nền tảng hosting free (Render/Fly.io dashboard). Khi cần tập trung nhiều service: cân nhắc free tier của Grafana Cloud / Better Stack (Logtail) — **chỉ thêm khi thật sự cần**, không bắt buộc từ Phase 0 |

---

## 12. Ranh giới rõ ràng

| Câu hỏi | Trả lời |
|---------|---------|
| User login ở đâu? | Identity (qua UI từng app hoặc Admin) |
| Super Admin quản user ở đâu? | **Platform Admin Portal** |
| Admin sửa giá phòng ở đâu? | BookingHotels — không phải Platform |
| Convert PDF cần Platform không? | Không bắt buộc (guest); có token thì gắn user |
| Thêm app mới có tạo Identity mới không? | **Không** — chỉ thêm client |

---

*Tài liệu thiết kế mục tiêu. Chi tiết sprint xem `PLAN.md`.*

# Platform — PLAN triển khai

> Kế hoạch thực hiện **Platform** = Identity API + Super Admin UI.
> Bám `DOCS.md`. DocForge là client đầu tiên; BookingHotels gắn sau.

---

## 0.0. Dịch vụ free sẽ dùng (chốt trước khi code)

> Nguyên tắc xuyên suốt PLAN: **mọi bước từ code → CI → deploy đều phải chạy được bằng gói free / free-tier hợp pháp**, không cần thẻ tín dụng nếu tránh được, và **không vi phạm license/điều khoản sử dụng** của bất kỳ thư viện hay dịch vụ nào.

| Nhu cầu | Dịch vụ free | Vì sao chọn | Giới hạn cần biết |
|---------|--------------|-------------|--------------------|
| Git hosting + CI | **GitHub** (repo public hoặc private) + **GitHub Actions** | Free, không giới hạn thời gian lưu log CI với repo public; private có free minutes/tháng | Private repo: giới hạn phút CI/tháng theo gói free |
| Container registry | **GHCR** (ghcr.io, gắn liền GitHub) | Free cho image public; free quota cho private, cùng hệ sinh thái GitHub Actions (không cần secret riêng) | Image private có giới hạn dung lượng/băng thông theo tài khoản |
| Database PostgreSQL | **Neon** (neon.tech) | Free vĩnh viễn, không thẻ tín dụng, scale-to-zero khi idle, đủ cho Identity DB giai đoạn đầu | 0.5 GB storage/project, ~100 compute-hours/tháng — theo dõi nếu traffic tăng |
| Hosting Identity API + Admin Portal | **Render** (Free Web Service) | Free vĩnh viễn, không thẻ tín dụng, deploy thẳng từ Docker image/GitHub | Sleep sau ~15 phút không có traffic, cold start vài chục giây — chấp nhận được cho MVP/staging, cân nhắc nâng cấp khi lên production thật |
| Framework Identity | **ABP Framework Community** (MIT) | Free, đủ tính năng Identity/Permission cần dùng | **Không** dùng ABP Commercial/Business template |
| OIDC server | **OpenIddict OSS** (Apache-2.0) | Free, đủ cho `/connect/token`, JWKS, quản lý Applications qua API tự viết | **Không** cài module **"OpenIddict Pro"/"OpenIddict UI"** của ABP.IO (trả phí) |
| Log tập trung (khi cần) | Dashboard log có sẵn của Render/GitHub Actions trước; **Grafana Cloud free tier** hoặc **Better Stack (Logtail) free tier** nếu cần sau | Đủ dùng cho MVP, không phát sinh chi phí | Thêm ở Phase 4 nếu thật sự cần, không bắt buộc từ đầu |

Khi một dịch vụ free hết hạn mức (Neon hết compute-hours, Render sleep gây ảnh hưởng demo…), ghi vào README lý do và phương án thay thế free khác — **không** mặc định chuyển sang gói trả phí mà không có quyết định rõ ràng.

---

## 0. Mục tiêu cuối (Definition of Done — toàn Platform MVP)

- [ ] Repo Platform riêng, **CI xanh** trên mọi PR/push `main`
- [ ] **CD**: deploy Identity + Admin Portal lên môi trường staging/production (pipeline hoặc 1-click có document)
- [ ] Đăng ký / đăng nhập / logout / đổi mật khẩu (đổi xong **revoke session**, bắt login lại)
- [ ] JWT RS256 + JWKS; DocForge PyService verify được
- [ ] DocForge FE: auth UI + guest convert vẫn chạy
- [ ] Super Admin đăng nhập **Admin Portal**, xem/sửa user, gán role, khóa user
- [ ] Seed role `User` / `Admin`, client `DocForge_Web`
- [ ] Secret không nằm trong git; cấu hình qua env / secret store
- [ ] Toàn bộ hạ tầng (Git/CI/registry/DB/hosting) dùng gói **free/free-tier**, không phát sinh chi phí, không dùng module trả phí (xem §0.0)
- [ ] Log dạng JSON có `git_sha` + `build_id` + `trace_id`; xem được log của một lần deploy cụ thể để fix bug (xem §2.5-G)
- [ ] README + DOCS cập nhật đúng thực tế code + hướng dẫn deploy

---

## 1. Nguyên tắc làm việc

1. **Identity trước — Admin UI ngay sau khi có API user/role** (không để Admin quá trễ).
2. DocForge **không** break guest convert.
3. Mọi quyết định đa app: thêm **client**, không nhân bản user DB.
4. Đổi mật khẩu = đổi hash + **revoke mọi refresh token** + FE/Admin xóa session local.
5. Mỗi phase có checklist merge được.
6. **CI/CD từ Phase 0**: không merge code “chỉ chạy trên máy local”; mọi thứ lên môi trường remote phải qua pipeline (hoặc script deploy đã gắn CI).

---

## 2. Các phase

### Phase 0 — Khởi tạo repo & skeleton + CI (3–5 ngày)

> Làm **đúng thứ tự từng bước** dưới đây — đây là điểm bắt đầu thật sự của dự án, từ tài khoản trống đến repo có CI xanh. Toàn bộ dịch vụ dùng ở đây là **free** (xem §0.0).

| # | Việc | Lệnh / thao tác cụ thể | Output |
|---|------|------------------------|--------|
| 0.1 | Tạo tài khoản GitHub (nếu chưa có) | github.com → Sign up (free) | Tài khoản GitHub |
| 0.2 | Tạo repo trống trên GitHub | GitHub → **New repository** → tên `Platform` (hoặc `VzTong.Platform`) → **không** tick "Add README" (sẽ tạo local rồi push) | Repo GitHub trống |
| 0.3 | Clone repo về máy | `git clone https://github.com/<org>/Platform.git` `cd Platform` | Thư mục local rỗng, đã trỏ remote |
| 0.4 | Cài .NET SDK (nếu chưa có) | Cài .NET 8 SDK (hoặc 10 nếu chốt version mới hơn) | `dotnet --version` chạy được |
| 0.5 | Tạo solution + project skeleton | `dotnet new sln -n Platform` rồi tạo các project theo ABP (dùng ABP CLI **Community**: `dotnet tool install -g Volo.Abp.Cli` → `abp new Platform.Identity -t app --database-provider ef -u none` **hoặc** tạo tay theo cấu trúc tham khảo QuickBite.Identity, xem `DOCS.md` §4.1) | `src/Platform.Identity.*` build được |
| 0.6 | Thêm `.gitignore` cho .NET | Dùng template chuẩn (`bin/`, `obj/`, `*.user`, `appsettings.*.Development.json`, `*.pfx`, **`.env`**) | Không commit rác build lẫn secret |
| 0.6b | Tạo `.env.example` (commit) + `.env` (gitignored, KHÔNG commit) | `.env.example` liệt kê tên biến (`ConnectionStrings__Default=`, `App__SelfUrl=`, …) không có giá trị thật; `.env` chứa giá trị thật (connection string Neon, key…) dùng cho docker-compose/local run | Người sau clone repo biết cần khai báo biến gì, mà không lộ secret |
| 0.7 | Thêm `.dockerignore` tương ứng | Loại trừ `bin/`, `obj/`, `.git/` | Build image không kéo rác |
| 0.8 | Commit khởi tạo | `git add .` → `git commit -m "chore: init Platform skeleton"` → `git push -u origin main` | Commit đầu tiên trên GitHub |
| 0.9 | Copy `DOCS.md` + `PLAN.md` vào repo (root) | `git add DOCS.md PLAN.md` → commit | Tài liệu nằm trong repo, versioned cùng code |
| 0.10 | Tạo tài khoản Neon (free Postgres) | neon.tech → Sign up (không cần thẻ) → **New Project** → copy connection string | Connection string Postgres dev |
| 0.11 | Cấu hình connection string qua **user-secrets** (dev) — **không** commit vào `appsettings.json` | `dotnet user-secrets init` (trong project Web/DbMigrator) → `dotnet user-secrets set "ConnectionStrings:Default" "<neon-connection-string>"` | Secret không nằm trong git |
| 0.12 | Chạy thử `DbMigrator` với Neon dev | `dotnet run --project src/Platform.Identity.DbMigrator` | Schema tạo được trên Neon |
| 0.13 | `Dockerfile` cho Identity Web (multi-stage, `mcr.microsoft.com/dotnet/sdk` → `aspnet` runtime) | Viết `Dockerfile` ở root hoặc trong project | `docker build .` chạy được local |
| 0.14 | Tạo GitHub Actions workflow CI (`.github/workflows/ci.yml`) | Xem mẫu YAML ở §2.5-B | Push/PR trigger build |
| 0.15 | Bật **branch protection** cho `main`: require CI pass trước khi merge | GitHub repo → Settings → Branches → Add rule | Không merge được code đỏ |
| 0.16 | Tạo GitHub Actions workflow build & push image lên **GHCR** khi merge `main` | Dùng `secrets.GITHUB_TOKEN` có sẵn, không cần tạo secret riêng | Image `ghcr.io/<org>/platform-identity:<sha>` |

**DoD Phase 0:** `dotnet build` + `DbMigrator` tạo được schema trên Neon; repo có `.gitignore`/`.dockerignore` sạch; **CI chạy xanh trên GitHub Actions**; branch protection bật; image build/push GHCR thành công ít nhất 1 lần.

---

### Phase 1 — Identity core (1–2 tuần)

| # | Việc | Output |
|---|------|--------|
| 1.1 | Seed role `User`, `Admin` | DB có 2 role |
| 1.2 | Seed permission `Platform.*`, `Identity.*`, `DocForge.*` (bộ MVP) | Permission definitions |
| 1.3 | Gán permission cho `Admin` / `User` | Mapping đúng DOCS |
| 1.4 | Seed OpenIddict client `DocForge_Web` | Login SPA được |
| 1.5 | Register API | User mới + role User |
| 1.6 | Login `/connect/token` (password + refresh) | Trả access + refresh |
| 1.7 | Profile GET/PUT | |
| 1.8 | **Change-password** + revoke mọi refresh token user | Bắt buộc login lại |
| 1.9 | JWKS + discovery hoạt động | PyService/test verify được |
| 1.10 | CORS: origin DocForge FE (+ Admin sau) | |
| 1.11 | User lock/unlock (API) | Admin dùng sau |
| 1.12 | Test tích hợp cơ bản (register → login → profile → change-password → login lại) | |

**DoD Phase 1:** Postman/curl đủ vòng đời user; đổi MK xong refresh cũ fail.

---

### Phase 2 — Super Admin Portal MVP (1–2 tuần)

| # | Việc | Output |
|---|------|--------|
| 2.1 | Chọn UI: ABP MVC/Blazor **hoặc** SPA | Quyết định ghi README |
| 2.2 | Host Admin + login qua Identity | Chỉ user có `Platform.Admin.Access` vào được |
| 2.3 | Layout Admin (menu: Dashboard, Users, Roles) | |
| 2.4 | **Users**: list, search, chi tiết | |
| 2.5 | **Users**: gán/gỡ role | |
| 2.6 | **Users**: khóa / mở khóa | |
| 2.7 | **Users**: (optional) admin reset password → revoke session user đó | |
| 2.8 | **Roles**: list + xem permission đang gán | |
| 2.9 | **Roles**: sửa permission của role (MVP có thể đơn giản) | |
| 2.10 | Dashboard đơn giản (số user, user mới 7 ngày) | |
| 2.11 | Seed 1 tài khoản Super Admin ban đầu | `admin@...` / đổi MK ngay |

**DoD Phase 2:** Super Admin thao tác user/role trên UI; user thường không vào được Admin.

---

### Phase 3 — Tích hợp DocForge (1 tuần)

| # | Việc | Output |
|---|------|--------|
| 3.1 | FE: `VITE_IDENTITY_URL`, `client_id` | |
| 3.2 | FE: `useAuth` + interceptor Bearer | |
| 3.3 | FE: trang đăng nhập / đăng ký / đổi MK | Đổi MK → clear token → về login |
| 3.4 | FE: Menu theo trạng thái đăng nhập | |
| 3.5 | PyService: JWKS verify optional | |
| 3.6 | Kiểm tra guest: convert **không** cần token | |
| 3.7 | Kiểm tra user: Bearer hợp lệ; token hết hạn → refresh/logout | |
| 3.8 | Cập nhật DocForge DESIGN / README (link Platform) | |

**DoD Phase 3:** User thật trên DocForge login được; guest vẫn convert; Admin quản được user đó trên Portal.

---

### Phase 4 — Củng cố & đa client (linh hoạt)

| # | Việc | Ưu tiên |
|---|------|---------|
| 4.1 | Refresh token rotation siết | Cao |
| 4.2 | UI/API quản lý **Clients** (OpenIddict apps) trên Admin | Trung bình |
| 4.3 | Authorization code + PKCE cho SPA | Trung bình |
| 4.4 | Google login | Thấp |
| 4.5 | Quên mật khẩu (email) | Thấp — sau đổi MK |
| 4.6 | Audit log thao tác Admin | Thấp |
| 4.7 | Seed/placeholder client `BookingHotels_Web` | Khi bắt đầu Hotel |
| 4.8 | Redis permission cache | Khi cần scale |
| 4.9 | CD nâng cao: blue/green hoặc slot swap; backup DB định kỳ | Trung bình |

---

## 2.5. CI/CD (bắt buộc vì sẽ deploy)

> Làm **song song từ Phase 0–1**, không để đến lúc “code xong mới nghĩ deploy”.

### A. Mục tiêu CI/CD

| Môi trường | Mục đích |
|------------|----------|
| **CI** (mọi PR / push) | Build + test tự động; chặn merge khi đỏ |
| **Staging** | Identity + Admin + DB riêng; DocForge trỏ staging Identity để thử |
| **Production** | URL Authority cố định (HTTPS); secret thật; certificate ký OpenIddict |

### B. CI — Continuous Integration

**Trigger:** pull request + push `main` (và `develop` nếu có).

**Job tối thiểu:**

1. Checkout
2. Setup .NET SDK (đúng version solution)
3. `dotnet restore`
4. `dotnet build --no-restore -c Release`
5. `dotnet test` (khi đã có test project; Phase 0 có thể skip nếu chưa có test)
6. (Optional) `docker build` để chắc Dockerfile không gãy

**Artifact (optional):** publish output hoặc image tag `sha` ngắn.

**Branch protection (khuyến nghị):**

- Require CI green trước khi merge `main`
- Không push secret; dùng GitHub Secrets / GitLab CI variables

**Ví dụ cấu trúc file (GitHub Actions):**

```yaml
# .github/workflows/ci.yml
name: CI
on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main, develop]
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "8.0.x"   # hoặc 10.x — khớp solution
      - run: dotnet restore
      - run: dotnet build -c Release --no-restore
      - run: dotnet test -c Release --no-build --verbosity normal
        continue-on-error: false   # bỏ step này nếu chưa có test
```

### C. CD — Continuous Deployment

**Nguyên tắc:**

- Migration DB chạy **có kiểm soát** (job riêng hoặc bước trước khi bật traffic).
- Image/artifact version theo **git sha** hoặc semver tag.
- Config qua **env / secret**, không bake connection string vào image.
- Identity và Admin có thể cùng image khác entrypoint hoặc hai service.

**Nền tảng deploy đã chốt (free, xem lý do ở §0.0):**

| Nền | Dùng cho | Ghi chú free |
|-----|----------|--------------|
| **Render (Free Web Service)** | Identity API + Admin Portal (staging, và production giai đoạn đầu) | Không thẻ tín dụng; sleep sau ~15 phút idle — chấp nhận cho MVP, note rõ trong README để không bị hiểu nhầm là bug |
| **Neon (Free Postgres)** | DB staging + production giai đoạn đầu | Scale-to-zero khi idle, 0.5GB/project — theo dõi dung lượng |
| **GHCR** | Registry image Identity/Admin | Gắn liền GitHub Actions, không cần secret riêng |

Nếu sau này traffic thật cần always-on (không sleep) hoặc DB lớn hơn, đó là quyết định nâng cấp có chủ đích ở Phase 4 — ghi lý do vào README, không nâng cấp "mặc định".

**Luồng CD tối thiểu (staging rồi production):**

```
CI xanh trên main
    → Build image (Identity, Admin)
    → Push registry (GHCR / Docker Hub)
    → Deploy staging
    → Chạy DbMigrator (staging)
    → Smoke test: /health + /.well-known/openid-configuration
    → [Manual approve] Deploy production
    → DbMigrator production
    → Smoke test production
```

**Smoke test sau deploy (bắt buộc trong script/CD):**

- [ ] `GET {Authority}/health` hoặc root → 200
- [ ] `GET {Authority}/.well-known/openid-configuration` → 200 + issuer đúng
- [ ] `GET {Authority}/.well-known/jwks.json` → có keys
- [ ] Admin Portal load được trang login

### D. Secret & config khi deploy

| Biến | Ghi chú |
|------|---------|
| `ConnectionStrings__Default` | PostgreSQL |
| `App__SelfUrl` / Authority | HTTPS production |
| `App__CorsOrigins` | FE DocForge, Admin, Hotel… |
| OpenIddict signing cert / key | File mount hoặc secret; **không** commit `.pfx` |
| `Authentication__Google__*` | Phase sau |

Dùng GitHub Environments (`staging` / `production`) + protection rules cho production.

### E. Database migration trong CD

- **Không** auto-migrate trong `Startup` trên production nếu chưa kiểm soát được.
- Khuyến nghị: step `dotnet run --project DbMigrator` (hoặc container migrator one-shot) **trước** khi rollout web.
- Backup DB trước migrate production (checklist release).

### F. Việc gắn vào từng phase

| Phase | CI/CD việc thêm |
|-------|------------------|
| **0** | Workflow CI build; Dockerfile |
| **1** | CD staging Identity; smoke OIDC; secrets staging |
| **2** | Deploy Admin Portal staging; CORS Admin origin |
| **3** | DocForge FE/PyService trỏ staging Identity; sau đó production Authority |
| **4** | Làm cứng production (approve tay, backup, monitor) |

### G. Logging — bắt buộc để debug sau deploy

> Deploy free (Render sleep, cold start…) càng cần log rõ ràng, vì không có APM trả phí đứng sau. Chi tiết kỹ thuật xem `DOCS.md` §11.2, ở đây là việc cần làm trong pipeline:

| # | Việc | Output |
|---|------|--------|
| H.1 | Cấu hình Serilog ghi JSON ra console (stdout) cho cả Identity và Admin | `docker logs` / Render log stream đọc được structured log |
| H.2 | Build image truyền `git_sha` (từ `${{ github.sha }}`) và `build_id` (`${{ github.run_id }}`) vào biến môi trường / enrich log | Mỗi log line biết chính xác đang chạy build nào |
| H.3 | Middleware sinh `trace_id` mỗi request (hoặc lấy `X-Request-Id`), log xuyên suốt | Trace được một request lỗi cụ thể |
| H.4 | Mỗi step CI/CD (build, test, migrate, smoke test, deploy) in rõ **kết quả pass/fail** ra GitHub Actions log | Khi CD fail, biết ngay fail ở bước nào mà không cần đoán |
| H.5 | README ghi rõ cách xem log production (link dashboard Render, cách filter theo `git_sha`) | Người sau (hoặc chính mình 1 tháng sau) fix bug nhanh |

### H. DoD CI/CD (gắn vào DoD toàn MVP)

- [ ] Mỗi PR đỏ thì không merge
- [ ] `main` luôn build được trên CI
- [ ] Staging Identity URL public (hoặc VPN) + JWKS gọi được từ PyService staging
- [ ] Production deploy lặp lại được từ pipeline/script (không chỉ “copy file lên server bằng tay” không ghi lại)
- [ ] Runbook ngắn trong README: deploy, rollback, migrate
- [ ] Log JSON có `git_sha`/`build_id`/`trace_id`; README ghi cách xem log theo từng lần deploy (xem §2.5-G)

---

## 3. Thứ tự ưu tiên trong sprint

```
Phase 0 skeleton + CI + Dockerfile
    → Phase 1 Identity + CD staging Identity
        → Phase 2 Admin Portal + deploy Admin staging
            → Phase 3 DocForge gắn Identity (staging → production)
                → Phase 4 củng cố / Hotel client / CD production cứng
```

Không làm Admin UI trước khi change-password + lock user API ổn.
Không làm Google trước khi password flow chắc.

---

## 4. Phân rã kỹ thuật gợi ý

### 4.1. Identity

- ABP module Identity + Permission + OpenIddict
- `AuthAppService`: Register, ChangePassword
- ChangePassword: `UserManager.ChangePasswordAsync` → `OpenIddictToken` revoke theo `subject`
- PermissionDefinitionProvider: khai báo `Platform.*`, `Identity.*`, `DocForge.*`
- DataSeed: roles, role-permissions, applications

### 4.2. Admin Portal

- Route/menu dựa trên permission
- Users page → Identity user API
- Không nhúng business DocForge/Hotel

### 4.3. DocForge client

- `useAuth.js`
- Sau `changePassword()` success: `clearTokens()` + `router.push('/dang-nhap')`

---

## 5. Rủi ro & cách giảm

| Rủi ro | Giảm |
|--------|------|
| Làm Identity quá lớn trước khi có UI | Chốt Phase 1 API mỏng + Phase 2 Admin ngay |
| Break guest DocForge | Mọi API convert mặc định optional auth; test case guest |
| Password grant kém an toàn lâu dài | MVP ok; Phase 4 chuyển PKCE |
| Nhầm Admin Platform với Admin Hotel | DOCS ranh giới; không làm CRUD phòng trên Platform |
| Đổi MK vẫn dùng được token cũ | Bắt buộc revoke refresh; access ngắn hạn; test case |

---

## 6. Checklist từng lần release nhỏ

- [ ] CI xanh trên commit/release tag
- [ ] Build + migrate OK (staging/production đúng thứ tự)
- [ ] Smoke: health + openid-configuration + jwks
- [ ] Register / login / refresh
- [ ] Change-password → refresh cũ 400/invalid
- [ ] Admin chỉ user có quyền
- [ ] DocForge guest convert (khi đã gắn)
- [ ] Không secret trong git
- [ ] Biết cách rollback (image tag trước / backup DB)
- [ ] Log của lần deploy này tra được theo `git_sha` (Render log stream hoặc GitHub Actions run)
- [ ] Không có dịch vụ/thư viện nào mới thêm vào bị trả phí hoặc vi phạm license (đối chiếu §0.0)

---

## 7. Tài liệu đi kèm

| File | Nội dung |
|------|----------|
| `DOCS.md` | Thiết kế tổng thể Platform |
| `PLAN.md` | File này — thứ tự làm |
| DocForge `DESIGN.md` | Cập nhật khi Phase 3 xong (link Platform) |
| (Cũ) `PLAN_identity.md` | Có thể archive hoặc redirect sang `Platform/DOCS.md` + `PLAN.md` |

---

## 8. Tóm tắt một câu

> Làm **Platform repo riêng**: trước hết Identity (auth + JWT + đổi MK hủy session), ngay sau đó **Super Admin UI** quản user/role toàn hệ thống, rồi mới gắn DocForge làm client; Hotel và app khác chỉ thêm client + permission sau.

---

*Cập nhật checkbox khi hoàn thành từng phase.*
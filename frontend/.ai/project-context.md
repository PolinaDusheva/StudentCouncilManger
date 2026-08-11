# Project context — Student Council (frontend)

> Работен контекст за AI асистенти и разработчици. **Обновявай го при всяка реализирана
> функционалност** — секция „Дневник на реализираното“ най-долу.

Последна редакция: 2026-08-11

---

## 1. Какво е това

React уеб клиент за Student Council API — вътрешна система на Студентския съвет при
Икономически университет – Варна. Backend-ът (`../backend`) е готов и **не се променя от
frontend-а**, освен по изрична заявка.

Договорът за заявките е **[`api-requests.xsd`](./api-requests.xsd)** — XSD схема на всички
входящи заявки към API-то (v1). Тя е копие на `../api-requests.xsd`.

Планът по модули е в **[`roadmap.md`](./roadmap.md)**.

### Как се ползва схемата

XSD-то описва **само заявки**, не отговори. Затова:

| Какво | Откъде идва | Файл |
|---|---|---|
| Enum стойности | XSD секция 2 | `src/lib/types/enums.ts` |
| Ограничения при валидация (дължини, шаблони) | XSD секция 1 | `src/lib/validation/common.ts` |
| Тела и параметри на заявки | XSD секции 4–12 | `src/lib/api/*.ts` |
| Типове на отговорите | C# record-ите в `../backend/src/StudentCouncil.Application/Features/**` | `src/lib/types/dto.ts` |

**Правило:** преди да добавиш нов API извикване, намери съответния `complexType` в XSD-то.
Документацията вътре казва HTTP метода, маршрута и източника на всяко поле
(`(path)`, `(query)`, `(body)`, `(multipart)`).

---

## 2. Стек

| | |
|---|---|
| Build | Vite 8 |
| UI | React 19 + TypeScript (strict) |
| Рутинг | React Router 7 (декларативен `<Routes>`) |
| Server state | TanStack Query 5 |
| Форми | react-hook-form + Zod 4 (`@hookform/resolvers`) |
| Стилове | Tailwind CSS 4 (`@tailwindcss/vite`, конфигурация в `src/index.css` чрез `@theme`) |
| Икони | lucide-react |

Алиас `@/` сочи `src/` (в `tsconfig.app.json` и `vite.config.ts` — **и двете** трябва да се
обновяват заедно).

---

## 3. Конвенции на API-то

Всичко по-долу е проверено в кода на backend-а, не предположено.

- **База:** `/api/v1/...`
- **JSON:** camelCase; enum-ите се предават като **стрингове**
- **Дати:** `DateTime` → ISO-8601 UTC; `DateOnly` → `YYYY-MM-DD`. И двете са `string` в TS.
- **Пагинация:** `PagedResult<T>` — `{ items, page, pageSize, totalCount, totalPages }`.
  Сървърът клампва `pageSize` в `[1, 100]` вместо да връща грешка.
- **Грешки:** RFC 7807 ProblemDetails с допълнително поле `code`
  (`invalid_credentials`, `not_found`, `password_change_required`, ...).
  Валидационните грешки носят и `errors: { поле: [съобщения] }`.
  Обработва се в `src/lib/api/problem.ts` → `ApiError`.
- **Rate limiting:** 100 заявки/мин глобално, 10/мин за `/auth/*` → 429.
- **Качване на файлове:** максимум 25 MB; сървърът проверява разширение **и** магически
  байтове, не само MIME типа.

### Автентикация

JWT bearer + refresh токен. Access токенът носи claims: `sub`, `email`, `name`, `role`,
`stamp`, `dept`, `deptId` и `must_change_password` (само когато е активен).

Три неща, които определят поведението на клиента:

1. **`stamp` claim се проверява при всяка заявка** срещу `SecurityStamp` в базата
   (`Infrastructure/DependencyInjection.cs`). Смяна на парола ротира stamp-а → **всички
   издадени access токени умират моментално**.
2. **Refresh токенът се ротира при всяко ползване.** Затова обновяването е *single-flight* —
   няколко паралелни 401-ци споделят едно `POST /auth/refresh`
   (`src/lib/api/client.ts`).
3. **Гейт за смяна на парола.** Докато `mustChangePassword` е вдигнат, API-то връща
   `403 password_change_required` на **всичко** освен `/auth/*`. Флагът **не се връща от
   `GET /auth/me`** — чете се от claim-а в токена (`src/lib/auth/jwt.ts`), за да работи и
   след презареждане на страницата.

> ⚠️ **`POST /auth/change-password` прекратява сесията.** Хендлърът ротира security stamp-а и
> извиква `RevokeAllForMemberAsync`. Няма начин сесията да се запази — единственият коректен
> ход е чист вход наново. Същото важи и за `reset-password`.

---

## 4. CORS и локална разработка

Backend-ът **нямаше** CORS. Добавен е по заявка (вариант А) в `../backend`:

- `Program.cs` — `AddCors` с origin-и от конфигурация; `app.UseCors()` е поставен **преди**
  `UseAuthentication` и `UseRateLimiter`, за да носят CORS хедъри и 401/429 отговорите
- `appsettings.json` — празен `Cors:Origins` (изключено по подразбиране)
- `appsettings.Development.json` — `http://localhost:5173` + `PasswordReset:ResetUrlBase`
  сочи локалния frontend

В dev режим CORS все пак не се задейства: Vite proxy-то препраща `/api` към
`http://localhost:5160`, така че заявките са same-origin. CORS-ът е за деплой.

### Пускане

```bash
# 1. Backend (изисква PostgreSQL на 5432)
cd backend/src/StudentCouncil.Api
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=studentcouncil;Username=postgres;Password=postgres"
dotnet user-secrets set "Jwt:SigningKey" "<поне 32 символа>"
dotnet user-secrets set "Seed:AdminEmail" "president@ue-varna.bg"
dotnet user-secrets set "Seed:AdminPassword" "Test1234"   # иначе се генерира и се логва
cd ../.. && dotnet run --project src/StudentCouncil.Api

# 2. Frontend
cd frontend && npm run dev     # http://localhost:5173

# Тестове
npm test                       # следящ режим
npm run test:run               # еднократно (за CI)
```

Сийдърът създава 4-те отдела и начален `OrgPresident` с `MustChangePassword = true` — тоест
първият вход **винаги** минава през екрана за смяна на парола.

Променливи на средата: виж `.env.example`.

---

## 5. Структура

```
frontend/
├── .ai/
│   ├── api-requests.xsd        # договорът за заявките (копие на ../api-requests.xsd)
│   └── project-context.md      # този файл
├── vitest.config.ts            # отделен от vite.config.ts (той е във функционална форма)
├── src/
│   ├── test/
│   │   ├── setup.ts            # jest-dom, msw, почистване + стъб за <dialog>
│   │   ├── server.ts           # msw сървър без подразбиращи се handler-и
│   │   └── renderWithProviders.tsx
│   ├── components/
│   │   ├── layout/             # AuthLayout (екрани без сесия), AppShell (със сесия)
│   │   └── ui/                 # Button, Input, Alert, Spinner, Select, Badge, Avatar,
│   │                           # EmptyState, Modal, ConfirmDialog, Table, Pagination
│   ├── lib/
│   │   ├── api/
│   │   │   ├── client.ts       # fetch wrapper: bearer, refresh, ProblemDetails
│   │   │   ├── problem.ts      # ApiError, NetworkError, бг съобщения по код
│   │   │   └── auth.ts         # /auth/* — Zod схеми + извиквания
│   │   ├── auth/
│   │   │   ├── AuthProvider.tsx, context.ts, useAuth.ts
│   │   │   ├── jwt.ts          # чете claims от access токена (без верификация)
│   │   │   └── tokenStorage.ts # localStorage + синхронизация между табове
│   │   ├── hooks/
│   │   │   ├── useDebounce.ts
│   │   │   └── useAuthenticatedImage.ts   # изображения зад bearer токен → blob URL
│   │   ├── types/              # enums.ts (от XSD), dto.ts (отговори)
│   │   ├── utils/              # cn.ts, format.ts (дати)
│   │   └── validation/common.ts
│   └── routes/
│       ├── guards.tsx          # RequireAuth / RequireAnonymous
│       ├── auth/               # Login, ForgotPassword, ResetPassword, ChangePassword
│       └── dashboard/
```

### Правила при добавяне на модул

1. Нов файл `src/lib/api/<модул>.ts` — Zod схеми **до** извикванията, които ги ползват.
2. Типовете на отговорите отиват в `src/lib/types/dto.ts`.
3. Нови enum-и → `src/lib/types/enums.ts` + бг етикет, ако се показва.
4. Страници в `src/routes/<модул>/`; маршрутът се добавя в `src/App.tsx`.
5. Достъпът се крие през `usePermissions()`, а не през проверка на роля —
   сървърът вече е изчислил флаговете.
6. **Обнови дневника долу.**

---

## 6. Дневник на реализираното

### 2026-08-11 — Модул 1: общи компоненти и тестова основа

**Тестова инфраструктура**
- Vitest + Testing Library + msw; `npm test` / `npm run test:run`
- `onUnhandledRequest: 'error'` — тест, който удря нестубнат ендпойнт, пада шумно
- **Адресът на jsdom е фиксиран** на `http://localhost` в `vitest.config.ts`.
  По подразбиране Vitest ползва `localhost:3000`, което не съвпадаше с msw handler-ите
  и всичките 7 теста падаха наведнъж

**Покритие: 44 теста в 7 файла**
- `client.ts` — bearer токен, обновяване с повторение, single-flight, чистене на сесия,
  анонимни ендпойнти, изграждане на query string
- `problem.ts`, `format.ts`, `useDebounce`, `Table`, `Pagination`, `ConfirmDialog`

**Два теста са проверени, че наистина хващат регресия** (счупих кода нарочно и потвърдих):
- Махане на single-flight → `expected 2 to be 1` ✓
- Махане на изместването по часова зона → `08.03.2026` вместо `09.03.2026` при `TZ=Pacific/Honolulu` ✓

**Компоненти:** `Select`, `Badge`, `Avatar`, `EmptyState`, `Modal`, `ConfirmDialog`,
`Table`, `Pagination`
**Помощни:** `formatDate` / `formatDateTime`, `useDebounce`, `useAuthenticatedImage`

**Три находки за следващите модули**

1. ⚠️ **Изображенията зад токен не работят с `<img src>`.** `GET /members/{id}/photo` иска
   `Authorization` хедър (проверено: 401 без токен), а браузърът не го слага на `<img>`.
   Затова `useAuthenticatedImage` тегли ръчно → `createObjectURL` → `revokeObjectURL` при
   размонтиране. **Същото ще трябва за сваляне на документи в модул 3.**
2. **Стойностите на HTTP хедъри трябва да са Latin-1.** Тестови токени на кирилица чупят
   `Headers.set` с `TypeError: Cannot convert argument to a ByteString`. Истинските JWT-та
   са base64url, така че проблемът е само в тестовите данни — но си струва да се знае.
3. **`bg-BG` форматира датата като `09.03.2026 г.`** — със суфикс „г.“. Това е коректно за
   езика и се приема както е, вместо да се реже след форматирането.

**Ограничения**
- `<dialog>` няма реализация в jsdom. В `src/test/setup.ts` има минимален стъб само за да
  се рендерира компонентът; **фокус, backdrop и Esc остават непроверени** — искат браузър
- `Table.onRowClick` е удобство само за мишка (`<tr>` не се фокусира). Екраните, които го
  ползват, **трябва** да сложат истински линк в някоя клетка за клавиатурен достъп

**Отложено (YAGNI):** `Textarea` → модул 3, `formatMinutes` → модул 5, `formatEur` → модул 6

---

### 2026-08-11 — Скелет + автентикация

**Инфраструктура**
- Vite + React 19 + TS (strict, `noUncheckedIndexedAccess`), алиас `@/`
- Tailwind 4 с `brand` палитра в `@theme`
- Vite proxy `/api` → `http://localhost:5160` (заобикаля CORS в dev)
- TanStack Query: `staleTime` 30s, без повторни опити при 4xx

**Слой за API**
- `client.ts` — bearer токен, single-flight refresh с повторение на заявката,
  ProblemDetails → `ApiError`, `NetworkError` при недостъпен сървър
- `problem.ts` — бг съобщения по `code`; `errorMessage()` за UI
- `enums.ts` — всички enum-и от XSD секция 2 + етикети за роля/отдел/статус
- `dto.ts` — типове на отговорите за всичките 6 области (не само auth, за да са налични
  при следващите модули)
- `common.ts` — `emailSchema`, `passwordSchema`, `guidSchema` по фасетите от XSD

**Автентикация (пълна)**
- `AuthProvider` — възстановяване на сесията при зареждане, вход, изход,
  синхронизация между табове, реакция на изтекла сесия
- `jwt.ts` — чете `must_change_password` от токена (`/auth/me` не го връща)
- `guards.tsx` — `RequireAuth` (+ пренасочване към смяна на парола), `RequireAnonymous`
- Екрани: Вход, Забравена парола, Нова парола (по линк с `?email=&token=`),
  Смяна на парола
- `ChangePasswordPage` прекратява сесията и връща към входа — сървърът така или иначе
  е убил токените

**Backend (по заявка)**
- Добавен CORS — виж секция 4

**Проверено**
- `npx tsc -b` — чисто
- `npm run build` — успешен (398 kB JS / 125 kB gzip)
- Vite сървърът отдава приложението; proxy-то препраща `/api` коректно
- `dotnet build` на backend-а след CORS промяната — 0 грешки, 0 предупреждения

**НЕ е проверено**
- Реален вход от край до край — на машината няма работещ PostgreSQL, така че backend-ът
  не е стартиран
- Визуален преглед в браузър — browser tooling-ът не беше достъпен в сесията

---

## 7. Какво предстои

Модули, по ред на зависимост (всеки е отделна стъпка с обновяване на този файл):

1. **Членове и отдели** — `/members`, `/departments`; списък с филтри и пагинация,
   профил, редакция на собствен профил + снимка, администриране (само `canManageMembers`)
2. **Задачи** — `/tasks`; списък, Kanban board (`/tasks/board`), детайл, коментари,
   документи (качване/сваляне), смяна на статус
3. **Календар** — `/events`; изгледи ден/седмица/месец/списък, износ `.ics`
4. **Дежурства** — `/duty-records`; месечна норма, лична справка
5. **Бюджет** — `/budget`; разходи и годишна справка (`canManageBudget`)
6. **Известия** — `/notifications`; център за известия, брояч непрочетени

Извън обхвата на уеб клиента: `/devices` (push токени — само за мобилните приложения).

### Отворени въпроси

- Деплой: къде се хоства frontend-ът и кой origin да влезе в `Cors:Origins` за production
- Дали да се пази `refreshToken` в `localStorage` (сегашно решение) или в cookie —
  при XSS `localStorage` е достъпен; backend-ът в момента не поддържа cookie-based auth

---

## 8. Локална среда — проверено на 2026-08-11

PostgreSQL 16 инсталиран през Homebrew (`brew services start postgresql@16`), база
`studentcouncil`, собственик = потребителят на macOS, без парола за локални връзки.
User secrets са зададени за `StudentCouncil.Api`.

**Тествано от край до край през Vite proxy-то (`localhost:5173/api/...`):**

| Проверка | Резултат |
|---|---|
| `POST /auth/login` | 200, `mustChangePassword: true` |
| `must_change_password` claim в access токена | присъства → `jwt.ts` подходът е верен |
| `GET /auth/me` при вдигнат гейт | 200 (заобикаля гейта), връща 6-те права |
| `GET /members` при вдигнат гейт | **403 `password_change_required`** |
| `POST /auth/change-password` | 204 |
| Стар access токен след смяна | **401** — security stamp е ротиран |
| Стар refresh токен след смяна | **401** — всички са анулирани |
| Вход с новата парола | `mustChangePassword: false` |

Тоест решението `ChangePasswordPage` да прекратява сесията и да връща към входа е
единственото коректно — сървърът вече е убил токените.

Акаунтът е върнат в първоначално състояние (парола `Test1234`, гейтът вдигнат отново
чрез `UPDATE "AspNetUsers" SET must_change_password = true`).

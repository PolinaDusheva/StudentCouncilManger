# Design System Reskin Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reskin the StudentCouncilManager frontend from the current blue Tailwind `brand-*` theme to the warm pink-accent design system defined in `docs/superpowers/specs/2026-08-19-design-system-reskin-design.md`, touching every shared UI primitive, the app shell, and every route file, with no behavior changes.

**Architecture:** Tailwind v4 CSS-first `@theme` in `frontend/src/index.css` gets a new semantic color/typography token set. Every `components/ui/*` primitive is restyled to consume those tokens (no prop/API changes). A new `Card` primitive replaces the duplicated `rounded-xl bg-white p-* shadow-sm ring-1 ring-slate-200` pattern found across ~9 route files. Every route file then gets its literal `brand-*`/`slate-*`/`red-*` Tailwind classes swapped for the new tokens.

**Tech Stack:** React 19, Tailwind CSS v4 (`@tailwindcss/vite`, CSS-first `@theme`, no `tailwind.config.js`), `clsx` + `tailwind-merge` via `cn()`, TypeScript, Vite.

**Verification approach:** This is a pure visual reskin — no new logic, so there is nothing to unit-test. Each task's "test" step is `npm run build` (`tsc -b && vite build`) from `frontend/`, which catches TypeScript/JSX errors (a broken `cn()` call, a bad prop) even though it cannot catch a wrong Tailwind class name (Tailwind classes are untyped strings). The final task adds a grep-based check that zero `brand-` references remain, plus a full visual pass through the running app with the preview tools.

---

## File map

**Modify (tokens):**
- `frontend/src/index.css`

**Modify (primitives, `frontend/src/components/ui/`):**
- `Button.tsx`, `Input.tsx`, `Select.tsx`, `Badge.tsx`, `Alert.tsx`, `Avatar.tsx`, `EmptyState.tsx`, `Pagination.tsx`, `Table.tsx`, `Modal.tsx`

**Create:**
- `frontend/src/components/ui/Card.tsx`

**Modify (shell, `frontend/src/components/layout/`):**
- `AppShell.tsx`, `AuthLayout.tsx`

**Modify (routes):**
- `routes/dashboard/DashboardPage.tsx`
- `routes/budget/BudgetPage.tsx`
- `routes/events/CalendarPage.tsx`, `MonthGrid.tsx`, `EventAgenda.tsx`, `EventDetailPage.tsx`, `eventLabels.ts`
- `routes/members/MembersPage.tsx`, `MemberDetailPage.tsx`, `MyProfilePage.tsx`
- `routes/departments/DepartmentsPage.tsx`, `DepartmentDetailPage.tsx`
- `routes/notifications/NotificationBell.tsx`
- `routes/auth/LoginPage.tsx`
- `routes/tasks/TaskCard.tsx`, `TaskBoardPage.tsx`, `TasksPage.tsx`, `TaskDetailPage.tsx`, `TaskFormPage.tsx`, `TaskFilters.tsx`, `TaskStatusMenu.tsx`, `TaskComments.tsx`, `TaskDocuments.tsx`

All edits are class-string swaps (old Tailwind literal → new token-based literal). No component prop, function signature, or route behavior changes anywhere in this plan.

---

### Task 1: Design tokens

**Files:**
- Modify: `frontend/src/index.css`

- [ ] **Step 1: Replace the `@theme` block and base layer**

Replace the entire current contents of `frontend/src/index.css` with:

```css
@import 'tailwindcss';

@theme {
  /* Neutrals */
  --color-ink: #1c1c1c;
  --color-ink-soft: #333333;
  --color-muted: #777777;
  --color-faint: #aaa4a4;

  --color-page: #f4f0ee;
  --color-surface: #ffffff;
  --color-subtle: #fbf9f8;
  --color-row-hover: #fdf9fa;
  --color-line: #ece7e7;
  --color-divider: #eeeaea;
  --color-border: #e6e0e0;

  /* Accent */
  --color-accent: #ff3c70;
  --color-accent-hover: #e0295a;
  --color-accent-soft: #ffe5ea;
  --color-accent-soft-text: #c72552;

  /* Data / info blue — not the primary color */
  --color-data: #4a90e2;

  /* Danger (buttons, destructive text) — distinct from the lighter `tone-danger-*` badge/alert tints */
  --color-danger: #dc3545;
  --color-danger-hover: #b52b39;

  /* Badge / Alert tone triples */
  --color-tone-neutral-bg: #f4f0ee;
  --color-tone-neutral-text: #555555;
  --color-tone-neutral-border: #eae4e4;

  --color-tone-success-bg: #f7f9f7;
  --color-tone-success-text: #1e7a34;
  --color-tone-success-border: #e3ece5;
  --color-tone-success-alert-text: #1b5f2a;

  --color-tone-warning-bg: #fdfaf3;
  --color-tone-warning-text: #96690a;
  --color-tone-warning-border: #f0e6cf;
  --color-tone-warning-alert-text: #7a5508;

  --color-tone-danger-bg: #fdf6f7;
  --color-tone-danger-text: #b52b39;
  --color-tone-danger-border: #f4dfe2;
  --color-tone-danger-alert-text: #8e2028;

  --color-tone-info-bg: #f5f8fc;
  --color-tone-info-text: #2f6cb0;
  --color-tone-info-border: #dbe6f3;
  --color-tone-info-alert-text: #2a5b91;

  --font-serif: Georgia, 'Times New Roman', serif;
}

@layer base {
  html {
    -webkit-text-size-adjust: 100%;
  }

  body {
    @apply bg-page text-ink-soft antialiased;
  }

  /* Keyboard focus is always visible; pointer clicks do not draw a ring. */
  :focus-visible {
    @apply outline-accent outline-2 outline-offset-2;
  }
}

@layer utilities {
  /* Thin decorative strip — AppShell top bar, section underlines. Used sparingly by design. */
  .gradient-accent-bar {
    background: linear-gradient(
      90deg,
      rgba(137, 58, 180, 0.55) 0%,
      rgba(253, 29, 29, 0.69) 51%,
      rgba(252, 176, 69, 0.82) 100%
    );
  }

  /* Saturated mark gradient — logo squares, the signed-in member's own avatar ring. */
  .gradient-accent-mark {
    background: linear-gradient(135deg, #893ab4 0%, #fd1d1d 55%, #fcb045 100%);
  }
}
```

- [ ] **Step 2: Verify the build still typechecks (Tailwind class errors won't surface here, but a malformed CSS file will fail Vite)**

Run: `cd frontend && npm run build`
Expected: PASS (the rest of the app still references the now-removed `brand-*` classes, so this build is expected to **still succeed** — Tailwind v4 doesn't fail the build on an unknown utility class, it just emits no rule for it. Visual breakage from the stale `brand-*` references is fixed in the following tasks, not this one.)

- [ ] **Step 3: Commit**

```bash
cd frontend && git add src/index.css
git commit -m "style: replace brand color theme with warm pink-accent design tokens"
```

---

### Task 2: Button + Pagination

**Files:**
- Modify: `frontend/src/components/ui/Button.tsx`
- Modify: `frontend/src/components/ui/Pagination.tsx`

- [ ] **Step 1: Rewrite Button's variants, sizes and base classes**

In `frontend/src/components/ui/Button.tsx`, replace:

```tsx
const VARIANTS: Record<Variant, string> = {
  primary: 'bg-brand-600 text-white hover:bg-brand-700 disabled:hover:bg-brand-600',
  secondary:
    'bg-white text-slate-900 ring-1 ring-slate-300 ring-inset hover:bg-slate-50 disabled:hover:bg-white',
  ghost: 'text-slate-700 hover:bg-slate-100 disabled:hover:bg-transparent',
  danger: 'bg-red-600 text-white hover:bg-red-700 disabled:hover:bg-red-600',
}

const SIZES: Record<Size, string> = {
  sm: 'h-8 px-3 text-sm',
  md: 'h-10 px-4 text-sm',
}
```

with:

```tsx
const VARIANTS: Record<Variant, string> = {
  primary:
    'bg-ink text-white hover:bg-accent hover:shadow-[0_5px_15px_rgba(255,60,112,0.3)] disabled:hover:bg-ink disabled:hover:shadow-none',
  secondary:
    'bg-transparent text-ink ring-2 ring-border ring-inset hover:ring-accent hover:text-accent disabled:hover:ring-border disabled:hover:text-ink',
  ghost: 'text-muted hover:bg-page hover:text-ink disabled:hover:bg-transparent disabled:hover:text-muted',
  danger: 'bg-danger text-white hover:bg-danger-hover disabled:hover:bg-danger',
}

const SIZES: Record<Size, string> = {
  sm: 'h-9 px-4 text-sm',
  md: 'h-11 px-5 text-sm',
}
```

Then update the base classes in the returned `<button>`, replacing:

```tsx
        'inline-flex items-center justify-center gap-2 rounded-lg font-medium transition-colors',
```

with:

```tsx
        'inline-flex items-center justify-center gap-2 rounded-full font-semibold transition-all',
```

- [ ] **Step 2: Make Pagination's prev/next buttons circular**

In `frontend/src/components/ui/Pagination.tsx`, replace both occurrences of:

```tsx
        <Button
          variant="secondary"
          size="sm"
          onClick={() => onPageChange(page - 1)}
          disabled={page <= 1}
          aria-label="Предишна страница"
        >
          <ChevronLeft aria-hidden className="size-4" />
        </Button>
```

and

```tsx
        <Button
          variant="secondary"
          size="sm"
          onClick={() => onPageChange(page + 1)}
          disabled={page >= totalPages}
          aria-label="Следваща страница"
        >
          <ChevronRight aria-hidden className="size-4" />
        </Button>
```

with (add `className="size-9 p-0"` — `cn()` puts `className` last, so it overrides `SIZES.sm`'s `px-4` and forces an equal-sided circle):

```tsx
        <Button
          variant="secondary"
          size="sm"
          className="size-9 p-0"
          onClick={() => onPageChange(page - 1)}
          disabled={page <= 1}
          aria-label="Предишна страница"
        >
          <ChevronLeft aria-hidden className="size-4" />
        </Button>
```

```tsx
        <Button
          variant="secondary"
          size="sm"
          className="size-9 p-0"
          onClick={() => onPageChange(page + 1)}
          disabled={page >= totalPages}
          aria-label="Следваща страница"
        >
          <ChevronRight aria-hidden className="size-4" />
        </Button>
```

Also replace both `text-sm text-slate-600` occurrences (the "X – Y от Z" and "page / totalPages" text) with `text-sm text-muted`.

- [ ] **Step 3: Verify**

Run: `cd frontend && npm run build`
Expected: PASS

- [ ] **Step 4: Commit**

```bash
cd frontend && git add src/components/ui/Button.tsx src/components/ui/Pagination.tsx
git commit -m "style: reskin Button and Pagination to pill shape and new tokens"
```

---

### Task 3: Input + Select

**Files:**
- Modify: `frontend/src/components/ui/Input.tsx`
- Modify: `frontend/src/components/ui/Select.tsx`

- [ ] **Step 1: Reskin Input**

In `frontend/src/components/ui/Input.tsx`, replace:

```tsx
      <label htmlFor={id} className="block text-sm font-medium text-slate-700">
        {label}
      </label>

      <input
        ref={ref}
        id={id}
        aria-invalid={error ? true : undefined}
        aria-describedby={describedBy}
        className={cn(
          'block w-full rounded-lg border-0 px-3 py-2 text-sm text-slate-900 shadow-sm',
          'ring-1 ring-slate-300 ring-inset placeholder:text-slate-400',
          'focus:ring-brand-500 focus:ring-2 focus:ring-inset focus:outline-none',
          'disabled:cursor-not-allowed disabled:bg-slate-50 disabled:text-slate-500',
          error && 'ring-red-500 focus:ring-red-500',
          className,
        )}
        {...props}
      />

      {error ? (
        <p id={errorId} role="alert" className="text-sm text-red-600">
          {error}
        </p>
      ) : hint ? (
        <p id={hintId} className="text-sm text-slate-500">
          {hint}
        </p>
      ) : null}
```

with:

```tsx
      <label htmlFor={id} className="block text-sm font-semibold text-ink-soft">
        {label}
      </label>

      <input
        ref={ref}
        id={id}
        aria-invalid={error ? true : undefined}
        aria-describedby={describedBy}
        className={cn(
          'block w-full rounded-xl border-2 border-line bg-surface px-4 py-3 text-sm text-ink',
          'shadow-[inset_0_2px_5px_rgba(0,0,0,0.05)] placeholder:text-faint',
          'focus:border-accent focus:shadow-[0_0_0_4px_rgba(255,60,112,0.15)] focus:outline-none',
          'disabled:cursor-not-allowed disabled:bg-subtle disabled:text-faint',
          error && 'border-danger focus:border-danger focus:shadow-none',
          className,
        )}
        {...props}
      />

      {error ? (
        <p id={errorId} role="alert" className="text-sm text-danger">
          {error}
        </p>
      ) : hint ? (
        <p id={hintId} className="text-sm text-muted">
          {hint}
        </p>
      ) : null}
```

- [ ] **Step 2: Reskin Select**

In `frontend/src/components/ui/Select.tsx`, replace:

```tsx
      <label htmlFor={id} className="block text-sm font-medium text-slate-700">
        {label}
      </label>

      <select
        ref={ref}
        id={id}
        aria-invalid={error ? true : undefined}
        aria-describedby={describedBy}
        className={cn(
          'block w-full rounded-lg border-0 bg-white px-3 py-2 text-sm text-slate-900 shadow-sm',
          'ring-1 ring-slate-300 ring-inset',
          'focus:ring-brand-500 focus:ring-2 focus:ring-inset focus:outline-none',
          'disabled:cursor-not-allowed disabled:bg-slate-50 disabled:text-slate-500',
          error && 'ring-red-500 focus:ring-red-500',
          className,
        )}
        {...props}
      >
```

with:

```tsx
      <label htmlFor={id} className="block text-sm font-semibold text-ink-soft">
        {label}
      </label>

      <select
        ref={ref}
        id={id}
        aria-invalid={error ? true : undefined}
        aria-describedby={describedBy}
        className={cn(
          'block w-full rounded-xl border-2 border-line bg-surface px-4 py-3 text-sm text-ink',
          'focus:border-accent focus:shadow-[0_0_0_4px_rgba(255,60,112,0.15)] focus:outline-none',
          'disabled:cursor-not-allowed disabled:bg-subtle disabled:text-faint',
          error && 'border-danger focus:border-danger focus:shadow-none',
          className,
        )}
        {...props}
      >
```

Then, further down in the same file, replace:

```tsx
      {error ? (
        <p id={errorId} role="alert" className="text-sm text-red-600">
          {error}
        </p>
      ) : hint ? (
        <p id={hintId} className="text-sm text-slate-500">
          {hint}
        </p>
      ) : null}
```

with:

```tsx
      {error ? (
        <p id={errorId} role="alert" className="text-sm text-danger">
          {error}
        </p>
      ) : hint ? (
        <p id={hintId} className="text-sm text-muted">
          {hint}
        </p>
      ) : null}
```

- [ ] **Step 3: Verify**

Run: `cd frontend && npm run build`
Expected: PASS

- [ ] **Step 4: Commit**

```bash
cd frontend && git add src/components/ui/Input.tsx src/components/ui/Select.tsx
git commit -m "style: reskin Input and Select with new border and focus tokens"
```

---

### Task 4: Badge + Alert

**Files:**
- Modify: `frontend/src/components/ui/Badge.tsx`
- Modify: `frontend/src/components/ui/Alert.tsx`

- [ ] **Step 1: Reskin Badge**

In `frontend/src/components/ui/Badge.tsx`, replace:

```tsx
const TONES: Record<BadgeTone, string> = {
  neutral: 'bg-slate-100 text-slate-700 ring-slate-200',
  success: 'bg-green-50 text-green-700 ring-green-200',
  warning: 'bg-amber-50 text-amber-800 ring-amber-200',
  danger: 'bg-red-50 text-red-700 ring-red-200',
  info: 'bg-blue-50 text-blue-700 ring-blue-200',
}
```

with:

```tsx
const TONES: Record<BadgeTone, string> = {
  neutral: 'bg-tone-neutral-bg text-tone-neutral-text ring-tone-neutral-border',
  success: 'bg-tone-success-bg text-tone-success-text ring-tone-success-border',
  warning: 'bg-tone-warning-bg text-tone-warning-text ring-tone-warning-border',
  danger: 'bg-tone-danger-bg text-tone-danger-text ring-tone-danger-border',
  info: 'bg-tone-info-bg text-tone-info-text ring-tone-info-border',
}
```

Then replace:

```tsx
        'inline-flex items-center rounded-md px-2 py-0.5 text-xs font-medium ring-1 ring-inset',
```

with:

```tsx
        'inline-flex items-center rounded-lg px-2 py-0.5 text-xs font-semibold ring-1 ring-inset',
```

- [ ] **Step 2: Reskin Alert**

In `frontend/src/components/ui/Alert.tsx`, replace:

```tsx
const TONES: Record<Tone, { box: string; icon: typeof Info }> = {
  error: { box: 'bg-red-50 text-red-800 ring-red-200', icon: AlertCircle },
  // For outcomes that succeeded but need attention — e.g. an event saved with schedule overlaps.
  warning: { box: 'bg-amber-50 text-amber-900 ring-amber-200', icon: AlertTriangle },
  success: { box: 'bg-green-50 text-green-800 ring-green-200', icon: CheckCircle2 },
  info: { box: 'bg-blue-50 text-blue-800 ring-blue-200', icon: Info },
}
```

with:

```tsx
const TONES: Record<Tone, { box: string; icon: typeof Info }> = {
  error: { box: 'bg-tone-danger-bg text-tone-danger-alert-text ring-tone-danger-border', icon: AlertCircle },
  // For outcomes that succeeded but need attention — e.g. an event saved with schedule overlaps.
  warning: {
    box: 'bg-tone-warning-bg text-tone-warning-alert-text ring-tone-warning-border',
    icon: AlertTriangle,
  },
  success: { box: 'bg-tone-success-bg text-tone-success-alert-text ring-tone-success-border', icon: CheckCircle2 },
  info: { box: 'bg-tone-info-bg text-tone-info-alert-text ring-tone-info-border', icon: Info },
}
```

Then replace:

```tsx
      className={cn('flex gap-2.5 rounded-lg px-3.5 py-3 text-sm ring-1 ring-inset', box, className)}
```

with:

```tsx
      className={cn('flex gap-2.5 rounded-[15px] px-3.5 py-3 text-sm ring-1 ring-inset', box, className)}
```

- [ ] **Step 3: Verify**

Run: `cd frontend && npm run build`
Expected: PASS

- [ ] **Step 4: Commit**

```bash
cd frontend && git add src/components/ui/Badge.tsx src/components/ui/Alert.tsx
git commit -m "style: reskin Badge and Alert tone colors to warm palette"
```

---

### Task 5: Avatar

**Files:**
- Modify: `frontend/src/components/ui/Avatar.tsx`

- [ ] **Step 1: Recolor initials fallback and add the gradient-ring variant**

Replace the entire contents of `frontend/src/components/ui/Avatar.tsx` with:

```tsx
import { useAuthenticatedImage } from '@/lib/hooks/useAuthenticatedImage'
import { cn } from '@/lib/utils/cn'

type Size = 'sm' | 'md' | 'lg'

const SIZES: Record<Size, string> = {
  sm: 'size-8 text-xs',
  md: 'size-10 text-sm',
  lg: 'size-20 text-xl',
}

interface AvatarProps {
  /** The API's `photoUrl` (a relative path), or null when the member has no photo. */
  photoUrl: string | null | undefined
  fullName: string
  size?: Size
  className?: string
  /** Gradient ring, reserved for the signed-in member's own avatar in the app header. */
  ring?: boolean
}

/** First letters of the first two words, e.g. "Иван Петров" → "ИП". */
function initials(fullName: string): string {
  return fullName
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((word) => word[0]?.toUpperCase() ?? '')
    .join('')
}

/**
 * Member photo, falling back to initials.
 *
 * The image is loaded through {@link useAuthenticatedImage} because the photo endpoint
 * requires a bearer token. Decorative for assistive tech: an avatar is always rendered next
 * to the member's name, so announcing it again would just repeat that name.
 */
export function Avatar({ photoUrl, fullName, size = 'md', className, ring = false }: AvatarProps) {
  const objectUrl = useAuthenticatedImage(photoUrl)

  const content = objectUrl ? (
    <img src={objectUrl} alt="" className="size-full object-cover" />
  ) : (
    initials(fullName)
  )

  if (ring) {
    return (
      <span aria-hidden className={cn('gradient-accent-mark inline-flex shrink-0 rounded-full p-[3px]', className)}>
        <span
          className={cn(
            'flex items-center justify-center overflow-hidden rounded-full',
            'bg-surface text-ink font-medium select-none',
            SIZES[size],
          )}
        >
          {content}
        </span>
      </span>
    )
  }

  return (
    <span
      aria-hidden
      className={cn(
        'inline-flex shrink-0 items-center justify-center overflow-hidden rounded-full',
        'bg-accent-soft text-accent-soft-text font-medium select-none',
        SIZES[size],
        className,
      )}
    >
      {content}
    </span>
  )
}
```

- [ ] **Step 2: Verify**

Run: `cd frontend && npm run build`
Expected: PASS

- [ ] **Step 3: Commit**

```bash
cd frontend && git add src/components/ui/Avatar.tsx
git commit -m "style: reskin Avatar initials and add gradient-ring variant"
```

---

### Task 6: EmptyState + Table

**Files:**
- Modify: `frontend/src/components/ui/EmptyState.tsx`
- Modify: `frontend/src/components/ui/Table.tsx`

- [ ] **Step 1: Reskin EmptyState**

In `frontend/src/components/ui/EmptyState.tsx`, replace:

```tsx
      <Inbox aria-hidden className="mb-3 size-8 text-slate-300" />
      <p className="text-sm font-medium text-slate-900">{title}</p>
      {description && <p className="mt-1 max-w-sm text-sm text-slate-500">{description}</p>}
```

with:

```tsx
      <Inbox aria-hidden className="mb-3 size-8 text-faint" />
      <p className="text-sm font-semibold text-ink">{title}</p>
      {description && <p className="mt-1 max-w-sm text-sm text-muted">{description}</p>}
```

- [ ] **Step 2: Reskin Table**

In `frontend/src/components/ui/Table.tsx`, replace both occurrences of:

```tsx
      <div className="rounded-xl bg-white ring-1 ring-slate-200">
```

with:

```tsx
      <div className="rounded-[15px] bg-surface ring-1 ring-divider">
```

Then replace:

```tsx
    <div className="overflow-x-auto rounded-xl bg-white ring-1 ring-slate-200">
      <table className="w-full text-left text-sm">
        <thead className="border-b border-slate-200 text-xs text-slate-500">
```

with:

```tsx
    <div className="overflow-x-auto rounded-[15px] bg-surface ring-1 ring-divider">
      <table className="w-full text-left text-sm">
        <thead className="border-b border-divider bg-subtle text-xs font-semibold text-muted uppercase tracking-wide">
```

Then replace:

```tsx
                      className="inline-flex items-center gap-1 hover:text-slate-900"
```

with:

```tsx
                      className="inline-flex items-center gap-1 hover:text-ink"
```

Then replace:

```tsx
        <tbody className="divide-y divide-slate-100">
          {rows.map((row) => (
            <tr
              key={rowKey(row)}
              onClick={onRowClick ? () => onRowClick(row) : undefined}
              className={cn(onRowClick && 'cursor-pointer hover:bg-slate-50')}
            >
```

with:

```tsx
        <tbody className="divide-y divide-divider">
          {rows.map((row) => (
            <tr
              key={rowKey(row)}
              onClick={onRowClick ? () => onRowClick(row) : undefined}
              className={cn(onRowClick && 'cursor-pointer hover:bg-row-hover')}
            >
```

- [ ] **Step 3: Verify**

Run: `cd frontend && npm run build`
Expected: PASS

- [ ] **Step 4: Commit**

```bash
cd frontend && git add src/components/ui/EmptyState.tsx src/components/ui/Table.tsx
git commit -m "style: reskin EmptyState and Table to new radius, tokens and row hover"
```

---

### Task 7: Modal

**Files:**
- Modify: `frontend/src/components/ui/Modal.tsx`

- [ ] **Step 1: Reskin the dialog shell**

Replace:

```tsx
      className={cn(
        'w-[calc(100vw-2rem)] max-w-lg rounded-xl bg-white p-0 shadow-xl',
        'backdrop:bg-slate-900/40',
        // `m-auto` is required, not cosmetic: the UA stylesheet centres a modal dialog with
        // `margin: auto`, and Tailwind's preflight resets every margin to 0 — without this
        // the dialog sticks to the top-left corner.
        'm-auto border-0 text-slate-900',
        className,
      )}
```

with:

```tsx
      className={cn(
        'w-[calc(100vw-2rem)] max-w-lg rounded-[20px] bg-surface p-0',
        'shadow-[0_15px_50px_rgba(0,0,0,0.15),0_2px_6px_rgba(0,0,0,0.1)]',
        'backdrop:bg-ink/40',
        // `m-auto` is required, not cosmetic: the UA stylesheet centres a modal dialog with
        // `margin: auto`, and Tailwind's preflight resets every margin to 0 — without this
        // the dialog sticks to the top-left corner.
        'm-auto border-0 text-ink-soft',
        className,
      )}
```

- [ ] **Step 2: Reskin header, title and close button**

Replace:

```tsx
      <div className="flex items-start justify-between gap-4 border-b border-slate-200 px-5 py-4">
        <h2 className="text-base font-semibold">{title}</h2>
        <button
          type="button"
          onClick={onClose}
          aria-label="Затвори"
          className="-m-1 rounded-md p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-600"
        >
          <X aria-hidden className="size-4" />
        </button>
      </div>
```

with:

```tsx
      <div className="flex items-start justify-between gap-4 border-b border-divider px-5 py-4">
        <h2 className="font-serif text-[19px] font-normal text-ink">{title}</h2>
        <button
          type="button"
          onClick={onClose}
          aria-label="Затвори"
          className="-m-1 rounded-md p-1 text-faint hover:bg-page hover:text-ink-soft"
        >
          <X aria-hidden className="size-4" />
        </button>
      </div>
```

- [ ] **Step 3: Reskin the footer border**

Replace:

```tsx
      {footer && (
        <div className="flex justify-end gap-2 border-t border-slate-200 px-5 py-4">{footer}</div>
      )}
```

with:

```tsx
      {footer && (
        <div className="flex justify-end gap-2 border-t border-divider px-5 py-4">{footer}</div>
      )}
```

- [ ] **Step 4: Verify**

Run: `cd frontend && npm run build`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
cd frontend && git add src/components/ui/Modal.tsx
git commit -m "style: reskin Modal with new radius, shadow and Georgia title"
```

---

### Task 8: New Card primitive

**Files:**
- Create: `frontend/src/components/ui/Card.tsx`

- [ ] **Step 1: Create the Card component**

Create `frontend/src/components/ui/Card.tsx`:

```tsx
import type { HTMLAttributes, ReactNode } from 'react'

import { cn } from '@/lib/utils/cn'

interface CardProps extends HTMLAttributes<HTMLDivElement> {
  children: ReactNode
  /** `panel` uses the larger 20px radius and roomier padding for single-section detail screens. */
  variant?: 'default' | 'panel'
}

/**
 * Shared white surface for page sections. Replaces the `rounded-xl bg-white p-* shadow-sm
 * ring-1 ring-slate-200` combination that used to be hand-rolled on nearly every route.
 */
export function Card({ children, variant = 'default', className, ...props }: CardProps) {
  return (
    <div
      className={cn(
        'bg-surface shadow-[0_4px_15px_rgba(0,0,0,0.05)] ring-1 ring-divider',
        variant === 'panel' ? 'rounded-[20px] p-6' : 'rounded-[15px] p-5',
        className,
      )}
      {...props}
    >
      {children}
    </div>
  )
}
```

- [ ] **Step 2: Verify**

Run: `cd frontend && npm run build`
Expected: PASS

- [ ] **Step 3: Commit**

```bash
cd frontend && git add src/components/ui/Card.tsx
git commit -m "feat: add shared Card primitive"
```

---

### Task 9: AppShell

**Files:**
- Modify: `frontend/src/components/layout/AppShell.tsx`

- [ ] **Step 1: Add the gradient top bar, gradient logo mark, and reskin header/nav**

Replace:

```tsx
  return (
    <div className="min-h-dvh">
      <header className="border-b border-slate-200 bg-white">
        <div className="mx-auto flex h-14 max-w-6xl items-center gap-3 px-4">
          <span className="bg-brand-600 flex size-8 items-center justify-center rounded-lg">
            <Users aria-hidden className="size-4 text-white" />
          </span>
          <span className="hidden font-semibold text-slate-900 sm:inline">Студентски съвет</span>

          <div className="ml-auto flex items-center gap-2">
            <NotificationBell />

            <NavLink
              to="/profile"
              className={({ isActive }) =>
                cn(
                  'flex items-center gap-2 rounded-lg px-2 py-1.5 hover:bg-slate-100',
                  isActive && 'bg-slate-100',
                )
              }
            >
              {/* `/auth/me` carries no photo URL, so the header always shows initials. */}
              {user && <Avatar photoUrl={null} fullName={user.fullName} size="sm" />}
              <span className="hidden text-right sm:block">
                <span className="block text-sm font-medium text-slate-900">{user?.fullName}</span>
                <span className="block text-xs text-slate-500">
                  {roleLabel}
                  {departmentLabel && ` · ${departmentLabel}`}
                </span>
              </span>
            </NavLink>

            <Button variant="secondary" size="sm" onClick={() => void signOut()}>
              <LogOut aria-hidden className="size-4" />
              <span className="hidden sm:inline">Изход</span>
            </Button>
          </div>
        </div>

        <nav aria-label="Основна навигация" className="mx-auto max-w-6xl px-4">
          <ul className="-mb-px flex gap-1">
            {NAV_ITEMS.map(({ to, label, icon: Icon, end }) => (
              <li key={to}>
                <NavLink
                  to={to}
                  end={end}
                  className={({ isActive }) =>
                    cn(
                      'flex items-center gap-2 border-b-2 px-3 py-2.5 text-sm font-medium transition-colors',
                      isActive
                        ? 'border-brand-600 text-brand-700'
                        : 'border-transparent text-slate-600 hover:border-slate-300 hover:text-slate-900',
                    )
                  }
                >
                  <Icon aria-hidden className="size-4" />
                  {label}
                </NavLink>
              </li>
            ))}
          </ul>
        </nav>
      </header>

      <main className="mx-auto max-w-6xl px-4 py-8">
        <Outlet />
      </main>
    </div>
  )
```

with:

```tsx
  return (
    <div className="min-h-dvh">
      <div className="gradient-accent-bar h-1.5" />

      <header className="border-b border-divider bg-surface">
        <div className="mx-auto flex h-14 max-w-6xl items-center gap-3 px-4">
          <span className="gradient-accent-mark flex size-8 items-center justify-center rounded-xl">
            <Users aria-hidden className="size-4 text-white" />
          </span>
          <span className="hidden font-serif text-lg text-ink sm:inline">Студентски съвет</span>

          <div className="ml-auto flex items-center gap-2">
            <NotificationBell />

            <NavLink
              to="/profile"
              className={({ isActive }) =>
                cn(
                  'flex items-center gap-2 rounded-lg px-2 py-1.5 hover:bg-page',
                  isActive && 'bg-page',
                )
              }
            >
              {/* `/auth/me` carries no photo URL, so the header always shows initials. */}
              {user && <Avatar photoUrl={null} fullName={user.fullName} size="sm" ring />}
              <span className="hidden text-right sm:block">
                <span className="block text-sm font-medium text-ink">{user?.fullName}</span>
                <span className="block text-xs text-muted">
                  {roleLabel}
                  {departmentLabel && ` · ${departmentLabel}`}
                </span>
              </span>
            </NavLink>

            <Button variant="secondary" size="sm" onClick={() => void signOut()}>
              <LogOut aria-hidden className="size-4" />
              <span className="hidden sm:inline">Изход</span>
            </Button>
          </div>
        </div>

        <nav aria-label="Основна навигация" className="mx-auto max-w-6xl px-4">
          <ul className="-mb-px flex gap-1">
            {NAV_ITEMS.map(({ to, label, icon: Icon, end }) => (
              <li key={to}>
                <NavLink
                  to={to}
                  end={end}
                  className={({ isActive }) =>
                    cn(
                      'flex items-center gap-2 border-b-2 px-3 py-2.5 text-sm font-medium transition-colors',
                      isActive
                        ? 'border-accent text-ink'
                        : 'border-transparent text-muted hover:border-border hover:text-ink',
                    )
                  }
                >
                  <Icon aria-hidden className="size-4" />
                  {label}
                </NavLink>
              </li>
            ))}
          </ul>
        </nav>
      </header>

      <main className="mx-auto max-w-6xl px-4 py-8">
        <Outlet />
      </main>
    </div>
  )
```

- [ ] **Step 2: Verify**

Run: `cd frontend && npm run build`
Expected: PASS

- [ ] **Step 3: Commit**

```bash
cd frontend && git add src/components/layout/AppShell.tsx
git commit -m "style: reskin AppShell with gradient bar, logo mark and accent nav"
```

---

### Task 10: AuthLayout + LoginPage

**Files:**
- Modify: `frontend/src/components/layout/AuthLayout.tsx`
- Modify: `frontend/src/routes/auth/LoginPage.tsx`

- [ ] **Step 1: Reskin AuthLayout to use Card and the gradient logo mark**

Replace the entire contents of `frontend/src/components/layout/AuthLayout.tsx` with:

```tsx
import type { ReactNode } from 'react'
import { Users } from 'lucide-react'

import { Card } from '@/components/ui/Card'

interface AuthLayoutProps {
  title: string
  subtitle?: ReactNode
  children: ReactNode
  /** Secondary links rendered under the card (e.g. "back to sign in"). */
  footer?: ReactNode
}

/** Centred card used by every unauthenticated screen. */
export function AuthLayout({ title, subtitle, children, footer }: AuthLayoutProps) {
  return (
    <div className="flex min-h-dvh flex-col items-center justify-center px-4 py-12">
      <div className="w-full max-w-sm">
        <div className="mb-6 flex flex-col items-center text-center">
          <span className="gradient-accent-mark mb-4 flex size-11 items-center justify-center rounded-2xl">
            <Users aria-hidden className="size-6 text-white" />
          </span>
          <h1 className="font-serif text-xl font-normal text-ink">{title}</h1>
          {subtitle && <p className="mt-1.5 text-sm text-muted">{subtitle}</p>}
        </div>

        <Card variant="panel">{children}</Card>

        {footer && <div className="mt-5 text-center text-sm text-muted">{footer}</div>}
      </div>
    </div>
  )
}
```

- [ ] **Step 2: Reskin LoginPage's forgot-password link**

In `frontend/src/routes/auth/LoginPage.tsx`, replace:

```tsx
        <Link to="/forgot-password" className="text-brand-700 font-medium hover:underline">
```

with:

```tsx
        <Link to="/forgot-password" className="text-accent hover:text-accent-hover font-medium hover:underline">
```

- [ ] **Step 3: Verify**

Run: `cd frontend && npm run build`
Expected: PASS

- [ ] **Step 4: Commit**

```bash
cd frontend && git add src/components/layout/AuthLayout.tsx src/routes/auth/LoginPage.tsx
git commit -m "style: reskin AuthLayout and LoginPage link color"
```

---

### Task 11: DashboardPage

**Files:**
- Modify: `frontend/src/routes/dashboard/DashboardPage.tsx`

- [ ] **Step 1: Swap the two hand-rolled cards for `Card`, recolor text and status dots**

Replace:

```tsx
import { Alert } from '@/components/ui/Alert'
```

with:

```tsx
import { Alert } from '@/components/ui/Alert'
import { Card } from '@/components/ui/Card'
```

Replace:

```tsx
      <div>
        <h1 className="text-2xl font-semibold text-slate-900">Здравей, {user.fullName.split(' ')[0]}!</h1>
        <p className="mt-1 text-sm text-slate-600">
          Влезе успешно. Функционалните модули предстоят.
        </p>
      </div>

      <section className="rounded-xl bg-white p-5 shadow-sm ring-1 ring-slate-200">
        <h2 className="mb-4 text-sm font-semibold text-slate-900">Профил</h2>
```

with:

```tsx
      <div>
        <h1 className="font-serif text-[34px] leading-[1.15] font-normal text-ink">
          Здравей, {user.fullName.split(' ')[0]}!
        </h1>
        <p className="mt-1 text-sm text-muted">Влезе успешно. Функционалните модули предстоят.</p>
      </div>

      <Card>
        <h2 className="mb-4 font-serif text-[22px] leading-[1.3] font-normal text-ink">Профил</h2>
```

Replace the closing `</section>` right after the `<dl>...</dl>` block (the first one) with `</Card>`, i.e. replace:

```tsx
        </dl>
      </section>

      <section className="rounded-xl bg-white p-5 shadow-sm ring-1 ring-slate-200">
        <h2 className="mb-1 text-sm font-semibold text-slate-900">Права</h2>
        <p className="mb-4 text-sm text-slate-600">
          Изчислени от сървъра според ролята. Определят кои действия ще се показват в интерфейса.
        </p>
```

with:

```tsx
        </dl>
      </Card>

      <Card>
        <h2 className="mb-1 font-serif text-[22px] leading-[1.3] font-normal text-ink">Права</h2>
        <p className="mb-4 text-sm text-muted">
          Изчислени от сървъра според ролята. Определят кои действия ще се показват в интерфейса.
        </p>
```

Replace:

```tsx
              <span
                aria-hidden
                className={
                  permissions[key]
                    ? 'size-1.5 shrink-0 rounded-full bg-green-600'
                    : 'size-1.5 shrink-0 rounded-full bg-slate-300'
                }
              />
              <span className={permissions[key] ? 'text-slate-900' : 'text-slate-400'}>
```

with:

```tsx
              <span
                aria-hidden
                className={
                  permissions[key]
                    ? 'size-1.5 shrink-0 rounded-full bg-tone-success-text'
                    : 'size-1.5 shrink-0 rounded-full bg-border'
                }
              />
              <span className={permissions[key] ? 'text-ink' : 'text-faint'}>
```

Replace the closing tag of the second section — find:

```tsx
        </ul>
      </section>

      <Alert tone="info">
```

with:

```tsx
        </ul>
      </Card>

      <Alert tone="info">
```

Finally, in the `Field` helper at the bottom, replace:

```tsx
function Field({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <dt className="text-slate-500">{label}</dt>
      <dd className="mt-0.5 font-medium text-slate-900">{value}</dd>
    </div>
  )
}
```

with:

```tsx
function Field({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <dt className="text-muted">{label}</dt>
      <dd className="mt-0.5 font-medium text-ink">{value}</dd>
    </div>
  )
}
```

- [ ] **Step 2: Verify**

Run: `cd frontend && npm run build`
Expected: PASS

- [ ] **Step 3: Commit**

```bash
cd frontend && git add src/routes/dashboard/DashboardPage.tsx
git commit -m "style: reskin DashboardPage with Card and new tokens"
```

---

### Task 12: BudgetPage

**Files:**
- Modify: `frontend/src/routes/budget/BudgetPage.tsx`

- [ ] **Step 1: Swap the summary tile for `Card`, recolor the icon badge, title and delete button**

Replace:

```tsx
import { Alert } from '@/components/ui/Alert'
import { Button } from '@/components/ui/Button'
import { ConfirmDialog } from '@/components/ui/ConfirmDialog'
```

with:

```tsx
import { Alert } from '@/components/ui/Alert'
import { Button } from '@/components/ui/Button'
import { Card } from '@/components/ui/Card'
import { ConfirmDialog } from '@/components/ui/ConfirmDialog'
```

Replace:

```tsx
            className="text-red-600 hover:bg-red-50"
```

with:

```tsx
            className="text-danger hover:bg-tone-danger-bg"
```

Replace:

```tsx
        <h1 className="text-2xl font-semibold text-slate-900">Бюджет</h1>
```

with:

```tsx
        <h1 className="font-serif text-[34px] leading-[1.15] font-normal text-ink">Бюджет</h1>
```

Replace:

```tsx
        <div className="flex items-center gap-3 rounded-xl bg-white px-5 py-4 shadow-sm ring-1 ring-slate-200">
          <span className="bg-brand-50 flex size-10 items-center justify-center rounded-lg">
            <Wallet aria-hidden className="text-brand-600 size-5" />
          </span>
          <div>
            <p className="text-xs text-slate-500">Общо за {year}</p>
            <p className="text-xl font-semibold text-slate-900">
              {summary.isPending ? '…' : formatEur(summary.data?.totalEur ?? 0)}
            </p>
          </div>
        </div>
```

with:

```tsx
        <Card className="flex items-center gap-3 px-5 py-4">
          <span className="bg-accent-soft flex size-10 items-center justify-center rounded-lg">
            <Wallet aria-hidden className="text-accent-soft-text size-5" />
          </span>
          <div>
            <p className="text-xs text-muted">Общо за {year}</p>
            <p className="text-xl font-semibold text-ink">
              {summary.isPending ? '…' : formatEur(summary.data?.totalEur ?? 0)}
            </p>
          </div>
        </Card>
```

- [ ] **Step 2: Verify**

Run: `cd frontend && npm run build`
Expected: PASS

- [ ] **Step 3: Commit**

```bash
cd frontend && git add src/routes/budget/BudgetPage.tsx
git commit -m "style: reskin BudgetPage summary tile and delete action"
```

---

### Task 13: Calendar (CalendarPage, MonthGrid, EventAgenda, eventLabels)

**Files:**
- Modify: `frontend/src/routes/events/CalendarPage.tsx`
- Modify: `frontend/src/routes/events/MonthGrid.tsx`
- Modify: `frontend/src/routes/events/EventAgenda.tsx`
- Modify: `frontend/src/routes/events/eventLabels.ts`

- [ ] **Step 1: Reskin CalendarPage's title and view-switcher**

In `frontend/src/routes/events/CalendarPage.tsx`, replace:

```tsx
        <h1 className="text-2xl font-semibold text-slate-900">Календар</h1>
```

with:

```tsx
        <h1 className="font-serif text-[34px] leading-[1.15] font-normal text-ink">Календар</h1>
```

Replace:

```tsx
          <span className="ml-1 text-sm font-medium text-slate-900 first-letter:uppercase">
            {periodLabel}
          </span>
        </div>

        <div role="tablist" aria-label="Изглед" className="flex gap-1 rounded-lg bg-slate-100 p-1">
          {(Object.keys(VIEW_LABELS) as CalendarView[]).map((candidate) => (
            <button
              key={candidate}
              type="button"
              role="tab"
              aria-selected={view === candidate}
              onClick={() => setView(candidate)}
              className={cn(
                'rounded-md px-3 py-1 text-sm font-medium transition-colors',
                view === candidate
                  ? 'bg-white text-slate-900 shadow-sm'
                  : 'text-slate-600 hover:text-slate-900',
              )}
            >
```

with:

```tsx
          <span className="ml-1 text-sm font-medium text-ink first-letter:uppercase">
            {periodLabel}
          </span>
        </div>

        <div role="tablist" aria-label="Изглед" className="flex gap-1 rounded-lg bg-page p-1">
          {(Object.keys(VIEW_LABELS) as CalendarView[]).map((candidate) => (
            <button
              key={candidate}
              type="button"
              role="tab"
              aria-selected={view === candidate}
              onClick={() => setView(candidate)}
              className={cn(
                'rounded-md px-3 py-1 text-sm font-medium transition-colors',
                view === candidate ? 'bg-surface text-ink shadow-sm' : 'text-muted hover:text-ink',
              )}
            >
```

Replace:

```tsx
      <p className="flex items-center gap-1.5 text-xs text-slate-500">
```

with:

```tsx
      <p className="flex items-center gap-1.5 text-xs text-muted">
```

- [ ] **Step 2: Reskin MonthGrid**

In `frontend/src/routes/events/MonthGrid.tsx`, replace:

```tsx
    <div className="overflow-hidden rounded-xl bg-white ring-1 ring-slate-200">
      <div className="grid grid-cols-7 border-b border-slate-200 bg-slate-50">
        {WEEKDAY_LABELS.map((label) => (
          <div key={label} className="px-2 py-2 text-center text-xs font-medium text-slate-500">
```

with:

```tsx
    <div className="overflow-hidden rounded-[15px] bg-surface ring-1 ring-divider">
      <div className="grid grid-cols-7 border-b border-divider bg-subtle">
        {WEEKDAY_LABELS.map((label) => (
          <div key={label} className="px-2 py-2 text-center text-xs font-medium text-muted">
```

Replace:

```tsx
    <div
      className={cn(
        'min-h-24 border-r border-b border-slate-100 p-1.5',
        !day.inMonth && 'bg-slate-50/60',
      )}
    >
      <span
        className={cn(
          'inline-flex size-6 items-center justify-center rounded-full text-xs',
          day.isToday && 'bg-brand-600 font-semibold text-white',
          !day.isToday && day.inMonth && 'text-slate-700',
          !day.isToday && !day.inMonth && 'text-slate-400',
        )}
      >
        {day.date.getDate()}
      </span>

      <ul className="mt-1 space-y-0.5">
        {visible.map((entry) => (
          <li key={`${entry.id}-${entry.occurrenceStartUtc ?? entry.startUtc}`}>
            <EntryLine entry={entry} />
          </li>
        ))}
      </ul>

      {hidden > 0 && <p className="mt-0.5 px-1 text-xs text-slate-500">+{hidden} още</p>}
```

with:

```tsx
    <div
      className={cn(
        'min-h-24 border-r border-b border-divider p-1.5',
        !day.inMonth && 'bg-subtle/60',
      )}
    >
      <span
        className={cn(
          'inline-flex size-6 items-center justify-center rounded-full text-xs',
          day.isToday && 'bg-accent font-semibold text-white',
          !day.isToday && day.inMonth && 'text-ink-soft',
          !day.isToday && !day.inMonth && 'text-faint',
        )}
      >
        {day.date.getDate()}
      </span>

      <ul className="mt-1 space-y-0.5">
        {visible.map((entry) => (
          <li key={`${entry.id}-${entry.occurrenceStartUtc ?? entry.startUtc}`}>
            <EntryLine entry={entry} />
          </li>
        ))}
      </ul>

      {hidden > 0 && <p className="mt-0.5 px-1 text-xs text-muted">+{hidden} още</p>}
```

Replace:

```tsx
      className="flex items-center gap-1 rounded px-1 py-0.5 text-xs hover:bg-slate-100"
    >
      <span aria-hidden className={cn('size-1.5 shrink-0 rounded-full', EVENT_TYPE_DOTS[entry.type])} />
      <span className="shrink-0 text-slate-500">{TIME.format(start)}</span>
      <span className="truncate text-slate-900">{entry.title}</span>
```

with:

```tsx
      className="flex items-center gap-1 rounded px-1 py-0.5 text-xs hover:bg-page"
    >
      <span aria-hidden className={cn('size-1.5 shrink-0 rounded-full', EVENT_TYPE_DOTS[entry.type])} />
      <span className="shrink-0 text-muted">{TIME.format(start)}</span>
      <span className="truncate text-ink">{entry.title}</span>
```

- [ ] **Step 3: Reskin EventAgenda**

In `frontend/src/routes/events/EventAgenda.tsx`, replace:

```tsx
      <div className="rounded-xl bg-white ring-1 ring-slate-200">
        <EmptyState title="Няма събития в този период" />
      </div>
```

with:

```tsx
      <div className="rounded-[15px] bg-surface ring-1 ring-divider">
        <EmptyState title="Няма събития в този период" />
      </div>
```

Replace:

```tsx
          <h2 className="mb-2 text-sm font-semibold text-slate-900 first-letter:uppercase">
            {DAY_HEADING.format(new Date(`${dayKey}T12:00:00`))}
          </h2>

          <ul className="divide-y divide-slate-100 overflow-hidden rounded-xl bg-white ring-1 ring-slate-200">
```

with:

```tsx
          <h2 className="mb-2 text-sm font-semibold text-ink first-letter:uppercase">
            {DAY_HEADING.format(new Date(`${dayKey}T12:00:00`))}
          </h2>

          <ul className="divide-y divide-divider overflow-hidden rounded-[15px] bg-surface ring-1 ring-divider">
```

Replace:

```tsx
    <Link to={to} className="flex gap-4 px-4 py-3 hover:bg-slate-50">
      <span className="w-24 shrink-0 text-sm text-slate-500">
        {TIME.format(start)} – {TIME.format(end)}
      </span>

      <span className="min-w-0 flex-1">
        <span className="block text-sm font-medium text-slate-900">{entry.title}</span>

        <span className="mt-1 flex flex-wrap items-center gap-2 text-xs text-slate-500">
```

with:

```tsx
    <Link to={to} className="flex gap-4 px-4 py-3 hover:bg-row-hover">
      <span className="w-24 shrink-0 text-sm text-muted">
        {TIME.format(start)} – {TIME.format(end)}
      </span>

      <span className="min-w-0 flex-1">
        <span className="block text-sm font-medium text-ink">{entry.title}</span>

        <span className="mt-1 flex flex-wrap items-center gap-2 text-xs text-muted">
```

Replace:

```tsx
          {entry.isDeadline && <span className="text-red-600">краен срок на задача</span>}
```

with:

```tsx
          {entry.isDeadline && <span className="text-danger">краен срок на задача</span>}
```

- [ ] **Step 4: Recolor the event-type dots**

In `frontend/src/routes/events/eventLabels.ts`, replace:

```ts
export const EVENT_TYPE_DOTS: Record<EventType, string> = {
  Meeting: 'bg-blue-500',
  PublicEvent: 'bg-green-500',
  InternalMeeting: 'bg-slate-400',
  SportsEvent: 'bg-amber-500',
  Deadline: 'bg-red-500',
  Other: 'bg-slate-400',
}
```

with:

```ts
export const EVENT_TYPE_DOTS: Record<EventType, string> = {
  Meeting: 'bg-data',
  PublicEvent: 'bg-tone-success-text',
  InternalMeeting: 'bg-faint',
  SportsEvent: 'bg-tone-warning-text',
  Deadline: 'bg-danger',
  Other: 'bg-faint',
}
```

- [ ] **Step 5: Verify**

Run: `cd frontend && npm run build`
Expected: PASS

- [ ] **Step 6: Commit**

```bash
cd frontend && git add src/routes/events/CalendarPage.tsx src/routes/events/MonthGrid.tsx src/routes/events/EventAgenda.tsx src/routes/events/eventLabels.ts
git commit -m "style: reskin calendar views and event-type dots"
```

---

### Task 14: EventDetailPage

**Files:**
- Modify: `frontend/src/routes/events/EventDetailPage.tsx`

- [ ] **Step 1: Swap both cards for `Card`, recolor text and the section title**

Replace:

```tsx
import { Alert } from '@/components/ui/Alert'
import { Avatar } from '@/components/ui/Avatar'
import { Badge } from '@/components/ui/Badge'
import { Button } from '@/components/ui/Button'
```

with:

```tsx
import { Alert } from '@/components/ui/Alert'
import { Avatar } from '@/components/ui/Avatar'
import { Badge } from '@/components/ui/Badge'
import { Button } from '@/components/ui/Button'
import { Card } from '@/components/ui/Card'
```

Replace:

```tsx
      <div className="rounded-xl bg-white p-6 shadow-sm ring-1 ring-slate-200">
        <div className="flex flex-wrap items-start justify-between gap-3">
          <h1 className="text-xl font-semibold text-slate-900">{event.title}</h1>
```

with:

```tsx
      <Card variant="panel">
        <div className="flex flex-wrap items-start justify-between gap-3">
          <h1 className="font-serif text-2xl font-normal text-ink">{event.title}</h1>
```

Replace:

```tsx
        {event.description && (
          <p className="mt-5 border-t border-slate-200 pt-5 text-sm whitespace-pre-wrap text-slate-700">
            {event.description}
          </p>
        )}

        <dl className="mt-5 grid gap-x-6 gap-y-4 border-t border-slate-200 pt-5 text-sm sm:grid-cols-2">
          <Field label="Начало" value={formatDateTime(event.startUtc)} />
          <Field label="Край" value={formatDateTime(event.endUtc)} />
          {event.location && (
            <div>
              <dt className="text-slate-500">Място</dt>
              <dd className="mt-0.5 flex items-center gap-1.5 font-medium text-slate-900">
                <MapPin aria-hidden className="size-4 text-slate-400" />
                {event.location}
              </dd>
            </div>
          )}
          {event.organizer && <Field label="Организатор" value={event.organizer.fullName} />}
        </dl>
      </div>

      <section className="rounded-xl bg-white p-6 shadow-sm ring-1 ring-slate-200">
        <h2 className="mb-4 text-sm font-semibold text-slate-900">
          Участници ({event.participants.length})
        </h2>

        {event.participants.length === 0 ? (
          <p className="text-sm text-slate-500">Няма записани участници.</p>
        ) : (
          <ul className="grid gap-3 sm:grid-cols-2">
            {event.participants.map((participant) => (
              <li key={participant.id}>
                <MemberLine member={participant} />
              </li>
            ))}
          </ul>
        )}
      </section>
```

with:

```tsx
        {event.description && (
          <p className="mt-5 border-t border-divider pt-5 text-sm whitespace-pre-wrap text-ink-soft">
            {event.description}
          </p>
        )}

        <dl className="mt-5 grid gap-x-6 gap-y-4 border-t border-divider pt-5 text-sm sm:grid-cols-2">
          <Field label="Начало" value={formatDateTime(event.startUtc)} />
          <Field label="Край" value={formatDateTime(event.endUtc)} />
          {event.location && (
            <div>
              <dt className="text-muted">Място</dt>
              <dd className="mt-0.5 flex items-center gap-1.5 font-medium text-ink">
                <MapPin aria-hidden className="size-4 text-faint" />
                {event.location}
              </dd>
            </div>
          )}
          {event.organizer && <Field label="Организатор" value={event.organizer.fullName} />}
        </dl>
      </Card>

      <Card variant="panel">
        <h2 className="mb-4 font-serif text-[22px] leading-[1.3] font-normal text-ink">
          Участници ({event.participants.length})
        </h2>

        {event.participants.length === 0 ? (
          <p className="text-sm text-muted">Няма записани участници.</p>
        ) : (
          <ul className="grid gap-3 sm:grid-cols-2">
            {event.participants.map((participant) => (
              <li key={participant.id}>
                <MemberLine member={participant} />
              </li>
            ))}
          </ul>
        )}
      </Card>
```

Replace:

```tsx
      className="inline-flex items-center gap-1.5 text-sm text-slate-600 hover:text-slate-900"
    >
      <ArrowLeft aria-hidden className="size-4" />
      Календар
```

with:

```tsx
      className="inline-flex items-center gap-1.5 text-sm text-muted hover:text-ink"
    >
      <ArrowLeft aria-hidden className="size-4" />
      Календар
```

Replace:

```tsx
function Field({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <dt className="text-slate-500">{label}</dt>
      <dd className="mt-0.5 font-medium text-slate-900">{value}</dd>
    </div>
  )
}

function MemberLine({ member }: { member: MemberSummaryDto }) {
  return (
    <Link to={`/members/${member.id}`} className="group flex min-w-0 items-center gap-2.5">
      <Avatar photoUrl={member.photoUrl} fullName={member.fullName} size="sm" />
      <span className="group-hover:text-brand-700 truncate text-sm font-medium text-slate-900 group-hover:underline">
        {member.fullName}
      </span>
    </Link>
  )
}
```

with:

```tsx
function Field({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <dt className="text-muted">{label}</dt>
      <dd className="mt-0.5 font-medium text-ink">{value}</dd>
    </div>
  )
}

function MemberLine({ member }: { member: MemberSummaryDto }) {
  return (
    <Link to={`/members/${member.id}`} className="group flex min-w-0 items-center gap-2.5">
      <Avatar photoUrl={member.photoUrl} fullName={member.fullName} size="sm" />
      <span className="group-hover:text-accent-hover truncate text-sm font-medium text-ink group-hover:underline">
        {member.fullName}
      </span>
    </Link>
  )
}
```

- [ ] **Step 2: Verify**

Run: `cd frontend && npm run build`
Expected: PASS

- [ ] **Step 3: Commit**

```bash
cd frontend && git add src/routes/events/EventDetailPage.tsx
git commit -m "style: reskin EventDetailPage with Card and new tokens"
```

---

### Task 15: Members (MembersPage, MemberDetailPage, MyProfilePage)

**Files:**
- Modify: `frontend/src/routes/members/MembersPage.tsx`
- Modify: `frontend/src/routes/members/MemberDetailPage.tsx`
- Modify: `frontend/src/routes/members/MyProfilePage.tsx`

- [ ] **Step 1: Reskin MembersPage**

In `frontend/src/routes/members/MembersPage.tsx`, replace:

```tsx
            className="hover:text-brand-700 font-medium hover:underline"
```

with:

```tsx
            className="hover:text-accent-hover font-medium hover:underline"
```

Replace:

```tsx
          <span className="text-slate-400">Организационно ниво</span>
```

with:

```tsx
          <span className="text-faint">Организационно ниво</span>
```

Replace:

```tsx
          <h1 className="text-2xl font-semibold text-slate-900">Членове</h1>
          {data && (
            <p className="mt-1 text-sm text-slate-600">
```

with:

```tsx
          <h1 className="font-serif text-[34px] leading-[1.15] font-normal text-ink">Членове</h1>
          {data && (
            <p className="mt-1 text-sm text-muted">
```

- [ ] **Step 2: Reskin MemberDetailPage**

In `frontend/src/routes/members/MemberDetailPage.tsx`, replace:

```tsx
import { Alert } from '@/components/ui/Alert'
import { Avatar } from '@/components/ui/Avatar'
import { Badge } from '@/components/ui/Badge'
import { Button } from '@/components/ui/Button'
import { ConfirmDialog } from '@/components/ui/ConfirmDialog'
```

with:

```tsx
import { Alert } from '@/components/ui/Alert'
import { Avatar } from '@/components/ui/Avatar'
import { Badge } from '@/components/ui/Badge'
import { Button } from '@/components/ui/Button'
import { Card } from '@/components/ui/Card'
import { ConfirmDialog } from '@/components/ui/ConfirmDialog'
```

Replace:

```tsx
      <Link
        to="/members"
        className="inline-flex items-center gap-1.5 text-sm text-slate-600 hover:text-slate-900"
      >
        <ArrowLeft aria-hidden className="size-4" />
        Всички членове
      </Link>

      <div className="rounded-xl bg-white p-6 shadow-sm ring-1 ring-slate-200">
        <div className="flex flex-wrap items-start gap-5">
          <Avatar photoUrl={member.photoUrl} fullName={member.fullName} size="lg" />

          <div className="min-w-0 flex-1">
            <h1 className="text-xl font-semibold text-slate-900">{member.fullName}</h1>
```

with:

```tsx
      <Link
        to="/members"
        className="inline-flex items-center gap-1.5 text-sm text-muted hover:text-ink"
      >
        <ArrowLeft aria-hidden className="size-4" />
        Всички членове
      </Link>

      <Card variant="panel">
        <div className="flex flex-wrap items-start gap-5">
          <Avatar photoUrl={member.photoUrl} fullName={member.fullName} size="lg" />

          <div className="min-w-0 flex-1">
            <h1 className="font-serif text-2xl font-normal text-ink">{member.fullName}</h1>
```

Replace:

```tsx
        <dl className="mt-6 grid gap-x-6 gap-y-4 border-t border-slate-200 pt-6 text-sm sm:grid-cols-2">
          <Field label="Имейл" value={member.email} />
          <Field label="Телефон" value={member.phoneNumber ?? '—'} />
          <Field
            label="Отдел"
            value={
              member.department
                ? (member.departmentName ?? DEPARTMENT_CODE_LABELS[member.department])
                : 'Организационно ниво'
            }
          />
          <Field label="Присъединен на" value={formatDate(member.joinedOn)} />
        </dl>
      </div>
```

with:

```tsx
        <dl className="mt-6 grid gap-x-6 gap-y-4 border-t border-divider pt-6 text-sm sm:grid-cols-2">
          <Field label="Имейл" value={member.email} />
          <Field label="Телефон" value={member.phoneNumber ?? '—'} />
          <Field
            label="Отдел"
            value={
              member.department
                ? (member.departmentName ?? DEPARTMENT_CODE_LABELS[member.department])
                : 'Организационно ниво'
            }
          />
          <Field label="Присъединен на" value={formatDate(member.joinedOn)} />
        </dl>
      </Card>
```

Replace:

```tsx
function Field({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <dt className="text-slate-500">{label}</dt>
      <dd className="mt-0.5 font-medium text-slate-900">{value}</dd>
    </div>
  )
}
```

with:

```tsx
function Field({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <dt className="text-muted">{label}</dt>
      <dd className="mt-0.5 font-medium text-ink">{value}</dd>
    </div>
  )
}
```

- [ ] **Step 3: Reskin MyProfilePage**

In `frontend/src/routes/members/MyProfilePage.tsx`, replace:

```tsx
import { Alert } from '@/components/ui/Alert'
import { Avatar } from '@/components/ui/Avatar'
import { Badge } from '@/components/ui/Badge'
import { Button } from '@/components/ui/Button'
import { Input } from '@/components/ui/Input'
```

with:

```tsx
import { Alert } from '@/components/ui/Alert'
import { Avatar } from '@/components/ui/Avatar'
import { Badge } from '@/components/ui/Badge'
import { Button } from '@/components/ui/Button'
import { Card } from '@/components/ui/Card'
import { Input } from '@/components/ui/Input'
```

Replace:

```tsx
      <h1 className="text-2xl font-semibold text-slate-900">Моят профил</h1>

      <section className="rounded-xl bg-white p-6 shadow-sm ring-1 ring-slate-200">
        <div className="flex items-center gap-5">
          <Avatar photoUrl={profile.photoUrl} fullName={profile.fullName} size="lg" />

          <div className="min-w-0">
            <p className="font-medium text-slate-900">{profile.fullName}</p>
            <p className="mt-0.5 text-sm text-slate-500">{profile.email}</p>
```

with:

```tsx
      <h1 className="font-serif text-[34px] leading-[1.15] font-normal text-ink">Моят профил</h1>

      <Card variant="panel">
        <div className="flex items-center gap-5">
          <Avatar photoUrl={profile.photoUrl} fullName={profile.fullName} size="lg" />

          <div className="min-w-0">
            <p className="font-medium text-ink">{profile.fullName}</p>
            <p className="mt-0.5 text-sm text-muted">{profile.email}</p>
```

Replace:

```tsx
        {savePhoto.isError && (
          <Alert tone="error" className="mt-4">
            {errorMessage(savePhoto.error)}
          </Alert>
        )}
      </section>

      <section className="rounded-xl bg-white p-6 shadow-sm ring-1 ring-slate-200">
        <h2 className="mb-4 text-sm font-semibold text-slate-900">Данни</h2>

        <dl className="mb-5 grid gap-x-6 gap-y-3 text-sm sm:grid-cols-2">
          <div>
            <dt className="text-slate-500">Роля</dt>
            <dd className="mt-1">
              <Badge tone={roleTone(profile.role)}>{SYSTEM_ROLE_LABELS[profile.role]}</Badge>
            </dd>
          </div>
          <div>
            <dt className="text-slate-500">Отдел</dt>
            <dd className="mt-0.5 font-medium text-slate-900">
              {profile.department
                ? (profile.departmentName ?? DEPARTMENT_CODE_LABELS[profile.department])
                : 'Организационно ниво'}
            </dd>
          </div>
          <div>
            <dt className="text-slate-500">Присъединен на</dt>
            <dd className="mt-0.5 font-medium text-slate-900">{formatDate(profile.joinedOn)}</dd>
          </div>
        </dl>

        <form
          onSubmit={handleSubmit((values) => saveProfile.mutate(values))}
          noValidate
          className="space-y-4 border-t border-slate-200 pt-5"
        >
```

with:

```tsx
        {savePhoto.isError && (
          <Alert tone="error" className="mt-4">
            {errorMessage(savePhoto.error)}
          </Alert>
        )}
      </Card>

      <Card variant="panel">
        <h2 className="mb-4 font-serif text-[22px] leading-[1.3] font-normal text-ink">Данни</h2>

        <dl className="mb-5 grid gap-x-6 gap-y-3 text-sm sm:grid-cols-2">
          <div>
            <dt className="text-muted">Роля</dt>
            <dd className="mt-1">
              <Badge tone={roleTone(profile.role)}>{SYSTEM_ROLE_LABELS[profile.role]}</Badge>
            </dd>
          </div>
          <div>
            <dt className="text-muted">Отдел</dt>
            <dd className="mt-0.5 font-medium text-ink">
              {profile.department
                ? (profile.departmentName ?? DEPARTMENT_CODE_LABELS[profile.department])
                : 'Организационно ниво'}
            </dd>
          </div>
          <div>
            <dt className="text-muted">Присъединен на</dt>
            <dd className="mt-0.5 font-medium text-ink">{formatDate(profile.joinedOn)}</dd>
          </div>
        </dl>

        <form
          onSubmit={handleSubmit((values) => saveProfile.mutate(values))}
          noValidate
          className="space-y-4 border-t border-divider pt-5"
        >
```

Replace the final closing tag — find:

```tsx
          </div>
        </form>
      </section>
    </div>
  )
}
```

with:

```tsx
          </div>
        </form>
      </Card>
    </div>
  )
}
```

- [ ] **Step 4: Verify**

Run: `cd frontend && npm run build`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
cd frontend && git add src/routes/members/MembersPage.tsx src/routes/members/MemberDetailPage.tsx src/routes/members/MyProfilePage.tsx
git commit -m "style: reskin member pages with Card and new tokens"
```

---

### Task 16: Departments (DepartmentsPage, DepartmentDetailPage)

**Files:**
- Modify: `frontend/src/routes/departments/DepartmentsPage.tsx`
- Modify: `frontend/src/routes/departments/DepartmentDetailPage.tsx`

- [ ] **Step 1: Reskin DepartmentsPage**

In `frontend/src/routes/departments/DepartmentsPage.tsx`, replace:

```tsx
      <h1 className="text-2xl font-semibold text-slate-900">Отдели</h1>
```

with:

```tsx
      <h1 className="font-serif text-[34px] leading-[1.15] font-normal text-ink">Отдели</h1>
```

Replace:

```tsx
      className="hover:ring-brand-300 block rounded-xl bg-white p-5 shadow-sm ring-1 ring-slate-200 transition-shadow hover:shadow-md"
    >
      <h2 className="font-semibold text-slate-900">{department.name}</h2>

      {department.description && (
        <p className="mt-1 line-clamp-2 text-sm text-slate-600">{department.description}</p>
      )}

      <div className="mt-4 flex items-center gap-4 text-sm text-slate-500">
```

with:

```tsx
      className="block rounded-[15px] bg-surface p-5 shadow-[0_4px_15px_rgba(0,0,0,0.05)] ring-1 ring-divider transition-shadow hover:shadow-md hover:ring-accent"
    >
      <h2 className="font-semibold text-ink">{department.name}</h2>

      {department.description && (
        <p className="mt-1 line-clamp-2 text-sm text-muted">{department.description}</p>
      )}

      <div className="mt-4 flex items-center gap-4 text-sm text-muted">
```

- [ ] **Step 2: Reskin DepartmentDetailPage**

In `frontend/src/routes/departments/DepartmentDetailPage.tsx`, replace:

```tsx
import { Alert } from '@/components/ui/Alert'
import { Avatar } from '@/components/ui/Avatar'
import { Badge } from '@/components/ui/Badge'
import { EmptyState } from '@/components/ui/EmptyState'
```

with:

```tsx
import { Alert } from '@/components/ui/Alert'
import { Avatar } from '@/components/ui/Avatar'
import { Badge } from '@/components/ui/Badge'
import { Card } from '@/components/ui/Card'
import { EmptyState } from '@/components/ui/EmptyState'
```

Replace:

```tsx
      <Link
        to="/departments"
        className="inline-flex items-center gap-1.5 text-sm text-slate-600 hover:text-slate-900"
      >
        <ArrowLeft aria-hidden className="size-4" />
        Всички отдели
      </Link>

      <div>
        <h1 className="text-2xl font-semibold text-slate-900">{department.name}</h1>
        {department.description && (
          <p className="mt-1 text-sm text-slate-600">{department.description}</p>
        )}
      </div>

      {leadership.length > 0 && (
        <section className="rounded-xl bg-white p-5 shadow-sm ring-1 ring-slate-200">
          <h2 className="mb-4 text-sm font-semibold text-slate-900">Ръководство</h2>
          <ul className="grid gap-4 sm:grid-cols-3">
            {leadership.map(({ title, member }) => (
              <li key={title}>
                <p className="mb-2 text-xs text-slate-500">{title}</p>
                <MemberLine member={member!} />
              </li>
            ))}
          </ul>
        </section>
      )}

      <section className="rounded-xl bg-white p-5 shadow-sm ring-1 ring-slate-200">
        <h2 className="mb-4 text-sm font-semibold text-slate-900">
          Състав ({department.memberCount})
        </h2>

        {department.members.length === 0 ? (
          <EmptyState title="Няма членове в този отдел" />
        ) : (
          <ul className="divide-y divide-slate-100">
            {department.members.map((member) => (
              <li key={member.id} className="flex items-center justify-between gap-3 py-3">
                <MemberLine member={member} />
                <Badge tone={roleTone(member.role)}>{SYSTEM_ROLE_LABELS[member.role]}</Badge>
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  )
}

function MemberLine({ member }: { member: MemberSummaryDto }) {
  return (
    <Link to={`/members/${member.id}`} className="group flex min-w-0 items-center gap-2.5">
      <Avatar photoUrl={member.photoUrl} fullName={member.fullName} size="sm" />
      <span className="group-hover:text-brand-700 truncate text-sm font-medium text-slate-900 group-hover:underline">
        {member.fullName}
      </span>
    </Link>
  )
}
```

with:

```tsx
      <Link
        to="/departments"
        className="inline-flex items-center gap-1.5 text-sm text-muted hover:text-ink"
      >
        <ArrowLeft aria-hidden className="size-4" />
        Всички отдели
      </Link>

      <div>
        <h1 className="font-serif text-[34px] leading-[1.15] font-normal text-ink">{department.name}</h1>
        {department.description && <p className="mt-1 text-sm text-muted">{department.description}</p>}
      </div>

      {leadership.length > 0 && (
        <Card>
          <h2 className="mb-4 font-serif text-[22px] leading-[1.3] font-normal text-ink">Ръководство</h2>
          <ul className="grid gap-4 sm:grid-cols-3">
            {leadership.map(({ title, member }) => (
              <li key={title}>
                <p className="mb-2 text-xs text-muted">{title}</p>
                <MemberLine member={member!} />
              </li>
            ))}
          </ul>
        </Card>
      )}

      <Card>
        <h2 className="mb-4 font-serif text-[22px] leading-[1.3] font-normal text-ink">
          Състав ({department.memberCount})
        </h2>

        {department.members.length === 0 ? (
          <EmptyState title="Няма членове в този отдел" />
        ) : (
          <ul className="divide-y divide-divider">
            {department.members.map((member) => (
              <li key={member.id} className="flex items-center justify-between gap-3 py-3">
                <MemberLine member={member} />
                <Badge tone={roleTone(member.role)}>{SYSTEM_ROLE_LABELS[member.role]}</Badge>
              </li>
            ))}
          </ul>
        )}
      </Card>
    </div>
  )
}

function MemberLine({ member }: { member: MemberSummaryDto }) {
  return (
    <Link to={`/members/${member.id}`} className="group flex min-w-0 items-center gap-2.5">
      <Avatar photoUrl={member.photoUrl} fullName={member.fullName} size="sm" />
      <span className="group-hover:text-accent-hover truncate text-sm font-medium text-ink group-hover:underline">
        {member.fullName}
      </span>
    </Link>
  )
}
```

- [ ] **Step 3: Verify**

Run: `cd frontend && npm run build`
Expected: PASS

- [ ] **Step 4: Commit**

```bash
cd frontend && git add src/routes/departments/DepartmentsPage.tsx src/routes/departments/DepartmentDetailPage.tsx
git commit -m "style: reskin department pages with Card and new tokens"
```

---

### Task 17: NotificationBell

**Files:**
- Modify: `frontend/src/routes/notifications/NotificationBell.tsx`

- [ ] **Step 1: Reskin the bell button, unread pill, dropdown panel and rows**

Replace:

```tsx
        className="relative rounded-lg p-2 text-slate-500 hover:bg-slate-100 hover:text-slate-700"
      >
        <Bell aria-hidden className="size-5" />
        {count > 0 && (
          <span
            aria-hidden
            className="absolute top-1 right-1 flex size-4 items-center justify-center rounded-full bg-red-500 text-[10px] font-medium text-white"
          >
```

with:

```tsx
        className="relative rounded-lg p-2 text-muted hover:bg-page hover:text-ink-soft"
      >
        <Bell aria-hidden className="size-5" />
        {count > 0 && (
          <span
            aria-hidden
            className="absolute top-1 right-1 flex size-4 items-center justify-center rounded-full bg-accent text-[10px] font-medium text-white"
          >
```

Replace:

```tsx
          className="absolute right-0 z-10 mt-2 w-80 rounded-lg bg-white shadow-lg ring-1 ring-slate-200"
        >
          <div className="flex items-center justify-between border-b border-slate-100 px-3 py-2">
            <span className="text-sm font-semibold text-slate-900">Известия</span>
            {count > 0 && (
              <button
                type="button"
                onClick={() => markAllRead.mutate()}
                disabled={markAllRead.isPending}
                className="text-brand-600 hover:text-brand-700 inline-flex items-center gap-1 text-xs font-medium disabled:opacity-50"
              >
```

with:

```tsx
          className="absolute right-0 z-10 mt-2 w-80 rounded-[15px] bg-surface shadow-[0_10px_30px_rgba(0,0,0,0.15)] ring-1 ring-divider"
        >
          <div className="flex items-center justify-between border-b border-divider px-3 py-2">
            <span className="text-sm font-semibold text-ink">Известия</span>
            {count > 0 && (
              <button
                type="button"
                onClick={() => markAllRead.mutate()}
                disabled={markAllRead.isPending}
                className="text-accent hover:text-accent-hover inline-flex items-center gap-1 text-xs font-medium disabled:opacity-50"
              >
```

Replace:

```tsx
            ) : list.data.items.length === 0 ? (
              <p className="px-3 py-6 text-center text-sm text-slate-500">Няма известия.</p>
            ) : (
              <ul className="divide-y divide-slate-100">
```

with:

```tsx
            ) : list.data.items.length === 0 ? (
              <p className="px-3 py-6 text-center text-sm text-muted">Няма известия.</p>
            ) : (
              <ul className="divide-y divide-divider">
```

Replace:

```tsx
      className={cn(
        'flex w-full gap-2.5 px-3 py-2.5 text-left hover:bg-slate-50',
        !navigable && 'cursor-default',
      )}
    >
      <span
        aria-hidden
        className={cn(
          'mt-1.5 size-1.5 shrink-0 rounded-full',
          notification.isRead ? 'bg-transparent' : 'bg-brand-600',
        )}
      />
      <span className="min-w-0 flex-1">
        <span className="block text-xs text-slate-500">
          {NOTIFICATION_TYPE_LABELS[notification.type] ?? notification.type}
        </span>
        <span
          className={cn(
            'block text-sm text-slate-900',
            notification.isRead ? 'font-normal' : 'font-medium',
          )}
        >
          {notification.title}
        </span>
        <span className="mt-0.5 block line-clamp-2 text-xs text-slate-600">{notification.body}</span>
        <span className="mt-0.5 block text-xs text-slate-400">
          {formatDateTime(notification.createdAtUtc)}
        </span>
      </span>
    </button>
```

with:

```tsx
      className={cn(
        'flex w-full gap-2.5 px-3 py-2.5 text-left hover:bg-row-hover',
        !navigable && 'cursor-default',
      )}
    >
      <span
        aria-hidden
        className={cn(
          'mt-1.5 size-1.5 shrink-0 rounded-full',
          notification.isRead ? 'bg-transparent' : 'bg-accent',
        )}
      />
      <span className="min-w-0 flex-1">
        <span className="block text-xs text-muted">
          {NOTIFICATION_TYPE_LABELS[notification.type] ?? notification.type}
        </span>
        <span
          className={cn('block text-sm text-ink', notification.isRead ? 'font-normal' : 'font-medium')}
        >
          {notification.title}
        </span>
        <span className="mt-0.5 block line-clamp-2 text-xs text-ink-soft">{notification.body}</span>
        <span className="mt-0.5 block text-xs text-faint">
          {formatDateTime(notification.createdAtUtc)}
        </span>
      </span>
    </button>
```

- [ ] **Step 2: Verify**

Run: `cd frontend && npm run build`
Expected: PASS

- [ ] **Step 3: Commit**

```bash
cd frontend && git add src/routes/notifications/NotificationBell.tsx
git commit -m "style: reskin NotificationBell dropdown and rows"
```

---

### Task 18: Task board (TaskCard, TaskBoardPage)

**Files:**
- Modify: `frontend/src/routes/tasks/TaskCard.tsx`
- Modify: `frontend/src/routes/tasks/TaskBoardPage.tsx`

- [ ] **Step 1: Reskin TaskCard**

In `frontend/src/routes/tasks/TaskCard.tsx`, replace:

```tsx
    <article className="rounded-lg bg-white p-3 shadow-sm ring-1 ring-slate-200">
      <Link
        to={`/tasks/${task.id}`}
        className="hover:text-brand-700 block text-sm font-medium text-slate-900 hover:underline"
      >
```

with:

```tsx
    <article className="rounded-[15px] bg-surface p-3 shadow-[0_4px_15px_rgba(0,0,0,0.05)] ring-1 ring-divider">
      <Link
        to={`/tasks/${task.id}`}
        className="hover:text-accent-hover block text-sm font-medium text-ink hover:underline"
      >
```

Replace:

```tsx
      {task.dueAtUtc && (
        <p
          className={
            task.isOverdue
              ? 'mt-2 flex items-center gap-1 text-xs font-medium text-red-600'
              : 'mt-2 text-xs text-slate-500'
          }
        >
```

with:

```tsx
      {task.dueAtUtc && (
        <p
          className={
            task.isOverdue
              ? 'mt-2 flex items-center gap-1 text-xs font-medium text-danger'
              : 'mt-2 text-xs text-muted'
          }
        >
```

Replace:

```tsx
        <div className="flex items-center gap-2.5 text-xs text-slate-500">
```

with:

```tsx
        <div className="flex items-center gap-2.5 text-xs text-muted">
```

- [ ] **Step 2: Reskin TaskBoardPage**

In `frontend/src/routes/tasks/TaskBoardPage.tsx`, replace:

```tsx
        <h1 className="text-2xl font-semibold text-slate-900">Дъска</h1>
```

with:

```tsx
        <h1 className="font-serif text-[34px] leading-[1.15] font-normal text-ink">Дъска</h1>
```

Replace:

```tsx
      <p className="text-sm text-slate-500">
        Показват се всички видими задачи без отказаните. За филтри използвай списъка.
      </p>
```

with:

```tsx
      <p className="text-sm text-muted">
        Показват се всички видими задачи без отказаните. За филтри използвай списъка.
      </p>
```

Replace:

```tsx
    <section className="rounded-xl bg-slate-100/70 p-3">
      <h2 className="mb-3 flex items-center justify-between px-1 text-sm font-semibold text-slate-700">
        {title}
        <span className="rounded-full bg-white px-2 py-0.5 text-xs font-normal text-slate-500">
          {tasks.length}
        </span>
      </h2>

      {tasks.length === 0 ? (
        <p className="px-1 py-6 text-center text-sm text-slate-400">Няма задачи</p>
      ) : (
```

with:

```tsx
    <section className="rounded-[20px] bg-page p-3">
      <h2 className="mb-3 flex items-center justify-between px-1 text-sm font-bold text-ink-soft">
        {title}
        <span className="rounded-full bg-surface px-2 py-0.5 text-xs font-normal text-muted">
          {tasks.length}
        </span>
      </h2>

      {tasks.length === 0 ? (
        <p className="px-1 py-6 text-center text-sm text-faint">Няма задачи</p>
      ) : (
```

- [ ] **Step 3: Verify**

Run: `cd frontend && npm run build`
Expected: PASS

- [ ] **Step 4: Commit**

```bash
cd frontend && git add src/routes/tasks/TaskCard.tsx src/routes/tasks/TaskBoardPage.tsx
git commit -m "style: reskin task board and card"
```

---

### Task 19: Task list, detail, form, filters, status menu, comments, documents

**Files:**
- Modify: `frontend/src/routes/tasks/TasksPage.tsx`
- Modify: `frontend/src/routes/tasks/TaskDetailPage.tsx`
- Modify: `frontend/src/routes/tasks/TaskFormPage.tsx`
- Modify: `frontend/src/routes/tasks/TaskFilters.tsx`
- Modify: `frontend/src/routes/tasks/TaskStatusMenu.tsx`
- Modify: `frontend/src/routes/tasks/TaskComments.tsx`
- Modify: `frontend/src/routes/tasks/TaskDocuments.tsx`

- [ ] **Step 1: Reskin TasksPage**

In `frontend/src/routes/tasks/TasksPage.tsx`, apply these five replacements:

Replace `className="mt-0.5 size-4 shrink-0 text-red-500"` with `className="mt-0.5 size-4 shrink-0 text-danger"`.

Replace `className="hover:text-brand-700 font-medium hover:underline"` with `className="hover:text-accent-hover font-medium hover:underline"`.

Replace `<span className="text-slate-400">Организационна</span>` with `<span className="text-faint">Организационна</span>`.

Replace `<span className={cn(task.isOverdue && 'font-medium text-red-600')}>` with `<span className={cn(task.isOverdue && 'font-medium text-danger')}>`.

Replace:

```tsx
          <h1 className="text-2xl font-semibold text-slate-900">Задачи</h1>
```

with:

```tsx
          <h1 className="font-serif text-[34px] leading-[1.15] font-normal text-ink">Задачи</h1>
```

Replace `<p className="mt-1 text-sm text-slate-600">` with `<p className="mt-1 text-sm text-muted">`.

Replace `<div role="tablist" aria-label="Обхват" className="flex gap-1 border-b border-slate-200">` with `<div role="tablist" aria-label="Обхват" className="flex gap-1 border-b border-divider">`.

Replace:

```tsx
          ? 'border-brand-600 text-brand-700'
          : 'border-transparent text-slate-600 hover:text-slate-900',
```

with:

```tsx
          ? 'border-accent text-ink'
          : 'border-transparent text-muted hover:text-ink',
```

Replace `<div className="mt-1 flex items-center gap-3 text-xs text-slate-500">` with `<div className="mt-1 flex items-center gap-3 text-xs text-muted">`.

- [ ] **Step 2: Reskin TaskDetailPage**

In `frontend/src/routes/tasks/TaskDetailPage.tsx`, replace:

```tsx
import { Alert } from '@/components/ui/Alert'
import { Avatar } from '@/components/ui/Avatar'
```

with:

```tsx
import { Alert } from '@/components/ui/Alert'
import { Avatar } from '@/components/ui/Avatar'
import { Card } from '@/components/ui/Card'
```

(Adjust the exact insertion point to alphabetical order among the existing `components/ui/*` imports in the file, matching the pattern used in other tasks in this plan.)

Replace:

```tsx
      <div className="rounded-xl bg-white p-6 shadow-sm ring-1 ring-slate-200">
        <div className="flex flex-wrap items-start justify-between gap-3">
          <h1 className="text-xl font-semibold text-slate-900">{task.title}</h1>
```

with:

```tsx
      <Card variant="panel">
        <div className="flex flex-wrap items-start justify-between gap-3">
          <h1 className="font-serif text-2xl font-normal text-ink">{task.title}</h1>
```

Replace:

```tsx
        <p className="mt-5 border-t border-slate-200 pt-5 text-sm whitespace-pre-wrap text-slate-700">
```

with:

```tsx
        <p className="mt-5 border-t border-divider pt-5 text-sm whitespace-pre-wrap text-ink-soft">
```

Replace:

```tsx
        <dl className="mt-5 grid gap-x-6 gap-y-4 border-t border-slate-200 pt-5 text-sm sm:grid-cols-2">
```

with:

```tsx
        <dl className="mt-5 grid gap-x-6 gap-y-4 border-t border-divider pt-5 text-sm sm:grid-cols-2">
```

Replace:

```tsx
            <dt className="text-slate-500">Краен срок</dt>
            <dd className="mt-0.5 flex items-center gap-1.5 font-medium text-slate-900">
              {isOverdue && <AlertTriangle aria-hidden className="size-4 text-red-500" />}
              <span className={isOverdue ? 'text-red-600' : undefined}>
```

with:

```tsx
            <dt className="text-muted">Краен срок</dt>
            <dd className="mt-0.5 flex items-center gap-1.5 font-medium text-ink">
              {isOverdue && <AlertTriangle aria-hidden className="size-4 text-danger" />}
              <span className={isOverdue ? 'text-danger' : undefined}>
```

Replace:

```tsx
            <dt className="text-slate-500">Създадена</dt>
            <dd className="mt-0.5 font-medium text-slate-900">
```

with:

```tsx
            <dt className="text-muted">Създадена</dt>
            <dd className="mt-0.5 font-medium text-ink">
```

Replace `<span className="font-normal text-slate-500"> от {task.createdBy.fullName}</span>` with `<span className="font-normal text-muted"> от {task.createdBy.fullName}</span>`.

Replace:

```tsx
      <section className="rounded-xl bg-white p-6 shadow-sm ring-1 ring-slate-200">
        <h2 className="mb-4 text-sm font-semibold text-slate-900">
```

(this is the section immediately after the closing of the first card — change its wrapper to `</Card>` above it and this one to `<Card variant="panel">`, keeping the inner `<h2>` sans-serif since it's followed by a compact list rather than being a standalone page section — but for consistency with every other detail page in this plan, promote it to the same Georgia section style):

with:

```tsx
      <Card variant="panel">
        <h2 className="mb-4 font-serif text-[22px] leading-[1.3] font-normal text-ink">
```

Make sure the closing tag of the first `<Card variant="panel">` (right before this second section starts) is `</Card>` and the closing tag of this second section (originally `</section>`) is also changed to `</Card>`.

Replace:

```tsx
      className="inline-flex items-center gap-1.5 text-sm text-slate-600 hover:text-slate-900"
```

with:

```tsx
      className="inline-flex items-center gap-1.5 text-sm text-muted hover:text-ink"
```

Replace:

```tsx
      <span className="group-hover:text-brand-700 truncate text-sm font-medium text-slate-900 group-hover:underline">
```

with:

```tsx
      <span className="group-hover:text-accent-hover truncate text-sm font-medium text-ink group-hover:underline">
```

- [ ] **Step 3: Reskin TaskFormPage**

In `frontend/src/routes/tasks/TaskFormPage.tsx`, replace:

```tsx
        className="inline-flex items-center gap-1.5 text-sm text-slate-600 hover:text-slate-900"
```

with:

```tsx
        className="inline-flex items-center gap-1.5 text-sm text-muted hover:text-ink"
```

Replace:

```tsx
      <h1 className="text-2xl font-semibold text-slate-900">
```

with:

```tsx
      <h1 className="font-serif text-[34px] leading-[1.15] font-normal text-ink">
```

Replace:

```tsx
        className="max-w-2xl space-y-4 rounded-xl bg-white p-6 shadow-sm ring-1 ring-slate-200"
```

with:

```tsx
        className="max-w-2xl space-y-4 rounded-[20px] bg-surface p-6 shadow-[0_4px_15px_rgba(0,0,0,0.05)] ring-1 ring-divider"
```

Replace:

```tsx
          <label htmlFor="task-description" className="block text-sm font-medium text-slate-700">
```

with:

```tsx
          <label htmlFor="task-description" className="block text-sm font-semibold text-ink-soft">
```

Replace:

```tsx
            className="focus:ring-brand-500 block w-full rounded-lg border-0 px-3 py-2 text-sm text-slate-900 shadow-sm ring-1 ring-slate-300 ring-inset focus:ring-2 focus:ring-inset focus:outline-none"
```

with:

```tsx
            className="block w-full rounded-xl border-2 border-line bg-surface px-4 py-3 text-sm text-ink shadow-[inset_0_2px_5px_rgba(0,0,0,0.05)] focus:border-accent focus:shadow-[0_0_0_4px_rgba(255,60,112,0.15)] focus:outline-none"
```

Replace:

```tsx
            <p role="alert" className="text-sm text-red-600">
```

with:

```tsx
            <p role="alert" className="text-sm text-danger">
```

Replace:

```tsx
            <span className="block text-sm font-medium text-slate-700">Вид</span>
```

with:

```tsx
            <span className="block text-sm font-semibold text-ink-soft">Вид</span>
```

Replace:

```tsx
            <p className="text-sm text-slate-500">
```

with:

```tsx
            <p className="text-sm text-muted">
```

- [ ] **Step 4: Reskin TaskFilters**

In `frontend/src/routes/tasks/TaskFilters.tsx`, replace:

```tsx
        <label className="flex h-10 items-center gap-2 text-sm text-slate-700">
```

with:

```tsx
        <label className="flex h-10 items-center gap-2 text-sm text-ink-soft">
```

Replace:

```tsx
            className="text-brand-600 focus:ring-brand-500 size-4 rounded border-slate-300"
```

with:

```tsx
            className="text-accent focus:ring-accent size-4 rounded border-line"
```

- [ ] **Step 5: Reskin TaskStatusMenu**

In `frontend/src/routes/tasks/TaskStatusMenu.tsx`, replace:

```tsx
          className="absolute right-0 z-10 mt-1 min-w-44 rounded-lg bg-white py-1 shadow-lg ring-1 ring-slate-200"
```

with:

```tsx
          className="absolute right-0 z-10 mt-1 min-w-44 rounded-[15px] bg-surface py-1 shadow-[0_10px_30px_rgba(0,0,0,0.15)] ring-1 ring-divider"
```

Replace:

```tsx
                'flex w-full items-center gap-2 px-3 py-2 text-left text-sm hover:bg-slate-50',
```

with:

```tsx
                'flex w-full items-center gap-2 px-3 py-2 text-left text-sm hover:bg-row-hover',
```

- [ ] **Step 6: Reskin TaskComments**

In `frontend/src/routes/tasks/TaskComments.tsx`, replace:

```tsx
    <section className="rounded-xl bg-white p-6 shadow-sm ring-1 ring-slate-200">
      <h2 className="mb-4 text-sm font-semibold text-slate-900">
```

with:

```tsx
    <Card variant="panel">
      <h2 className="mb-4 font-serif text-[22px] leading-[1.3] font-normal text-ink">
```

(Add `import { Card } from '@/components/ui/Card'` among this file's `@/components/ui/*` imports, and change the matching closing `</section>` to `</Card>`.)

Replace `<span className="font-medium text-slate-900">` with `<span className="font-medium text-ink">`.

Replace `<span className="ml-2 text-xs text-slate-500">` with `<span className="ml-2 text-xs text-muted">`.

Replace `<p className="mt-0.5 text-sm whitespace-pre-wrap text-slate-700">` with `<p className="mt-0.5 text-sm whitespace-pre-wrap text-ink-soft">`.

Replace `className="space-y-2 border-t border-slate-200 pt-4"` with `className="space-y-2 border-t border-divider pt-4"`.

Replace:

```tsx
              className="focus:ring-brand-500 block w-full rounded-lg border-0 px-3 py-2 text-sm text-slate-900 shadow-sm ring-1 ring-slate-300 ring-inset placeholder:text-slate-400 focus:ring-2 focus:ring-inset focus:outline-none"
```

with:

```tsx
              className="block w-full rounded-xl border-2 border-line bg-surface px-4 py-3 text-sm text-ink shadow-[inset_0_2px_5px_rgba(0,0,0,0.05)] placeholder:text-faint focus:border-accent focus:shadow-[0_0_0_4px_rgba(255,60,112,0.15)] focus:outline-none"
```

Replace `<p role="alert" className="text-sm text-red-600">` with `<p role="alert" className="text-sm text-danger">`.

- [ ] **Step 7: Reskin TaskDocuments**

In `frontend/src/routes/tasks/TaskDocuments.tsx`, replace:

```tsx
    <section className="rounded-xl bg-white p-6 shadow-sm ring-1 ring-slate-200">
```

with:

```tsx
    <Card variant="panel">
```

(Add `import { Card } from '@/components/ui/Card'` among this file's `@/components/ui/*` imports, and change the matching closing `</section>` to `</Card>`.)

Replace `<h2 className="text-sm font-semibold text-slate-900">` with `<h2 className="font-serif text-[22px] leading-[1.3] font-normal text-ink">`.

Replace `<ul className="divide-y divide-slate-100">` with `<ul className="divide-y divide-divider">`.

Replace `<FileText aria-hidden className="size-5 shrink-0 text-slate-400" />` with `<FileText aria-hidden className="size-5 shrink-0 text-faint" />`.

Replace `<p className="truncate text-sm font-medium text-slate-900">` with `<p className="truncate text-sm font-medium text-ink">`.

Replace `<p className="text-xs text-slate-500">` with `<p className="text-xs text-muted">`.

Replace `className="text-red-600 hover:bg-red-50"` with `className="text-danger hover:bg-tone-danger-bg"`.

- [ ] **Step 8: Verify**

Run: `cd frontend && npm run build`
Expected: PASS

- [ ] **Step 9: Commit**

```bash
cd frontend && git add src/routes/tasks/TasksPage.tsx src/routes/tasks/TaskDetailPage.tsx src/routes/tasks/TaskFormPage.tsx src/routes/tasks/TaskFilters.tsx src/routes/tasks/TaskStatusMenu.tsx src/routes/tasks/TaskComments.tsx src/routes/tasks/TaskDocuments.tsx
git commit -m "style: reskin remaining task routes with Card and new tokens"
```

---

### Task 20: Final verification pass

**Files:** none (verification only)

- [ ] **Step 1: Confirm no stale references to the removed `brand-*` theme remain**

Run: `cd frontend && grep -rn "brand-" src/ --include='*.tsx' --include='*.ts'`
Expected: no output (empty). If anything matches, go back and fix that file using the same token mapping used elsewhere in this plan (`brand-600`/`brand-700` → `accent`/`accent-hover` for interactive states, `brand-50`/`brand-100` → `accent-soft`, `text-brand-*` → `text-accent`/`text-ink`/`text-accent-hover` as appropriate to context).

- [ ] **Step 2: Confirm no stale `slate-*` or literal `red-*` classes remain outside `node_modules`**

Run: `cd frontend && grep -rn "slate-\|text-red-\|bg-red-\|border-red-\|ring-red-" src/ --include='*.tsx' --include='*.ts'`
Expected: no output. Fix any remaining matches with the token mapping used throughout this plan (`slate-900`→`ink`, `slate-700`/`slate-800`→`ink-soft`, `slate-600`/`slate-500`→`muted`, `slate-400`/`slate-300`→`faint`, `slate-200`/`slate-100`→`divider`, `slate-50`→`page`/`subtle`, `red-600`/`red-500`→`danger`).

- [ ] **Step 3: Full build**

Run: `cd frontend && npm run build`
Expected: PASS with zero errors.

- [ ] **Step 4: Lint**

Run: `cd frontend && npm run lint`
Expected: PASS (or only pre-existing warnings unrelated to this change).

- [ ] **Step 5: Start the dev server and visually verify the primary screens**

Use the `preview_start` tool with the `dev` configuration from `frontend/.claude/launch.json` (create it if it doesn't exist, pointing `runtimeExecutable` to `npm`, `runtimeArgs` to `["run", "dev"]`, `port` to Vite's default `5173`, `cwd` to `frontend`). Then, signed in as a test member:

- Screenshot `/login` — confirm the gradient logo mark, Georgia title, pink focus ring on the email field, pill submit button.
- Screenshot `/` (Dashboard) — confirm the warm page background, the two `Card` sections, Georgia page/section titles.
- Screenshot `/tasks` and `/tasks/board` — confirm the Kanban card shadow/radius, priority badges in the new tone colors, the active tab underline in pink.
- Screenshot `/events` (month view) — confirm the today-indicator circle is pink, event dots use the new tone colors.
- Screenshot `/budget` — confirm the icon badge uses the soft pink tint, not blue.
- Screenshot `/members` and a member detail page — confirm the table header/row-hover colors and the avatar initials tint.
- Screenshot `/departments` and a department detail page.
- Open the notification bell dropdown and confirm its panel styling.

Fix anything that visibly still shows blue/slate remnants or a broken layout (e.g. a missed `Card` closing tag) by editing the relevant file directly, then re-run Step 3.

- [ ] **Step 6: Commit any fixes found during visual verification**

Only if Step 5 required fixes:

```bash
cd frontend && git add -A
git commit -m "style: fix remaining design-system reskin issues found in visual verification"
```

---

## Self-review notes

- **Spec coverage:** every section of `docs/superpowers/specs/2026-08-19-design-system-reskin-design.md` (tokens, primitives, `Card`, shell, page-by-page pass) has a corresponding task. The spec's page-by-page list omitted the Tasks routes despite the source design covering the Kanban board explicitly — Tasks 18–19 close that gap.
- **No placeholders:** every step gives the literal old/new class strings or full file contents; no "add appropriate styling" steps.
- **Type consistency:** `Card`'s `variant` prop (`'default' | 'panel'`), `Avatar`'s `ring` prop, and every new `--color-*`/`--font-serif` token name are used identically across all tasks that reference them.
- **Scope discipline:** dark mode, behavior changes, and inventing a marketing-style hero section (present in the `.dc.html` mock but not in this app's actual pages) are explicitly out of scope per the spec.

# Design system reskin — spec

Source: `frontend/StudentCouncilManagerDesign.zip` (`Дизайн система StudentCouncilManager.dc.html`), a token/component spec generated against this repo's `frontend/src`. Replaces the current single blue `brand-{50..900}` Tailwind theme with a warm, pink-accented system. Light mode only — dark mode is explicitly out of scope for this pass.

## Goals

- Single accent color (`#FF3C70` pink) replaces `brand-600` blue as the interactive/primary color across buttons, focus rings, active nav/tab state, and filters.
- Blue (`#4A90E2`) is demoted to "data/info only" — no longer the primary brand color.
- Warm off-white page background (`#F4F0EE`) replaces `slate-50`.
- Georgia serif (system stack: `Georgia, 'Times New Roman', serif`) for page/section titles only; everything else stays system sans.
- Pill-shaped buttons, softer/warmer shadows, updated radius scale (8/12/15/20/full).
- A purple→red→orange gradient used sparingly: top bar, one section underline, one highlighted border/logo — never as a page background.
- Status/tone semantics (`neutral`/`success`/`warning`/`danger`/`info`) are unchanged in name and meaning — only their underlying hex values move to the new warm palette. `taskLabels.ts`, `eventLabels.ts`, `memberLabels.ts`, `notificationLabels.ts` require no changes.
- No component prop/API changes — this is a visual reskin, not a refactor of component contracts.

## Token architecture

Extend the existing `@theme` block in `frontend/src/index.css` (Tailwind v4 CSS-first config) with a new semantic palette, as literal hex (matching the spec exactly — no oklch conversion, since dark-mode derivation is deferred):

**Neutrals**
| Token | Hex | Use |
|---|---|---|
| `ink` | `#1C1C1C` | Headings, primary button bg |
| `ink-soft` | `#333333` | Body text |
| `muted` | `#777777` | Secondary/meta text |
| `faint` | `#AAA4A4` | Disabled text |
| `page` | `#F4F0EE` | Page background |
| `surface` | `#FFFFFF` | Cards, inputs |
| `subtle` | `#FBF9F8` | Table header, quiet panels |
| `line` | `#ECE7E7` | Input border (default) |
| `divider` | `#EEEAEA` | Card borders, dividers |
| `border` | `#E6E0E0` | Secondary button border |

**Accent / data**
| Token | Hex | Use |
|---|---|---|
| `accent` | `#FF3C70` | Primary interactive color |
| `accent-hover` | `#E0295A` | Link hover |
| `accent-soft` | `#FFE5EA` | Avatar initials bg |
| `accent-soft-text` | `#C72552` | Avatar initials text |
| `data` | `#4A90E2` | Blue, data/info only |

**Tone triples** (bg / text / border, per tone — used by `Badge` and `Alert`):
- `neutral`: `#F4F0EE` / `#555555` / `#EAE4E4`
- `success`: `#F7F9F7` / `#1E7A34` / `#E3ECE5` (alert text variant `#1B5F2A`)
- `warning`: `#FDFAF3` / `#96690A` / `#F0E6CF` (alert text variant `#7A5508`)
- `danger`: `#FDF6F7` / `#B52B39` / `#F4DFE2` (alert text variant `#8E2028`)
- `info`: `#F5F8FC` / `#2F6CB0` / `#DBE6F3` (alert text variant `#2A5B91`)

**Gradient**: `linear-gradient(90deg, rgba(137,58,180,.55) 0%, rgba(253,29,29,.69) 51%, rgba(252,176,69,.82) 100%)` — not a color token; added as a small reusable `.gradient-accent-bar` utility class in `index.css`, used in at most 2-3 places per screen (AppShell top bar, logo mark, one section underline).

**Radius scale**: 8 (badge), 12 (input), 15 (card/table), 20 (panel/modal), full (button, avatar).

**Shadows**:
- Card/row: `0 4px 15px rgba(0,0,0,.05)`
- Modal: `0 15px 50px rgba(0,0,0,.15), 0 2px 6px rgba(0,0,0,.1)`
- Menu/dropdown: `0 10px 30px rgba(0,0,0,.15)`
- Focus: `0 0 0 4px rgba(255,60,112,.15)` + 2px `accent` border

## Component changes (`components/ui/*`)

No API/prop changes — internal class strings only.

- **Button**: `rounded-full`, heights 44px/36px (was 40/32). Primary: `ink` bg, `accent` bg + shadow on hover. Secondary: transparent, 2px `border`, `accent` border/text on hover. Ghost: transparent, `page` bg on hover. Danger: unchanged red family, new hex.
- **Input/Select**: 2px `line` border, radius 12, inset shadow. Focus: 2px `accent` border + halo shadow (replaces `ring-brand-500`/`ring-2`). Error/disabled logic unchanged, new colors.
- **Badge**: 5 tones, new hex triples, radius 8.
- **Alert**: 4 tones (error/warning/success/info), new hex, existing lucide icons kept.
- **Avatar**: initials bg `accent-soft`/`accent-soft-text` (was `brand-100/700`). Gradient ring variant reserved for the header's own-profile avatar only (new, additive — used in AppShell).
- **EmptyState**: unchanged structure, new neutral colors.
- **Pagination**: circular pill prev/next buttons (was rectangular), `accent` hover.
- **Table**: card radius 15, header bg `subtle`, row hover `#FDF9FA` (warm pink-tinted, replaces `slate-50`).
- **Modal**: radius 20, heavier shadow, Georgia title.

## New shared `Card` component

Seven+ files currently hand-roll an identical `rounded-xl bg-white p-{5,6} shadow-sm ring-1 ring-slate-200` shell: `DashboardPage`, `BudgetPage`, `EventDetailPage`, `MemberDetailPage`, `MyProfilePage`, `DepartmentsPage`, `DepartmentDetailPage`, `AuthLayout`. Extract `components/ui/Card.tsx` (radius 15/20, spec shadow, `divider` border) and migrate all call sites to it.

## Shell (`AppShell` / `AuthLayout`)

- Gradient top bar (thin strip) above the header.
- Header stays white; logo mark gets a gradient bg square (purple→red→orange) with initials.
- Active nav tab: bottom border switches from `border-brand-600 text-brand-700` to `border-accent text-ink`. Inactive unchanged pattern, new neutral hex.
- Header avatar (own profile) gets the gradient-ring treatment; this is the one "own profile" special case from the spec.
- `AuthLayout` card becomes the new `Card` component; logo badge bg moves from `brand-600` to the gradient.

## Page-by-page pass

After primitives + `Card` land, sweep every route file for direct Tailwind literals and replace with new tokens / `Card`:

- `DashboardPage.tsx` — card → `Card`, `bg-green-600`/`bg-slate-300` status dots → tone tokens.
- `BudgetPage.tsx` — card → `Card`, `bg-brand-50`/`text-brand-600` icon badge → `accent-soft`/`accent`, inline `text-red-600` delete button → danger tone.
- `CalendarPage.tsx` — segmented control active/inactive states → new neutrals, `accent` active.
- `MonthGrid.tsx` — bespoke grid, not a primitive: today-indicator circle `bg-brand-600` → `accent`; header/cell borders → `divider`/`subtle`; `EVENT_TYPE_DOTS` palette reviewed against new tone hexes.
- `EventAgenda.tsx` — card wrappers → `Card`, `divide-slate-100` → `divider`, inline `text-red-600` deadline label → danger tone.
- `EventDetailPage.tsx` — cards → `Card`, `group-hover:text-brand-700` → `accent-hover`.
- `MembersPage.tsx` — `hover:text-brand-700` → `accent-hover`.
- `MemberDetailPage.tsx`, `MyProfilePage.tsx` — cards → `Card`.
- `DepartmentsPage.tsx` — card grid → `Card` + `hover:ring-brand-300` → `hover:ring-accent`/soft variant.
- `DepartmentDetailPage.tsx` — cards → `Card`, `group-hover:text-brand-700` → `accent-hover`.
- `NotificationBell.tsx` — bespoke, not a primitive: bell button, unread-count pill (`bg-red-500`), dropdown panel shadow/radius, unread dot (`bg-brand-600` → `accent`), "mark all read" link color — all manually reskinned.
- `LoginPage.tsx` — `text-brand-700` forgot-password link → `accent-hover`.

## Explicitly out of scope

- Dark mode (no `dark:` variants, no theme toggle).
- Any behavior/logic changes.
- Inventing hero-section page headers (the `.dc.html` mock's marketing-style hero doesn't apply to this app's actual page layouts).

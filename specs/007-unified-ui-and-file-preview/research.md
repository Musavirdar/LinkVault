# Technical Research & Decisions: Unified Apple UI/UX & Universal File Preview

**Feature**: `007-unified-ui-and-file-preview`
**Status**: Completed

---

## 1. Universal In-App File Preview & Streaming Architecture

### Decision
Implement client-side authenticated Blob streaming with `URL.createObjectURL` for media/documents/code under 50MB, backed by ticket-based inline stream URLs for standalone previews and direct downloads. The preview system will be powered by a dedicated, reusable Apple-grade `FilePreviewModal` component integrated into all file tables and panels (`FileList.tsx`, `FilesPage.tsx`, `DirectoryDetailPanel.tsx`, and `SharedFilePage.tsx`).

### Rationale
- **Security & Authorization**: Directly passing `Authorization: Bearer <token>` in an `<img>` or `<iframe src="...">` tag is unsupported by HTML standards. Fetching the file data via `apiClient` / `fetch()` with the existing auth header and wrapping the resulting `Blob` in `URL.createObjectURL(blob)` allows 100% authenticated inline display of images, PDFs, text, and media without exposing tokens in query strings.
- **Zero Double-Download**: By obtaining a Blob or ObjectURL, the user can inspect, zoom, read, or click "Download" without making a redundant network round-trip.
- **Single-Use Ticket Compatibility**: For cases where opening in a new tab is preferred, `POST /api/files/{id}/ticket` provides a short-lived, single-use ticket that streams with `Content-Disposition: inline`.
- **Format Specialization**:
  - **Images** (`image/*`): Full viewport rendering with zoom/scale, pan, dimensions, and dimensions metadata.
  - **PDF Documents** (`application/pdf`): Embedded object/iframe displaying the blob URL with native PDF paging and zoom tools.
  - **Code & Plaintext** (`text/*`, `application/json`, code extensions): Decoded UTF-8 text rendered inside a monospace container with line numbers, syntax coloration, and a one-click copy button.
  - **Audio/Video** (`audio/*`, `video/*`): HTML5 `<video>` and `<audio>` controls with seekbar, volume, and playback speed.
  - **Binary/Archives** (`application/zip`, etc.): Informative overview card detailing file attributes (name, extension, size, MIME type, upload timestamp, directory) with a prominent download button.

### Alternatives Considered
- *Third-party cloud preview service (e.g. Google Docs Viewer or Office 365)*: Rejected because it requires LinkVault files to be publicly accessible over the internet, violating privacy and security access rules.
- *Server-side thumbnail/rendering generation*: Rejected because it introduces heavy server dependencies (like ImageMagick or headless Chromium) without providing immediate in-browser document reading and text copying.

---

## 2. Replacing Discord Rails with Unified Apple HIG Layout

### Decision
Deprecate the Discord-style 4-column layout (`ServerBar`, `ChannelSidebar`, right-side `ActivityPanel`) in `RootLayout.tsx` and replace it with a single, elegant, translucent Apple Sidebar (`AppleSidebar.tsx`) matching `LandingPage.tsx` and `AppleLandingPage.tsx`.

### Rationale
- **User Consistency**: The user explicitly stated that the landing page UI/UX is far superior to the discord-style layout inside the app. By extracting the landing page's sidebar design into a primary layout component (`AppleSidebar`), all authenticated pages (`/dashboard`, `/files`, `/directories`, `/links`, `/settings`, `/audit`, `/agent`) instantly inherit:
  - Frosted glass sidebar (`w-64`, `bg-white/80 dark:bg-[#1C1C1E]/80 backdrop-blur-2xl border-r border-black/[0.06] dark:border-white/10`)
  - LinkVault logo and title
  - Quick Search / Command Palette shortcut (`⌘K`)
  - Nav items with smooth spring pills (`layoutId="active-sidebar-pill"`, `appleSpring`)
  - Preferences & Theme controls at the base
  - Spacious main content canvas (`MainCanvas`) freed from the 240px right-hand activity rail
- **Apple HIG Token Standardization**:
  - **Radii**: 20px squircle containers (`rounded-2xl`), 12px control elements (`rounded-xl` / `rounded-lg`).
  - **Surfaces**: Frosted glass materials (`backdrop-blur-2xl bg-white/70 dark:bg-[#1C1C1E]/70 border-black/[0.06] dark:border-white/10 shadow-sm`).
  - **Typography**: SF Pro font stack, tight tracking (`tracking-tight`), crisp font weights (regular 400, medium 500, semibold 600).
  - **Motion**: Physical Apple springs (`stiffness: 400, damping: 32, mass: 0.8`).
  - **Colors**: Cupertino primary blue (`#0071E3`), Apple Dark (`#000000` / `#1C1C1E`), Apple Light (`#F5F5F7` / `#FFFFFF`).

### Alternatives Considered
- *Keeping the 4-column Discord layout and merely tweaking CSS borders*: Rejected because the Discord server bubbles and right-side activity column are fundamentally incongruent with Apple Human Interface Guidelines and clutter the file management workflow.
- *Full rewrite of every page from scratch*: Rejected because existing page business logic (API integration, state management, upload handlers, ticket downloads) is solid and only requires layout re-anchoring and Apple HIG container styling.

---

## 3. Login & Authentication Flow Redesign

### Decision
Restructure `LoginPage.tsx` into a centered Apple-grade frosted glass container (`max-w-md w-full rounded-3xl bg-white/80 dark:bg-[#1C1C1E]/80 backdrop-blur-2xl border border-black/[0.06] dark:border-white/10 shadow-2xl p-8`) positioned over an ambient radial blur background matching the landing page.

### Rationale
- Eliminates the disjointed experience where users click "Sign In" or "Get Started" on the landing page and land on a dark, cluttered discord-style form.
- Provides smooth segmented tab controls ("Sign In" / "Create Account"), password strength indicators, and one-click Demo Admin login.

---

## 4. Mobile Responsiveness Strategy

### Decision
Adopt the Apple iOS / iPadOS navigation drawer pattern for viewports `< 768px`:
- Floating frosted glass menu pill button (`fixed top-4 left-4 z-50 p-2.5 rounded-full bg-white/80 dark:bg-[#1C1C1E]/80 backdrop-blur-xl border border-black/[0.08] dark:border-white/12 shadow-sm`).
- Slide-out drawer with Apple spring transition (`stiffness: 350, damping: 32`) over an ambient backdrop blur overlay.
- Full viewport modal sheet on mobile for the `FilePreviewModal`.


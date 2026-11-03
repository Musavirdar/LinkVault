# Feature Specification: Unified Apple Design System & Universal In-App File Preview

**Feature Branch**: `007-unified-ui-and-file-preview`

**Created**: November 3, 2026

**Status**: Draft

**Input**: User description: "1. the landing page is having one design ui/ux and loginpage, dashboard etc look different, i want my should look like the landing page design. simply landing page is better looking and i want my whole app look better, 2. The biggest problem in my app is when i upload a file from every upload sec it only showed file uploaded but i can't open the file i want to be able open any file in my app it has only download option i want both open and download option"

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Universal In-App File Preview ("Open") & Download (Priority: P1)

As a user browsing files, directories, or dashboard activity, I want to be able to immediately open and view any uploaded file inside the application (images, PDFs, documents, text/code, audio, and video) with an interactive preview modal, while retaining the ability to download the file directly, so that I can inspect my files without leaving the application or cluttering my local downloads folder.

**Why this priority**: Users are currently blocked from viewing the contents of any files they upload. Having only a "download" button prevents rapid verification and browsing, making the core file storage capability incomplete.

**Independent Test**: Can be tested by uploading a file (image, PDF, or text file) from any upload section (Dashboard, Files page, or Directory view), clicking the "Open" action (or clicking the file item), and confirming that the in-app preview modal displays the rendered file content with controls to zoom/inspect, copy, or download.

**Acceptance Scenarios**:

1. **Given** a user is viewing a list of files in the Dashboard, Files page, or a Directory panel,
   **When** the user hovers or inspects a file row or card,
   **Then** the user sees both an "Open" action (preview) and a "Download" action clearly accessible.
2. **Given** a user has uploaded an image file (e.g., JPEG, PNG, WebP),
   **When** the user clicks "Open" on the file,
   **Then** an in-app lightbox modal opens showing the high-resolution image with zoom controls, file name, dimensions, size, and a download button.
3. **Given** a user has uploaded a PDF document,
   **When** the user clicks "Open",
   **Then** an in-app viewer opens displaying the multi-page PDF document inline with navigation and download options.
4. **Given** a user has uploaded a code or plain text file (e.g., `.txt`, `.json`, `.csv`, `.md`, `.js`, `.cs`, `.py`),
   **When** the user clicks "Open",
   **Then** an in-app code/text viewer modal opens displaying formatted text with syntax readability, line numbers, a copy-to-clipboard button, and download option.
5. **Given** a user has uploaded an audio or video file (e.g., MP3, WAV, MP4, WebM),
   **When** the user clicks "Open",
   **Then** an in-app media player modal opens with interactive play, pause, seek, volume controls, and download option.
6. **Given** a user has uploaded an unsupported or complex binary archive (e.g., ZIP, TAR, unknown binary),
   **When** the user clicks "Open",
   **Then** a clean file details card opens showing metadata (file type, size, upload date, directory path) with a prominent "Download File" button, rather than a broken viewer or silent failure.
7. **Given** a user clicks the "Download" action directly on any file list or inside the preview modal,
   **When** the download action is triggered,
   **Then** the file is securely streamed and saved to the user's device with its original file name and format.

---

### User Story 2 - Unified Apple HIG Visual & Interaction Design System (Priority: P1)

As a user navigating between the Landing page, Login page, Dashboard, Directories, Files, Links, Settings, and Profile views, I want a cohesive, elegant Apple Human Interface Guidelines (HIG) visual design language across the entire application—featuring translucent frosted glass sidebars, squircle containers, clean typography, consistent primary accents, and fluid physical motion—so that the internal application feels as polished, modern, and beautiful as the landing page.

**Why this priority**: The current app layout suffers from severe visual fragmentation: the landing page features sleek Apple frosted glass and squircle cards, whereas authenticated views and the login page display discordant Discord-style server/channel rails and right-side activity panels. Unifying the design resolves user dissatisfaction and provides an intuitive, high-end experience.

**Independent Test**: Can be tested by navigating through every screen of the application (Landing, Login, Dashboard, Directories, Files, Links, Settings) and verifying that the navigation sidebar, typography, container shapes, frosted materials, button styling, and light/dark modes share the exact same aesthetic and layout language.

**Acceptance Scenarios**:

1. **Given** a user navigates to the Login / Register screen,
   **When** the page renders,
   **Then** the login experience uses an Apple-grade frosted glass card centered on an ambient gradient background matching the Landing Page design tokens, with smooth tab transitions and clean typography.
2. **Given** an authenticated user opens the application (Dashboard, Directories, Files, Links, Settings, Audit, Profile),
   **When** the main application layout renders,
   **Then** the discordant Discord multi-column rail (narrow server icon strip and right-side activity rail) is replaced by the unified Apple translucent sidebar (`w-64`, frosted glass blur, brand logo, quick navigation items, and user profile/settings anchor) consistent with the Landing Page.
3. **Given** a user views content cards, tables, and dialogs across any page,
   **When** inspecting visual containers and controls,
   **Then** all surfaces adhere to Apple HIG design tokens: 20px squircle container radius, 12px control radius, subtle borders (`border-white/10` or light mode counterpart), and fluid spring micro-interactions.
4. **Given** a user switches between Dark Mode and Light Mode,
   **When** the theme is toggled,
   **Then** all pages, sidebars, modals, and file preview panels smoothly adapt to the corresponding Apple Dark (`#000000` / `#1C1C1E`) or Apple Light (`#F5F5F7` / `#FFFFFF`) color palette with maintained contrast.
5. **Given** a user has customized their primary accent color in Preferences,
   **When** viewing active navigation pills, focus states, and primary call-to-action buttons,
   **Then** the custom accent color is honored smoothly across all unified components while defaulting to Cupertino Blue (`#0071E3`).

---

### User Story 3 - Responsive Apple Navigation & Layout Modes (Priority: P2)

As a user accessing LinkVault on mobile devices, tablets, or compact desktop windows, I want the unified Apple navigation and file viewing experiences to respond gracefully to smaller viewports with an intuitive drawer and touch-friendly controls.

**Why this priority**: Users need consistent access to file management and viewing across laptop, tablet, and mobile displays without layout clipping or cramped multi-rail bars.

**Independent Test**: Can be tested by resizing the browser window to mobile width (<768px) and confirming that the navigation collapses into an Apple-style drawer accessible via a frosted floating menu control, and file previews adapt to full-screen touch viewports.

**Acceptance Scenarios**:

1. **Given** a viewport width narrower than 768px,
   **When** viewing any authenticated page,
   **Then** the sidebar collapses into a sliding Apple drawer with a frosted backdrop, accessible via a standard top-left tap target.
2. **Given** a mobile user opens a file preview modal,
   **When** the preview appears,
   **Then** it expands to fit the mobile screen safely, with accessible close, zoom/scroll, and download touch targets.

---

### Edge Cases

- **Corrupted or 0-byte file uploaded**: When opened, the previewer detects empty or invalid content and displays an alert indicating "Empty or unreadable file" while offering the option to download the raw file.
- **Large file preview (>50MB text or video)**: Media files support streaming range requests for instantaneous playback; text files above a readable size limit (e.g., 5MB) prompt the user with a "File is very large — download to view completely or preview first 1,000 lines" safeguard to prevent browser tab locking.
- **Expired session or preview ticket**: If a file ticket or auth session expires while a preview is open, the system alerts the user cleanly with a re-authentication or refresh prompt without discarding current view context or refreshing the entire app.
- **SVG and scriptable file types**: SVGs and potentially scriptable files are rendered in an isolated sandbox or previewed as safe images/source code to prevent cross-site scripting (XSS).
- **Directory with 0 files**: Empty directories display an Apple-styled empty state illustration with a prominent "Upload File" drag-and-drop zone and action button.
- **Concurrent uploads in directory**: When multiple files are dropped or selected simultaneously, each file displays immediate optimistic upload progress, and once complete, both "Open" and "Download" actions become instantly available.

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST provide both an **"Open"** action (preview) and a **"Download"** action for every file displayed across the Dashboard, Files Page, Directory Detail Panel, and Shared File views.
- **FR-002**: Clicking "Open" (or clicking the file item row) MUST open an in-app file preview modal/lightbox without navigating away from the current page.
- **FR-003**: The file preview modal MUST support inline visual rendering for standard image formats (JPEG, PNG, GIF, WebP, SVG).
- **FR-004**: The file preview modal MUST support inline document rendering for PDF files with paging or scroll controls.
- **FR-005**: The file preview modal MUST support inline code/text rendering for plaintext and source files (TXT, JSON, MD, CSV, XML, YAML, LOG, JS, TS, CS, PY, HTML, CSS) with syntax formatting, line numbering, and a one-click copy button.
- **FR-006**: The file preview modal MUST support inline audio and video playback for standard browser media formats (MP4, WebM, MP3, WAV, OGG).
- **FR-007**: For unsupported binary formats or archives, the file preview modal MUST present an Apple-styled file overview card displaying metadata (file name, extension, formatted size, upload timestamp, directory location) and a primary "Download" action.
- **FR-008**: The backend streaming endpoint and download ticket generation MUST support inline content presentation with appropriate MIME headers and range processing.
- **FR-009**: The authenticated application layout MUST replace the legacy multi-column Discord rails (`ServerBar`, `ChannelSidebar`, right-hand `ActivityPanel`) with the unified, frosted glass Apple sidebar modeled directly after `LandingPage.tsx`.
- **FR-010**: The unified sidebar MUST include brand branding, quick jump search, primary navigation links (Dashboard, Agent Workspace, Files, Directories, Links, Activity/Audit), and bottom settings/profile controls.
- **FR-011**: All authenticated application views (`DashboardPage`, `DirectoriesPage`, `FilesPage`, `LinksPage`, `SettingsPage`, `AnalyticsPage`, `AuditPage`, `ProfileEditorPage`) MUST adopt the Apple HIG design language:
  - 20px squircle container radius (`rounded-2xl`)
  - 12px interactive element radius (`rounded-xl` / `rounded-lg`)
  - Translucent frosted glass containers (`backdrop-blur-2xl bg-white/70 dark:bg-[#1C1C1E]/70 border-black/[0.06] dark:border-white/10`)
  - SF Pro typographic hierarchy, weights, and tight letter tracking
  - Fluid spring physics transitions on interactive elements
- **FR-012**: The `LoginPage` and registration flow MUST be restyled to match the Apple aesthetic of the landing page, utilizing a centered frosted glass panel with ambient lighting and clean form controls.
- **FR-013**: The application MUST support seamless theme switching between Apple Dark Mode (`#000000` / `#1C1C1E`) and Apple Light Mode (`#F5F5F7` / `#FFFFFF`), updating all pages, sidebars, cards, and modals synchronously.
- **FR-014**: The application MUST preserve user customization for primary accent color (defaulting to Apple Cupertino Blue `#0071E3`).
- **FR-015**: On viewports narrower than 768px, the navigation MUST collapse into a responsive Apple slide-out drawer accessible via an unobtrusive floating button.

---

### Key Entities

- **FileResource**: Represents a stored file in LinkVault, containing unique identifier, file name, original name, storage path, MIME type, file size in bytes, download count, view count, directory association, active status, and timestamps.
- **FilePreviewContext**: Represents an active in-app preview session, encapsulating the target file metadata, detected format category (image, pdf, text, media, binary), streaming URL (or single-use inline ticket), zoom level, and display state.
- **ApplicationLayoutState**: Represents the unified application shell configuration, controlling active sidebar navigation item, theme mode (dark/light), custom accent color, mobile drawer open/close state, and command palette trigger.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: **100% In-App Preview Availability**: 100% of files listed in any view (Dashboard, Files, Directories) have an accessible "Open" action that successfully launches the preview modal without page navigation.
- **SC-002**: **Preview Launch Speed**: File preview modals for standard images, PDFs, and text files under 10MB open and render within 1.0 second on standard broadband connections.
- **SC-003**: **Design System Consistency**: 100% of internal pages (Login, Dashboard, Directories, Files, Links, Settings, Audit) utilize the unified Apple HIG design language (frosted glass sidebar, 20px squircle cards, SF Pro typography), eliminating legacy discordant Discord-style rails.
- **SC-004**: **Dual Action Usability**: In usability verification, 100% of users can distinguish and execute both "Open" (preview) and "Download" (save to disk) operations on their first attempt without confusion.
- **SC-005**: **Zero Mocks**: All previewed files render authentic content retrieved directly from the storage provider via authenticated or ticket-based streaming endpoints.

---

## Assumptions

- **Browser Capabilities**: Target browsers support standard modern HTML5 features including CSS backdrop-filter (`backdrop-blur`), flexbox/grid layout, native PDF rendering, and media audio/video elements.
- **Storage and File Access**: The backend storage provider (`IStorageProvider`) already contains the binary payloads uploaded by users and can stream files with accurate MIME types.
- **Authentication**: Existing JWT authentication and download ticket validation mechanisms are reused for inline preview streaming without requiring changes to token encryption schemes.
- **Scope Boundaries**: In-place collaborative file editing (e.g., editing DOCX or spreadsheets inside the browser) is out of scope for this feature; file viewing, copying text, zooming images, and downloading are in scope.
- **Navigation Structure**: The primary application layout replaces the 4-column Discord-style rail layout with a clean Apple-style left navigation bar, mirroring the layout structure established in `LandingPage.tsx`.
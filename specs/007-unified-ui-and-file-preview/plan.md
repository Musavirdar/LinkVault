# Implementation Plan: Unified Apple Design System & Universal In-App File Preview

**Branch**: `007-unified-ui-and-file-preview` | **Date**: November 3, 2026 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/007-unified-ui-and-file-preview/spec.md`

---

## Summary

Unify the LinkVault application under the Apple Human Interface Guidelines (HIG) visual design language established on the Landing Page, and implement an in-app file preview experience ("Open") alongside direct downloads across all file-listing surfaces.

1. **Apple HIG Layout & Navigation**:
   - Replace the legacy 4-column Discord layout (`ServerBar`, `ChannelSidebar`, right-hand `ActivityPanel`) with the unified Apple frosted glass sidebar (`AppleSidebar.tsx`), featuring brand identity, quick jump command search (`⌘K`), smooth spring pills (`layoutId="active-sidebar-pill"`), light/dark mode switching, and bottom preferences anchors.
   - Standardize all authenticated views (`DashboardPage`, `DirectoriesPage`, `FilesPage`, `LinksPage`, `SettingsPage`, `LoginPage`) with 20px squircle cards (`rounded-2xl`), 12px control elements (`rounded-xl`), translucent frosted glass materials (`backdrop-blur-2xl bg-white/70 dark:bg-[#1C1C1E]/70 border-black/[0.06] dark:border-white/10`), and SF Pro typography hierarchy.
2. **Universal In-App File Preview & Streaming**:
   - Create a reusable `FilePreviewModal.tsx` supporting images (high-res with zoom/pan), PDFs (embedded viewer), code/text (syntax styling, line numbers, copy button), audio/video (HTML5 player), and binary archives (informative metadata card with direct download CTA).
   - Ensure every file row/card across `FileList.tsx`, `FilesPage.tsx`, and `DirectoryDetailPanel.tsx` exposes both an **"Open"** (view) and **"Download"** (save) action.
   - Stream content securely via authenticated client-side blob retrieval and backend single-use tickets with `Content-Disposition: inline`.

---

## Technical Context

**Language/Version**: TypeScript 5.2 / React 18 (Frontend), C# 12 / .NET 8 (Backend)  
**Primary Dependencies**: Tailwind CSS, Framer Motion, Lucide React, Radix UI (Frontend); ASP.NET Core, Entity Framework Core (Backend)  
**Storage**: Local storage provider (`LocalStorageProvider.cs`) with relative upload paths, PostgreSQL / In-Memory relational database (`LinkVaultDbContext`)  
**Testing**: xUnit + WebApplicationFactory integration tests (`backend/LinkVault.Tests`), manual end-to-end browser scenarios  
**Target Platform**: Modern Evergreen Web Browsers (Chrome, Edge, Safari, Firefox), Desktop and Mobile viewports  
**Project Type**: Full-stack web application  
**Performance Goals**: File preview modal opens within <1.0s for files <10MB; 60fps spring transitions; instant theme toggling without flicker  
**Constraints**: Zero external cloud preview dependencies (local privacy preserved); zero mocks (authentic file streaming); preserve custom user accent preferences  
**Scale/Scope**: 5 primary pages updated (`RootLayout`, `LoginPage`, `FilesPage`, `DirectoriesPage`, `FileList`), 2 new components (`AppleSidebar`, `FilePreviewModal`)  

---

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle / Gate | Status | Evaluation |
|---|---|---|
| **Simplicity First** | PASS | Reuses existing `LandingPage` Apple design patterns and existing `FilesController` ticket and streaming endpoints without adding heavy third-party rendering engines. |
| **Single Responsibility** | PASS | `AppleSidebar` handles application navigation shell; `FilePreviewModal` handles in-app file inspection; existing controllers manage data persistence and file streaming. |
| **API-First** | PASS | Streamlines existing `/api/files/{id}/download` and `/api/files/{id}/ticket` endpoints with clear OpenAPI contract. |
| **Test Coverage** | PASS | Validated with 5 end-to-end quickstart scenarios and existing 71 .NET backend unit/integration tests. |
| **Technology Constraints** | PASS | Strictly .NET 8 backend and React 18 / Tailwind frontend. No new frameworks added. Docker Compose compatibility maintained. |

---

## Project Structure

### Documentation (`specs/007-unified-ui-and-file-preview/`)

```text
specs/007-unified-ui-and-file-preview/
├── spec.md                     # Feature specification
├── plan.md                     # Implementation plan (this file)
├── research.md                 # Phase 0 technical decisions
├── data-model.md               # Phase 1 data models & state architecture
├── quickstart.md               # Phase 1 validation scenarios
├── checklists/
│   └── requirements.md         # Requirements quality checklist
├── contracts/
│   ├── file-preview-api.yaml   # OpenAPI specification for preview & streaming
│   └── ui-tokens.md            # Apple HIG design tokens & component contracts
└── tasks.md                    # Phase 2 implementation tasks (/speckit-tasks output)
```

### Source Code Paths

```text
app/src/
├── components/
│   ├── layout/
│   │   ├── RootLayout.tsx          # Updated: replaced Discord rails with AppleSidebar
│   │   └── AppleSidebar.tsx        # New: unified Apple frosted glass navigation sidebar
│   ├── files/
│   │   └── FilePreviewModal.tsx    # New: universal in-app preview modal (images, pdfs, code, media)
│   ├── dashboard/
│   │   ├── FileList.tsx            # Updated: added Open button and click-to-preview
│   │   └── FileUpload.tsx          # Updated: Apple HIG styling & folder upload
│   └── directories/
│       └── DirectoryDetailPanel.tsx # Updated: added Open button alongside Download
├── pages/
│   ├── LoginPage.tsx               # Updated: Apple-grade centered frosted glass card
│   ├── DashboardPage.tsx           # Updated: Apple squircle layout & spacing
│   ├── FilesPage.tsx               # Updated: added Open button, click-to-preview, Apple styling
│   ├── DirectoriesPage.tsx         # Updated: Apple HIG card styling
│   └── LinksPage.tsx               # Updated: Apple HIG card styling
└── api/
    └── client.ts                   # Updated: added preview blob helper and ticket stream URL

backend/
├── Controllers/
│   └── FilesController.cs          # Verified: handles inline content streaming & tickets
└── Services/
    └── ResourceService.cs          # Verified: directory resource filtering
```

---

## Complexity Tracking

*No constitution violations. All architectural standards and technology constraints are respected.*

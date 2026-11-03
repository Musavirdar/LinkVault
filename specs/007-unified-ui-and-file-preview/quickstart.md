# Quickstart Validation Guide: Apple HIG Design & File Preview

**Feature**: `007-unified-ui-and-file-preview`
**Status**: Ready for Validation

This guide outlines runnable end-to-end verification scenarios to validate the unified Apple HIG design system and universal in-app file preview.

---

## Prerequisites

1. **Backend Server Running**:
   ```powershell
   cd backend
   dotnet run --no-build
   # Verified on http://localhost:5246
   ```
2. **Frontend Dev Server Running**:
   ```powershell
   cd app
   npm run dev
   # Verified on http://localhost:5173
   ```
3. **Demo Credentials**:
   - One-click "Demo Admin" button on `/login` (or credentials: `admin@linkvault.local` / `Admin123!`).

---

## Scenario 1: Verify Unified Apple Layout & Visual Language Across All Pages

### Objective
Prove that the legacy Discord-style 4-column layout is gone and replaced by the elegant translucent Apple Sidebar across all authenticated routes.

### Test Steps
1. Open browser to `http://localhost:5173/`.
2. Inspect the Landing Page sidebar (`bg-white/80 dark:bg-[#1C1C1E]/80 backdrop-blur-2xl`).
3. Click "Sign In" or navigate to `http://localhost:5173/login`.
4. Click "Demo Admin" to log in.
5. You are redirected to `http://localhost:5173/dashboard`.
6. Inspect the layout:
   - **Expected**: A single clean Apple frosted glass sidebar on the left (`w-64`). No Discord circles (`ServerBar`), no Discord channels list (`ChannelSidebar`), and no right-side `ActivityPanel` squeezing the canvas.
   - **Expected**: Cards use 20px squircle curvature (`rounded-2xl`), SF Pro typography, and Apple spring animations.
7. Click each navigation link in the sidebar (`Directories`, `Files`, `Links`, `Agent Workspace`, `Settings`).
   - **Expected**: Active navigation pill smoothly slides with Framer Motion spring physics. Each page adheres strictly to the Apple design system.
8. Toggle Dark / Light mode from the sidebar or header.
   - **Expected**: Whole app transitions smoothly between Apple Dark (`#000000` / `#1C1C1E`) and Apple Light (`#F5F5F7` / `#FFFFFF`).

---

## Scenario 2: In-App Preview for Images with Zoom & Download

### Objective
Prove that image files can be previewed immediately in an in-app lightbox modal with zoom and download controls.

### Test Steps
1. Navigate to `/files` or `/directories`.
2. Upload an image (`.png` or `.jpg`, e.g. `sample.png`).
3. In the file list, locate the uploaded image.
4. Verify both an **"Open"** (eye icon) and a **"Download"** (download icon) button are present.
5. Click **"Open"** (or click the row).
6. **Expected Outcome**:
   - `FilePreviewModal` opens with Apple spring transition.
   - High-definition image renders inside the modal viewport without triggering a browser download.
   - Image details (file name, dimensions, size, directory) are displayed in the header/footer.
   - Zoom in/out buttons scale the image smoothly.
   - Clicking "Download" inside the modal saves the file to disk.

---

## Scenario 3: In-App Preview for Documents & Plaintext / Code

### Objective
Prove that PDFs and code/text files render cleanly inline with syntax readability and copy buttons.

### Test Steps
1. Upload a `.pdf` document and a `.json` or `.txt` configuration file.
2. In `/files`, click **"Open"** on the `.pdf`.
   - **Expected**: Multi-page PDF document opens in the in-app viewer.
3. Close the modal and click **"Open"** on the `.json` or `.txt` file.
   - **Expected**: In-app code viewer modal displays the formatted code/text with line numbers and a "Copy to Clipboard" button.
4. Click "Copy to Clipboard".
   - **Expected**: Toast confirms "Copied to clipboard".

---

## Scenario 4: Unsupported Binary & Archive Fallback Card

### Objective
Prove that non-renderable binaries (e.g. `.zip`, `.tar`) present an Apple overview card with file metadata and download CTA instead of a blank screen or error.

### Test Steps
1. Upload a `.zip` archive file.
2. Click **"Open"** on the `.zip` file.
3. **Expected Outcome**:
   - Overview modal displays a stylish archive icon, file name, size, upload date, and a prominent "Download File" button.
   - Clicking "Download File" initiates the secure ticket stream and downloads the `.zip`.

---

## Scenario 5: Responsive Mobile Drawer Navigation (<768px)

### Objective
Prove that the unified Apple sidebar adapts cleanly to mobile viewports.

### Test Steps
1. Open Chrome DevTools (`F12`) and toggle device toolbar to iPhone / 390px width.
2. **Expected**:
   - Left sidebar collapses.
   - Floating frosted glass menu button appears in the top-left corner.
   - Tapping the menu button opens the Apple frosted slide-out drawer.
   - Navigating closes the drawer smoothly.


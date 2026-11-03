# UI Design Tokens & Component Contracts: Apple HIG Specification

**Feature**: `007-unified-ui-and-file-preview`
**Status**: Completed

---

## 1. Apple Color Palette

| Token | Light Mode Value | Dark Mode Value | Usage |
|---|---|---|---|
| `--bg-base` | `#F5F5F7` (Apple Gray 6) | `#000000` (Pure Dark) | Canvas background |
| `--bg-surface` | `rgba(255, 255, 255, 0.75)` | `rgba(28, 28, 30, 0.75)` | Squircles, cards, modals |
| `--bg-sidebar` | `rgba(255, 255, 255, 0.85)` | `rgba(28, 28, 30, 0.85)` | Translucent left sidebar |
| `--border-subtle` | `rgba(0, 0, 0, 0.06)` | `rgba(255, 255, 255, 0.10)` | Card borders, dividers |
| `--text-primary` | `#1D1D1F` (Apple Near-Black) | `#F5F5F7` (Apple Off-White)| Headings, primary labels |
| `--text-secondary`| `#6E6E73` (Apple Gray 1) | `#86868B` (Apple Gray 3) | Subtitles, timestamps |
| `--text-muted` | `#86868B` | `#636366` | Helper text, breadcrumbs |
| `--accent-primary`| `#0071E3` (Cupertino Blue) | `#0071E3` | Active pills, primary CTA |
| `--accent-success`| `#34C759` (Cupertino Green) | `#30D158` | Success badges, checkmarks |
| `--accent-warning`| `#FF9500` (Cupertino Orange) | `#FF9F0A` | Warnings, documents |
| `--accent-danger` | `#FF3B30` (Cupertino Red) | `#FF453A` | Errors, delete actions |

---

## 2. Geometry & Corner Radii

- **Container Radii (Cards, Panels, Dialogs)**: `rounded-2xl` (16px–20px squircle curvature)
- **Interactive Controls (Buttons, Inputs, Selects)**: `rounded-xl` (10px–12px curvature)
- **Small Badges & Keyboard Shortcuts (`⌘K`)**: `rounded-md` / `rounded-[6px]`
- **Avatars & Floating Trigger Buttons**: `rounded-full`

---

## 3. Materials & Blurs

- **Primary Frosted Glass (`backdrop-blur-2xl`)**:
  ```css
  backdrop-filter: blur(40px);
  -webkit-backdrop-filter: blur(40px);
  background-color: rgba(255, 255, 255, 0.80); /* Light */
  background-color: rgba(28, 28, 30, 0.80);     /* Dark */
  border: 1px solid rgba(0, 0, 0, 0.06);        /* Light */
  border: 1px solid rgba(255, 255, 255, 0.10);  /* Dark */
  ```
- **Card Elevated Glass (`backdrop-blur-xl`)**:
  ```css
  backdrop-filter: blur(24px);
  background-color: rgba(255, 255, 255, 0.70);
  box-shadow: 0 4px 24px -1px rgba(0, 0, 0, 0.05);
  ```

---

## 4. Motion & Spring Physics

Apple HIG fluid spring configuration for Framer Motion:

```typescript
export const appleSpring = {
  type: 'spring' as const,
  stiffness: 400,
  damping: 32,
  mass: 0.8,
};

export const modalSpring = {
  type: 'spring' as const,
  stiffness: 350,
  damping: 28,
};
```

---

## 5. Component Contracts

### `FilePreviewModal`
```typescript
interface FilePreviewModalProps {
  isOpen: boolean;
  onClose: () => void;
  file: {
    id: string;
    name: string;
    sizeFormatted?: string;
    fileType?: string;
    createdAt?: string;
    directoryName?: string;
  } | null;
  onDownload?: () => void;
}
```

### `AppleSidebar`
```typescript
interface AppleSidebarProps {
  currentPath: string;
  onNavigate: (path: string) => void;
  isMobileOpen: boolean;
  onCloseMobile: () => void;
}
```


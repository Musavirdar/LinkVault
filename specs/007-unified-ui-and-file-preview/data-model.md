# Data Model & State Architecture: Unified Apple UI/UX & File Preview

**Feature**: `007-unified-ui-and-file-preview`
**Status**: Completed

---

## 1. File Entities & Preview Types

### `FileResource` (Domain Model & API Representation)
Represents a stored file resource managed by LinkVault:

| Field | Type | Description |
|---|---|---|
| `id` | `UUID` / `string` | Unique identifier of the resource |
| `name` | `string` | Display name of the file with extension |
| `type` | `string` | Resource classification (`file`, `image`, `document`, `archive`, `video`, `audio`) |
| `fileType` | `string` (optional) | Detected MIME type (e.g. `image/png`, `application/pdf`, `text/plain`) |
| `storagePath` | `string` | Internal disk path or storage key |
| `sizeInBytes` | `long` (optional) | Raw file size in bytes |
| `sizeFormatted` | `string` (optional) | Human-readable size (`1.4 MB`, `320 KB`) |
| `directoryId` | `UUID` / `string` (optional) | ID of containing directory (`null` for root) |
| `directoryName` | `string` (optional) | Name of containing directory |
| `downloadCount` | `int` | Number of times downloaded |
| `viewCount` | `int` | Number of times opened/previewed |
| `isPublic` | `bool` | Whether resource has public link access |
| `createdAt` | `DateTime` / `ISO 8601` | Upload timestamp |
| `lastAccessedAt`| `DateTime` (optional) | Last download/preview timestamp |

---

## 2. In-App Preview State Model

### `FilePreviewState` (Client State)
Managed by `useFilePreview` or local modal controller:

```typescript
export type FilePreviewCategory = 
  | 'image'      // PNG, JPG, GIF, WebP, SVG
  | 'pdf'        // PDF documents
  | 'text'       // TXT, JSON, MD, CSV, YAML, LOG, JS, TS, CS, PY, HTML, CSS
  | 'media'      // MP3, WAV, MP4, WebM
  | 'binary';    // ZIP, TAR, unknown binary formats

export interface FilePreviewState {
  isOpen: boolean;
  resource: FileResource | null;
  category: FilePreviewCategory;
  blobUrl: string | null;
  textContent: string | null;
  loading: boolean;
  error: string | null;
  zoomLevel: number;        // 0.5 to 3.0 (default: 1.0)
  rotation: number;         // 0, 90, 180, 270 degrees
}
```

### Format Detection Rules
```typescript
export function detectPreviewCategory(name: string, mimeType?: string): FilePreviewCategory {
  const ext = name.split('.').pop()?.toLowerCase() ?? '';
  const mime = (mimeType ?? '').toLowerCase();

  // Images
  if (['png', 'jpg', 'jpeg', 'gif', 'webp', 'svg', 'ico', 'bmp'].includes(ext) || mime.startsWith('image/')) {
    return 'image';
  }
  // PDF
  if (ext === 'pdf' || mime === 'application/pdf') {
    return 'pdf';
  }
  // Audio & Video
  if (['mp4', 'webm', 'ogg', 'mov', 'mp3', 'wav', 'm4a', 'aac', 'flac'].includes(ext) || mime.startsWith('video/') || mime.startsWith('audio/')) {
    return 'media';
  }
  // Code & Plaintext
  if (['txt', 'json', 'md', 'csv', 'yaml', 'yml', 'xml', 'log', 'js', 'jsx', 'ts', 'tsx', 'cs', 'py', 'java', 'html', 'css', 'sql', 'sh', 'ps1', 'env'].includes(ext) || mime.startsWith('text/') || mime === 'application/json') {
    return 'text';
  }
  // Fallback
  return 'binary';
}
```

---

## 3. Navigation & Layout State Model

### `AppleNavigationItem`
Represents an item within the unified Apple Sidebar:

```typescript
export interface AppleNavigationItem {
  id: string;
  label: string;
  icon: LucideIcon;
  path: string;
  badge?: string | number;
  roles?: string[]; // RBAC filter
}
```

Standard sidebar items:
- **Dashboard**: `Home` icon, `/dashboard`
- **Agent Workspace**: `Brain` icon, `/agent` (badge: "Meta")
- **Files**: `Files` icon, `/files`
- **Directories**: `Folder` icon, `/directories`
- **Links**: `Link2` icon, `/links`
- **Activity**: `History` icon, `/audit`
- **Preferences**: `Settings` icon, `/settings` (anchored to bottom)

---

## 4. Ticket Entity (EF Core Relational Schema)

### `DownloadTicket`
Stored in database table `DownloadTickets` with 60-second TTL:

| Column | Type | Constraints | Description |
|---|---|---|---|
| `Id` | `UUID` | Primary Key | Internal ticket ID |
| `Token` | `varchar(64)` | Indexed, Unique | High-entropy base64url token |
| `ResourceId` | `UUID` | Foreign Key (Resources.Id) | Target file to stream |
| `UserId` | `UUID` | Foreign Key (Users.Id) | User who generated ticket |
| `OrganizationId` | `UUID` | Indexed | Organization boundary check |
| `ExpiresAtUtc` | `timestamp with time zone` | Indexed | Ticket expiration timestamp |
| `IsUsed` | `boolean` | Default `false` | Single-use consumption flag |
| `CreatedAt` | `timestamp with time zone` | Default `now()` | Ticket generation timestamp |


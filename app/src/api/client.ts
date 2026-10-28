// In development: leave empty so Vite's proxy forwards /api/* to the backend (no CORS).
// In production: set VITE_API_URL to your real backend URL (e.g. https://api.example.com).
export const API_BASE_URL = import.meta.env.VITE_API_URL ?? '';

interface ApiOptions {
  method?: string;
  body?: unknown;
  headers?: Record<string, string>;
  requiresAuth?: boolean;
}

interface ApiResponse<T> {
  data?: T;
  error?: string;
  status: number;
}

export async function apiClient<T>(
  endpoint: string,
  options: ApiOptions = {}
): Promise<ApiResponse<T>> {
  const { method = 'GET', body, headers = {}, requiresAuth = true } = options;

  const url = `${API_BASE_URL}/api${endpoint}`;

  const requestHeaders: Record<string, string> = {
    'Content-Type': 'application/json',
    ...headers,
  };

  if (requiresAuth) {
    const token = localStorage.getItem('accessToken');
    if (token) {
      requestHeaders['Authorization'] = `Bearer ${token}`;
    }
  }

  try {
    const response = await fetch(url, {
      method,
      headers: requestHeaders,
      body: body ? JSON.stringify(body) : undefined,
    });

    const status = response.status;

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({}));
      const message = errorData.error || errorData.message || errorData.title
        || (response.status === 401 ? 'Please sign in again.' :
            response.status === 403 ? 'You don\'t have permission to do that.' :
            response.status === 404 ? 'Resource not found.' :
            response.status === 429 ? 'Too many requests. Please slow down.' :
            response.status >= 500 ? 'Server error. Please try again later.' :
            `Request failed (${response.status})`);
      return { error: message, status };
    }


    // Handle empty responses
    if (response.status === 204) {
      return { status };
    }

    const data = await response.json();
    return { data, status };
  } catch (error) {
    const msg = error instanceof Error ? error.message : String(error);
    // Map browser network errors to a friendly message
    const friendly = msg.includes('fetch') || msg.includes('network') || msg.includes('ERR_CONNECTION')
      ? 'Server unavailable. Make sure the backend is running.'
      : msg;
    return { error: friendly, status: 0 };
  }

}

// Auth API
export const authApi = {
  login: (emailOrUsername: string, password: string) =>
    apiClient<{
      userId: string;
      email: string;
      username: string;
      displayName?: string;
      avatarUrl?: string;
      preAuthToken?: string;
      requiresTwoFactor: boolean;
      token?: string;
      refreshToken?: string;
      expiresAt?: string;
      accountType: string;
      role?: string;
    }>('/auth/login', {
      method: 'POST',
      body: { emailOrUsername, password },
      requiresAuth: false,
    }),

  verify2FA: (preAuthToken: string, code: string) =>
    apiClient<{
      userId: string;
      email: string;
      username: string;
      displayName?: string;
      avatarUrl?: string;
      token: string;
      refreshToken: string;
      expiresAt: string;
      accountType: string;
      role?: string;
    }>('/auth/2fa/verify', {
      method: 'POST',
      body: { preAuthToken, code },
      requiresAuth: false,
    }),

  register: (data: {
    email: string;
    username: string;
    password: string;
    organizationName?: string;
    displayName?: string;
    bio?: string;
    isTeamAccount?: boolean;
  }) =>
    apiClient<{ userId: string; email: string; username: string; message: string }>(
      '/auth/register',
      {
        method: 'POST',
        body: data,
        requiresAuth: false,
      }
    ),

  getProfile: () =>
    apiClient<{
      id: string;
      email: string;
      username: string;
      displayName?: string;
      bio?: string;
      avatarUrl?: string;
      status: string;
      accountType: string;
      role?: string;
      plan?: string;
      isTwoFactorEnabled: boolean;
      createdAt: string;
    }>('/auth/profile'),

  updateProfile: (data: { displayName?: string; bio?: string; avatarUrl?: string }) =>
    apiClient('/auth/profile', {
      method: 'PUT',
      body: data,
    }),

  logout: () =>
    apiClient('/auth/logout', {
      method: 'POST',
    }),

  changePassword: (data: { currentPassword: string; newPassword: string }) =>
    apiClient('/auth/change-password', {
      method: 'POST',
      body: data,
    }),

  refreshToken: (refreshToken: string) =>
    apiClient<{ token: string; refreshToken: string; expiresAt: string }>(
      '/auth/refresh',
      {
        method: 'POST',
        body: { refreshToken },
        requiresAuth: false,
      }
    ),
};

// Directories API
export const directoriesApi = {
  getAll: (parentId?: string) =>
    apiClient<
      Array<{
        id: string;
        name: string;
        description?: string;
        icon?: string;
        color?: string;
        isPublic: boolean;
        exportRule: string;
        parentDirectoryId?: string;
        resourceCount: number;
        createdAt: string;
      }>
    >(`/directories${parentId ? `?parentId=${parentId}` : ''}`),

  getById: (id: string) =>
    apiClient<{
      id: string;
      name: string;
      description?: string;
      icon?: string;
      color?: string;
      isPublic: boolean;
      exportRule: string;
      parentDirectoryId?: string;
      resourceCount: number;
      createdAt: string;
    }>(`/directories/${id}`),

  create: (data: {
    name: string;
    description?: string;
    icon?: string;
    color?: string;
    isPublic?: boolean;
    exportRule?: string;
    parentDirectoryId?: string;
  }) =>
    apiClient('/directories', {
      method: 'POST',
      body: data,
    }),

  update: (id: string, data: Partial<{
    name: string;
    description?: string;
    icon?: string;
    color?: string;
    isPublic: boolean;
    exportRule: string;
  }>) =>
    apiClient(`/directories/${id}`, {
      method: 'PUT',
      body: data,
    }),

  delete: (id: string) =>
    apiClient(`/directories/${id}`, {
      method: 'DELETE',
    }),

  getResources: (id: string) =>
    apiClient<
      Array<{
        id: string;
        name: string;
        type: string;
        fileType?: string;
        url?: string;
        sizeFormatted?: string;
        viewCount: number;
        clickCount: number;
        isPublic: boolean;
        createdAt: string;
      }>
    >(`/directories/${id}/resources`),
};

// Resources API
export const resourcesApi = {
  getById: (id: string) =>
    apiClient<{
      id: string;
      name: string;
      type: string;
      fileType?: string;
      url?: string;
      sizeFormatted?: string;
      viewCount: number;
      clickCount: number;
      isPublic: boolean;
      createdAt: string;
    }>(`/resources/${id}`),

  create: (data: {
    name: string;
    type: string;
    description?: string;
    url?: string;
    isPublic?: boolean;
    directoryId: string;
  }) =>
    apiClient('/resources', {
      method: 'POST',
      body: data,
    }),

  update: (id: string, data: Partial<{ name: string; description?: string; isPublic: boolean }>) =>
    apiClient(`/resources/${id}`, {
      method: 'PUT',
      body: data,
    }),

  delete: (id: string) =>
    apiClient(`/resources/${id}`, {
      method: 'DELETE',
    }),

  trackClick: (id: string) =>
    apiClient(`/resources/${id}/click`, {
      method: 'POST',
      requiresAuth: false,
    }),
};

// Links API
export const linksApi = {
  getAll: () =>
    apiClient<
      Array<{
        id: string;
        title: string;
        url: string;
        description?: string;
        color?: string;
        icon?: string;
        linkType: string;
        embedHtml?: string;
        clickCount: number;
        isActive: boolean;
        isFeatured: boolean;
        isVisible: boolean;
        scheduledAt?: string;
        expiresAt?: string;
        hasCodeLock: boolean;
        animationStyle?: string;
        createdAt: string;
      }>
    >('/links'),

  create: (data: {
    title: string;
    url: string;
    description?: string;
    color?: string;
    icon?: string;
    linkType?: string;
    embedHtml?: string;
    isFeatured?: boolean;
    scheduledAt?: string;
    expiresAt?: string;
    codeLock?: string;
    animationStyle?: string;
  }) =>
    apiClient('/links', {
      method: 'POST',
      body: data,
    }),

  update: (id: string, data: Partial<{
    title: string;
    url: string;
    description?: string;
    color?: string;
    icon?: string;
    linkType: string;
    embedHtml?: string;
    isActive: boolean;
    isFeatured: boolean;
    isVisible: boolean;
    scheduledAt?: string;
    expiresAt?: string;
    codeLock?: string;
    animationStyle?: string;
  }>) =>
    apiClient(`/links/${id}`, {
      method: 'PUT',
      body: data,
    }),

  delete: (id: string) =>
    apiClient(`/links/${id}`, {
      method: 'DELETE',
    }),

  trackClick: (id: string) =>
    apiClient(`/links/${id}/click`, {
      method: 'POST',
      requiresAuth: false,
    }),

  reorder: (linkIds: string[]) =>
    apiClient('/links/reorder', {
      method: 'PUT',
      body: { linkIds },
    }),

  unlock: (id: string, code: string) =>
    apiClient<{ url: string; valid: boolean }>(`/links/${id}/unlock`, {
      method: 'POST',
      body: { code },
      requiresAuth: false,
    }),
};

// Audit API
export const auditApi = {
  getLogs: (filters?: {
    actionType?: string;
    entityType?: string;
    userId?: string;
    fromDate?: string;
    toDate?: string;
    page?: number;
    pageSize?: number;
  }) => {
    const params = new URLSearchParams();
    if (filters?.actionType) params.append('actionType', filters.actionType);
    if (filters?.entityType) params.append('entityType', filters.entityType);
    if (filters?.userId) params.append('userId', filters.userId);
    if (filters?.fromDate) params.append('fromDate', filters.fromDate);
    if (filters?.toDate) params.append('toDate', filters.toDate);
    if (filters?.page) params.append('page', filters.page.toString());
    if (filters?.pageSize) params.append('pageSize', filters.pageSize.toString());

    return apiClient<{
      items: Array<{
        id: string;
        actionType: string;
        entityType: string;
        entityName?: string;
        description?: string;
        userName?: string;
        createdAt: string;
      }>;
      totalCount: number;
      page: number;
      pageSize: number;
      totalPages: number;
    }>(`/audit?${params.toString()}`);
  },

  getSummary: () =>
    apiClient<{
      totalActionsToday: number;
      totalActionsThisWeek: number;
      totalActionsThisMonth: number;
      actionsByType: Record<string, number>;
      actionsByUser: Record<string, number>;
    }>('/audit/summary'),

  getRecent: (limit = 20) =>
    apiClient<
      Array<{
        id: string;
        actionType: string;
        entityType: string;
        entityName?: string;
        description?: string;
        userName?: string;
        createdAt: string;
      }>
    >(`/audit/recent?limit=${limit}`),
};

// Public API
export interface PublicProfileTheme {
  id: string;
  backgroundType: string;
  backgroundValue: string;
  buttonStyle: string;
  buttonColor: string;
  buttonTextColor: string;
  buttonHoverColor?: string;
  fontFamily: string;
  profileLayout: string;
  socialLinksPosition: string;
  customCSS?: string;
  hideBranding: boolean;
}

export interface PublicLink {
  id: string;
  title: string;
  url: string;
  description?: string;
  thumbnailUrl?: string;
  color?: string;
  icon?: string;
  linkType: string;
  embedHtml?: string;
  displayOrder: number;
  clickCount: number;
  isFeatured: boolean;
  hasCodeLock: boolean;
  animationStyle?: string;
}

export interface PublicProfileData {
  username: string;
  displayName?: string;
  bio?: string;
  avatarUrl?: string;
  publicPageSlug?: string;
  socialLinks?: Record<string, string>;
  theme?: PublicProfileTheme;
  links: PublicLink[];
}

export const publicApi = {
  getProfile: (slug: string) =>
    apiClient<PublicProfileData>(`/public/profile/${slug}`, { requiresAuth: false }),

  trackClick: (slug: string, linkId: string) =>
    apiClient(`/public/profile/${slug}/click/${linkId}`, {
      method: 'POST',
      requiresAuth: false,
    }),

  unlockLink: (slug: string, linkId: string, code: string) =>
    apiClient<{ url: string; valid: boolean }>(`/public/profile/${slug}/unlock/${linkId}`, {
      method: 'POST',
      body: { code },
      requiresAuth: false,
    }),

  getTheme: () =>
    apiClient<PublicProfileTheme | null>('/public/theme'),

  updateTheme: (data: Partial<PublicProfileTheme>) =>
    apiClient<PublicProfileTheme>('/public/theme', {
      method: 'PUT',
      body: data,
    }),

  updateSocialLinks: (socialLinks: Record<string, string>) =>
    apiClient('/public/social-links', {
      method: 'PUT',
      body: socialLinks,
    }),

  updateSlug: (slug: string) =>
    apiClient('/public/slug', {
      method: 'PUT',
      body: { slug },
    }),
};

// File Upload API (multipart/form-data)
export const fileUploadApi = {
  upload: async (file: File, directoryId?: string): Promise<ApiResponse<{
    id: string;
    name: string;
    url: string;
    mimeType: string;
    size: number;
    createdAt: string;
  }>> => {
    const token = localStorage.getItem('accessToken');
    const formData = new FormData();
    formData.append('file', file);
    if (directoryId) formData.append('directoryId', directoryId);

    try {
      const res = await fetch(`${API_BASE_URL}/api/fileupload`, {
        method: 'POST',
        headers: token ? { Authorization: `Bearer ${token}` } : {},
        body: formData,
      });
      const status = res.status;
      if (!res.ok) {
        const err = await res.json().catch(() => ({ error: 'Upload failed' }));
        return { error: err.error || `HTTP ${status}`, status };
      }
      const data = await res.json();
      return { data, status };
    } catch (e) {
      return { error: e instanceof Error ? e.message : 'Network error', status: 0 };
    }
  },
};

// Files API (Authenticated Download & Tickets)
export const filesApi = {
  createDownloadTicket: (resourceId: string) =>
    apiClient<{ ticket: string; expiresAt: string; downloadUrl: string }>(`/files/${resourceId}/ticket`, {
      method: 'POST',
    }),

  getDownloadUrl: async (resourceId: string): Promise<string> => {
    const res = await filesApi.createDownloadTicket(resourceId);
    if (res.data?.downloadUrl) {
      return `${API_BASE_URL}${res.data.downloadUrl}`;
    }
    return `${API_BASE_URL}/api/files/${resourceId}/download`;
  },

  downloadWithTicket: async (resourceId: string, fileName?: string) => {
    const url = await filesApi.getDownloadUrl(resourceId);
    const a = document.createElement('a');
    a.href = url;
    if (fileName) a.download = fileName;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
  },
};

// Share API
export const shareApi = {
  create: (data: { resourceId: string; password?: string; expiresAt?: string; maxDownloads?: number; allowPreview?: boolean; requireEmail?: boolean }) =>
    apiClient<{ shareId: string; token: string; url: string }>('/share', {
      method: 'POST',
      body: data,
    }),

  getMyShares: () =>
    apiClient<Array<{ id: string; token: string; fileName: string; expiresAt?: string; maxDownloads?: number; currentDownloads: number; hasPassword: boolean; createdAt: string }>>('/share/my'),

  revoke: (id: string) =>
    apiClient(`/share/${id}`, { method: 'DELETE' }),
};

// Chunked Upload API (Terabox-style)
export const chunkedUploadApi = {
  init: (data: { fileName: string; mimeType: string; totalSize: number; totalChunks: number; chunkSize: number; directoryId?: string }) =>
    apiClient<{ uploadId: string; chunkSize: number; totalChunks: number }>('/upload/init', {
      method: 'POST',
      body: data,
    }),

  uploadChunk: async (uploadId: string, chunkIndex: number, chunk: Blob): Promise<ApiResponse<{ chunkIndex: number; progress: number }>> => {
    const token = localStorage.getItem('accessToken');
    const formData = new FormData();
    formData.append('chunk', chunk, `chunk_${chunkIndex}`);
    try {
      const res = await fetch(`${API_BASE_URL}/api/upload/${uploadId}/chunk/${chunkIndex}`, {
        method: 'PUT',
        headers: token ? { Authorization: `Bearer ${token}` } : {},
        body: formData,
      });
      const status = res.status;
      if (!res.ok) {
        const err = await res.json().catch(() => ({ error: 'Chunk upload failed' }));
        return { error: err.error || `HTTP ${status}`, status };
      }
      const data = await res.json();
      return { data, status };
    } catch (e) {
      return { error: e instanceof Error ? e.message : 'Network error', status: 0 };
    }
  },

  status: (uploadId: string) =>
    apiClient<{ completedChunks: number[]; missingChunks: number[]; progress: number }>(`/upload/${uploadId}/status`),

  complete: (uploadId: string) =>
    apiClient<{ id: string; name: string; url: string; checksum: string }>(`/upload/${uploadId}/complete`, {
      method: 'POST',
    }),
};

// Search API
export const searchApi = {
  search: (q: string) =>
    apiClient<{
      query: string;
      totalResults: number;
      results: {
        links: Array<{ type: string; id: string; title: string; subtitle: string; icon: string }>;
        resources: Array<{ type: string; id: string; title: string; subtitle: string; icon: string }>;
        directories: Array<{ type: string; id: string; title: string; subtitle: string; icon: string }>;
      };
    }>(`/search?q=${encodeURIComponent(q)}`),
};

// Attendance API
export const attendanceApi = {
  getHeatmap: () =>
    apiClient<{
      days: Array<{ date: string; count: number }>;
      summary: {
        totalDaysActive: number;
        totalActions: number;
        maxSingleDay: number;
        currentStreak: number;
      };
    }>('/attendance'),

  getSummary: () =>
    apiClient<{
      totalDaysActive: number;
      totalActions: number;
      currentStreak: number;
      busiestDay: { date: string; count: number } | null;
    }>('/attendance/summary'),

  getOnlineMembers: () =>
    apiClient<
      Array<{
        id: string;
        displayName: string;
        username: string;
        avatarUrl?: string;
        isOnline: boolean;
        lastSeen?: string;
      }>
    >('/attendance/online'),
};

// Chat API
export const chatApi = {
  getOrgHistory: (orgId: string) =>
    apiClient<
      Array<{
        id: string;
        content: string;
        senderName: string;
        senderAvatarUrl?: string;
        createdAt: string;
        isEdited: boolean;
      }>
    >(`/chat/org/${orgId}/history`),
};

// ---------- JWT Permissions Parser ----------
export interface JwtPermissions {
  canUpload: boolean;
  canDelete: boolean;
  canManageUsers: boolean;
  canViewAuditLogs: boolean;
  accountType: string;
  orgId?: string;
  role?: string;
}

export function parseJwtPermissions(token: string): JwtPermissions {
  try {
    const payloadB64 = token.split('.')[1];
    // Pad base64 to multiple of 4
    const padded = payloadB64.replace(/-/g, '+').replace(/_/g, '/');
    const json = decodeURIComponent(
      atob(padded)
        .split('')
        .map(c => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
        .join('')
    );
    const payload = JSON.parse(json);
    return {
      canUpload: payload.canUpload === 'True' || payload.canUpload === true,
      canDelete: payload.canDelete === 'True' || payload.canDelete === true,
      canManageUsers: payload.canManageUsers === 'True' || payload.canManageUsers === true,
      canViewAuditLogs: payload.canViewAuditLogs === 'True' || payload.canViewAuditLogs === true,
      accountType: payload.accountType || 'Individual',
      orgId: payload.orgId,
      role: payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || payload.role,
    };
  } catch {
    return {
      canUpload: false,
      canDelete: false,
      canManageUsers: false,
      canViewAuditLogs: false,
      accountType: 'Individual',
    };
  }
}

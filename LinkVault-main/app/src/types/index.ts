export type Theme = 'dark' | 'light';

export type UserStatus = 'online' | 'away' | 'offline' | 'dnd';

export interface User {
  id: string;
  name: string;
  username: string;
  avatar: string;
  status: UserStatus;
  plan?: string;
  role?: string;
  accountType?: string;
}

export interface Permissions {
  canUpload: boolean;
  canDelete: boolean;
  canManageUsers: boolean;
  canViewAuditLogs: boolean;
  accountType: string; // 'Individual' | 'Team' | 'Enterprise'
}

export interface Workspace {
  id: string;
  name: string;
  icon: string;
  color?: string;
  isActive?: boolean;
}

export interface NavItem {
  id: string;
  label: string;
  icon: string;
  href: string;
  badge?: number;
}

export interface NavSection {
  id: string;
  label: string;
  items: NavItem[];
}

export interface Stat {
  label: string;
  value: number;
  subtext: string;
  change?: number;
  icon: string;
}

export interface FileItem {
  id: string;
  name: string;
  type: 'image' | 'document' | 'folder' | 'video' | 'audio';
  date: string;
  size?: string;
  thumbnail?: string;
}

export interface Activity {
  id: string;
  user: User;
  action: string;
  target: string;
  timestamp: string;
  type: 'upload' | 'view' | 'system' | 'share';
}

export interface LinkItem {
  id: string;
  title: string;
  url: string;
  clicks: number;
  isActive: boolean;
}

export interface Profile {
  username: string;
  displayName: string;
  bio: string;
  avatar: string;
  links: LinkItem[];
  socials: {
    twitter?: string;
    github?: string;
    linkedin?: string;
    instagram?: string;
  };
}

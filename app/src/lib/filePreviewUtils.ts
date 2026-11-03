export type FilePreviewCategory = 'image' | 'pdf' | 'text' | 'media' | 'binary';

export interface FileMetadata {
  id: string;
  name: string;
  sizeFormatted?: string;
  sizeInBytes?: number | null;
  fileType?: string;
  type?: string;
  createdAt?: string;
  directoryName?: string;
  url?: string;
}

const IMAGE_EXTENSIONS = new Set([
  'png', 'jpg', 'jpeg', 'webp', 'gif', 'svg', 'bmp', 'ico', 'avif',
]);

const PDF_EXTENSIONS = new Set(['pdf']);

const MEDIA_EXTENSIONS = new Set([
  'mp4', 'webm', 'ogg', 'mov', 'mp3', 'wav', 'm4a', 'aac', 'flac',
]);

const TEXT_CODE_EXTENSIONS = new Set([
  'txt', 'md', 'markdown', 'json', 'csv', 'yaml', 'yml', 'xml', 'log',
  'js', 'jsx', 'ts', 'tsx', 'cs', 'py', 'java', 'html', 'css', 'scss',
  'sql', 'sh', 'bash', 'ps1', 'env', 'config', 'ini', 'toml', 'rs', 'go',
  'cpp', 'c', 'h', 'hpp', 'rb', 'php',
]);

export function detectPreviewCategory(name: string, mimeType?: string): FilePreviewCategory {
  const ext = (name.split('.').pop() || '').toLowerCase();
  const mime = (mimeType || '').toLowerCase();

  if (IMAGE_EXTENSIONS.has(ext) || mime.startsWith('image/')) {
    return 'image';
  }

  if (PDF_EXTENSIONS.has(ext) || mime === 'application/pdf') {
    return 'pdf';
  }

  if (MEDIA_EXTENSIONS.has(ext) || mime.startsWith('video/') || mime.startsWith('audio/')) {
    return 'media';
  }

  if (TEXT_CODE_EXTENSIONS.has(ext) || mime.startsWith('text/') || mime === 'application/json' || mime.includes('javascript') || mime.includes('typescript')) {
    return 'text';
  }

  return 'binary';
}

export function getLanguageFromFilename(filename: string): string {
  const ext = (filename.split('.').pop() || '').toLowerCase();
  switch (ext) {
    case 'js':
    case 'jsx':
      return 'JavaScript';
    case 'ts':
    case 'tsx':
      return 'TypeScript';
    case 'cs':
      return 'C#';
    case 'py':
      return 'Python';
    case 'json':
      return 'JSON';
    case 'md':
    case 'markdown':
      return 'Markdown';
    case 'html':
      return 'HTML';
    case 'css':
    case 'scss':
      return 'CSS';
    case 'sql':
      return 'SQL';
    case 'yaml':
    case 'yml':
      return 'YAML';
    case 'sh':
    case 'bash':
      return 'Shell Script';
    case 'ps1':
      return 'PowerShell';
    case 'csv':
      return 'CSV';
    case 'xml':
      return 'XML';
    default:
      return 'Plain Text';
  }
}


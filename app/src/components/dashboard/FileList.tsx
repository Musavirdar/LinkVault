import { useState } from 'react';
import { 
  Image, 
  FileText, 
  Folder, 
  Video, 
  Music, 
  MoreHorizontal,
  Download,
  Share2,
  Trash2,
  Eye,
} from 'lucide-react';
import { cn } from '@/lib/utils';
import type { FileItem } from '@/types';
import { filesApi } from '@/api/client';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { FilePreviewModal } from '@/components/files/FilePreviewModal';
import { FileListSkeleton } from './DashboardSkeleton';

interface FileListProps {
  files: FileItem[];
  isLoading?: boolean;
}

const typeIcons: Record<FileItem['type'], typeof Image> = {
  image: Image,
  document: FileText,
  folder: Folder,
  video: Video,
  audio: Music,
};

const typeColors: Record<FileItem['type'], string> = {
  image: 'text-[#0071E3] dark:text-[#0A84FF]',
  document: 'text-[#FF9500] dark:text-[#FF9F0A]',
  folder: 'text-[#34C759] dark:text-[#30D158]',
  video: 'text-[#FF3B30] dark:text-[#FF453A]',
  audio: 'text-[#AF52DE] dark:text-[#BF5AF2]',
};

export function FileList({ files, isLoading = false }: FileListProps) {
  const [previewFile, setPreviewFile] = useState<FileItem | null>(null);

  if (isLoading) {
    return <FileListSkeleton />;
  }

  return (
    <>
      <div className="rounded-2xl overflow-hidden bg-card border border-black/[0.06] dark:border-white/10 shadow-[0_4px_20px_rgba(0,0,0,0.04)] dark:shadow-[0_8px_32px_rgba(0,0,0,0.35)] transition-all">
        {/* Header */}
        <div className="flex items-center justify-between px-4 py-3 border-b border-black/[0.06] dark:border-white/10">
          <h3 className="text-base font-semibold text-text-primary">
            Recent Files
          </h3>
          <span className="text-xs text-text-muted">
            {files.length} {files.length === 1 ? 'item' : 'items'}
          </span>
        </div>

        {/* Empty state */}
        {files.length === 0 ? (
          <div className="py-12 px-4 text-center">
            <div className="w-12 h-12 rounded-2xl bg-black/[0.04] dark:bg-white/10 mx-auto flex items-center justify-center text-text-muted mb-3">
              <Folder className="w-6 h-6 opacity-60" />
            </div>
            <p className="text-sm font-medium text-text-primary">No recent files</p>
            <p className="text-xs text-text-muted mt-1">Upload a file or ask the Agent to organize your workspace.</p>
          </div>
        ) : (
          /* Table */
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead>
                <tr className="text-left text-xs font-medium uppercase tracking-wider text-text-muted border-b border-black/[0.06] dark:border-white/10">
                  <th className="px-4 py-3">Name</th>
                  <th className="px-4 py-3">Type</th>
                  <th className="px-4 py-3">Date</th>
                  <th className="px-4 py-3">Size</th>
                  <th className="px-4 py-3 w-10"></th>
                </tr>
              </thead>
              <tbody className="divide-y divide-black/[0.03] dark:divide-white/5">
                {files.map((file) => {
                  const Icon = typeIcons[file.type] || FileText;
                  return (
                    <tr
                      key={file.id}
                      onClick={() => setPreviewFile(file)}
                      className="group cursor-pointer transition-colors duration-150 hover:bg-black/[0.025] dark:hover:bg-white/[0.05]"
                    >
                      <td className="px-4 py-3">
                        <div className="flex items-center gap-3">
                          <div className="w-8 h-8 rounded-lg flex items-center justify-center bg-black/[0.03] dark:bg-white/10 flex-shrink-0">
                            <Icon className={cn('w-4 h-4', typeColors[file.type] || 'text-[#0071E3]')} />
                          </div>
                          <div className="flex flex-col min-w-0">
                            <span className="text-sm font-medium text-text-primary truncate group-hover:text-[#0071E3] dark:group-hover:text-[#0A84FF] transition-colors">
                              {file.name}
                            </span>
                            {file.directoryName && (
                              <span className="text-[11px] text-[#0071E3] dark:text-[#0A84FF] font-medium flex items-center gap-1">
                                <Folder className="w-3 h-3" />
                                {file.directoryName}
                              </span>
                            )}
                          </div>
                        </div>
                      </td>
                      <td className="px-4 py-3">
                        <span className="text-sm text-text-secondary capitalize">
                          {file.type}
                        </span>
                      </td>
                      <td className="px-4 py-3">
                        <span className="text-sm text-text-secondary">
                          {file.date}
                        </span>
                      </td>
                      <td className="px-4 py-3">
                        <span className="text-sm text-text-secondary">
                          {file.size || '-'}
                        </span>
                      </td>
                      <td className="px-4 py-3 text-right" onClick={(e) => e.stopPropagation()}>
                        <div className="flex items-center justify-end gap-1">
                          <button
                            type="button"
                            onClick={() => setPreviewFile(file)}
                            className="p-1.5 rounded-lg text-text-muted hover:text-[#0071E3] dark:hover:text-[#0A84FF] hover:bg-black/[0.04] dark:hover:bg-white/10 transition-colors"
                            title="Preview file"
                          >
                            <Eye className="w-4 h-4" />
                          </button>
                          <button
                            type="button"
                            onClick={() => filesApi.downloadWithTicket(file.id, file.name)}
                            className="p-1.5 rounded-lg text-text-muted hover:text-[#0071E3] dark:hover:text-[#0A84FF] hover:bg-black/[0.04] dark:hover:bg-white/10 transition-colors"
                            title="Download file"
                          >
                            <Download className="w-4 h-4" />
                          </button>
                          <DropdownMenu>
                            <DropdownMenuTrigger asChild>
                              <button
                                type="button"
                                className="p-1.5 rounded-lg text-text-muted hover:text-text-primary hover:bg-black/[0.04] dark:hover:bg-white/10 transition-colors"
                              >
                                <MoreHorizontal className="w-4 h-4" />
                              </button>
                            </DropdownMenuTrigger>
                            <DropdownMenuContent align="end" className="w-40 rounded-xl">
                              <DropdownMenuItem onClick={() => setPreviewFile(file)}>
                                <Eye className="w-4 h-4 mr-2" />
                                Preview
                              </DropdownMenuItem>
                              <DropdownMenuItem onClick={() => filesApi.downloadWithTicket(file.id, file.name)}>
                                <Download className="w-4 h-4 mr-2" />
                                Download
                              </DropdownMenuItem>
                              <DropdownMenuItem>
                                <Share2 className="w-4 h-4 mr-2" />
                                Share
                              </DropdownMenuItem>
                              <DropdownMenuItem className="text-accent-danger">
                                <Trash2 className="w-4 h-4 mr-2" />
                                Delete
                              </DropdownMenuItem>
                            </DropdownMenuContent>
                          </DropdownMenu>
                        </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>

      <FilePreviewModal
        isOpen={!!previewFile}
        onClose={() => setPreviewFile(null)}
        file={previewFile}
        onDownload={() => previewFile && filesApi.downloadWithTicket(previewFile.id, previewFile.name)}
      />
    </>
  );
}

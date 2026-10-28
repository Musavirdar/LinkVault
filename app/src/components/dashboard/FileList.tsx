import { motion } from 'framer-motion';
import { 
  Image, 
  FileText, 
  Folder, 
  Video, 
  Music, 
  MoreHorizontal,
  Download,
  Share2,
  Trash2
} from 'lucide-react';
import { cn } from '@/lib/utils';
import { staggerContainer, staggerItem } from '@/lib/animations';
import type { FileItem } from '@/types';
import { filesApi } from '@/api/client';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';

interface FileListProps {
  files: FileItem[];
}

const typeIcons: Record<FileItem['type'], typeof Image> = {
  image: Image,
  document: FileText,
  folder: Folder,
  video: Video,
  audio: Music,
};

const typeColors: Record<FileItem['type'], string> = {
  image: 'text-accent-primary',
  document: 'text-accent-warning',
  folder: 'text-accent-success',
  video: 'text-accent-danger',
  audio: 'text-accent-secondary',
};

export function FileList({ files }: FileListProps) {
  return (
    <motion.div
      className="rounded-lg overflow-hidden bg-bg-secondary border border-border-subtle"
      variants={staggerContainer}
      initial="hidden"
      animate="visible"
    >
      {/* Header */}
      <div className="flex items-center px-4 py-3 border-b border-border-subtle">
        <h3 className="text-lg font-semibold text-text-primary">
          Recent Files
        </h3>
      </div>

      {/* Table */}
      <div className="overflow-x-auto">
        <table className="w-full">
          <thead>
            <tr className="text-left text-xs font-medium uppercase tracking-wider text-text-muted border-b border-border-subtle">
              <th className="px-4 py-3">Name</th>
              <th className="px-4 py-3">Type</th>
              <th className="px-4 py-3">Date</th>
              <th className="px-4 py-3">Size</th>
              <th className="px-4 py-3 w-10"></th>
            </tr>
          </thead>
          <tbody>
            {files.map((file) => {
              const Icon = typeIcons[file.type];
              return (
                <motion.tr
                  key={file.id}
                  className="group cursor-pointer transition-colors duration-200 hover:bg-bg-tertiary"
                  variants={staggerItem}
                  whileHover={{ x: 2 }}
                >
                  <td className="px-4 py-3">
                    <div className="flex items-center gap-3">
                      <div className="w-8 h-8 rounded-lg flex items-center justify-center bg-bg-tertiary">
                        <Icon className={cn('w-4 h-4', typeColors[file.type])} />
                      </div>
                      <span className="text-sm font-medium text-text-primary">
                        {file.name}
                      </span>
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
                  <td className="px-4 py-3">
                    <DropdownMenu>
                      <DropdownMenuTrigger asChild>
                        <button className="p-1.5 rounded-md opacity-0 group-hover:opacity-100 transition-opacity duration-200 hover:bg-bg-floating text-text-muted hover:text-text-primary">
                          <MoreHorizontal className="w-4 h-4" />
                        </button>
                      </DropdownMenuTrigger>
                      <DropdownMenuContent align="end">
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
                  </td>
                </motion.tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </motion.div>
  );
}

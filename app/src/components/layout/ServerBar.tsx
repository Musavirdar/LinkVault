import { motion } from 'framer-motion';
import { ServerIcon } from '@/components/ui/ServerIcon';
import { useStore } from '@/store/useStore';
import { cn } from '@/lib/utils';
import { fadeInLeft } from '@/lib/animations';

export function ServerBar() {
  const { workspaces, activeWorkspace, setActiveWorkspace } = useStore();

  const handleWorkspaceClick = (id: string) => {
    setActiveWorkspace(id);
  };

  return (
    <motion.div
      className={cn(
        'w-[68px] h-full flex-shrink-0',
        'flex flex-col items-center py-4',
        'bg-white/70 dark:bg-[#1C1C1E]/70 backdrop-blur-2xl border-r border-black/[0.06] dark:border-white/10',
        'custom-scrollbar overflow-y-auto'
      )}
      variants={fadeInLeft}
      initial="hidden"
      animate="visible"
    >
      {/* Logo */}
      <div className="mb-3">
        <div className="
          w-11 h-11 rounded-2xl
          flex items-center justify-center
          bg-[#0071E3] text-white shadow-sm shadow-[#0071E3]/25
        ">
          <svg viewBox="0 0 24 24" className="w-5 h-5" fill="currentColor">
            <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-1 17.93c-3.95-.49-7-3.85-7-7.93 0-.62.08-1.21.21-1.79L9 15v1c0 1.1.9 2 2 2v1.93zm6.9-2.54c-.26-.81-1-1.39-1.9-1.39h-1v-3c0-.55-.45-1-1-1H8v-2h2c.55 0 1-.45 1-1V7h2c1.1 0 2-.9 2-2v-.41c2.93 1.19 5 4.06 5 7.41 0 2.08-.8 3.97-2.1 5.39z"/>
          </svg>
        </div>
      </div>

      <div className="w-8 h-px bg-black/[0.06] dark:bg-white/10 mb-3" />

      {/* Workspace icons */}
      <div className="flex-1 flex flex-col items-center gap-2.5">
        {workspaces.map((workspace) => (
          <ServerIcon
            key={workspace.id}
            icon={workspace.icon}
            color={workspace.color}
            isActive={activeWorkspace === workspace.id}
            onClick={() => handleWorkspaceClick(workspace.id)}
            label={workspace.name}
          />
        ))}
        
        <ServerIcon
          icon="plus"
          isAddButton
          onClick={() => {}}
        />
      </div>

      {/* Bottom actions */}
      <div className="flex flex-col items-center gap-2.5 mt-auto pt-3">
        <div className="w-8 h-px bg-black/[0.06] dark:bg-white/10 mb-1" />
        <ServerIcon
          icon="settings"
          isSettings
          onClick={() => {}}
        />
      </div>
    </motion.div>
  );
}

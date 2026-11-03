import { motion } from 'framer-motion';
import { useNavigate } from 'react-router-dom';
import { ServerIcon } from '@/components/ui/ServerIcon';
import { LinkVaultLogo } from '@/components/ui/LinkVaultLogo';
import { useStore } from '@/store/useStore';
import { cn } from '@/lib/utils';
import { fadeInLeft } from '@/lib/animations';

export function ServerBar() {
  const navigate = useNavigate();
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
      {/* Brand Logo */}
      <div className="mb-3 cursor-pointer" onClick={() => navigate('/dashboard')} title="LinkVault Dashboard">
        <LinkVaultLogo size={44} />
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

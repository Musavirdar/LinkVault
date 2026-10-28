import { useEffect, useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { PanelRightClose, PanelRightOpen, Loader2 } from 'lucide-react';
import { ActivityItem } from '@/components/activity/ActivityItem';
import { OnlineList } from '@/components/activity/OnlineList';
import { useStore } from '@/store/useStore';
import { auditApi, attendanceApi } from '@/api/client';
import { cn } from '@/lib/utils';
import { activityPanelVariants } from '@/lib/animations';
import type { Activity, User } from '@/types';
import { formatDistanceToNow } from 'date-fns';


// Map audit action types to Activity types
function mapActionToType(actionType: string): Activity['type'] {
  const m: Record<string, Activity['type']> = {
    Uploaded: 'upload', Created: 'upload', Viewed: 'view',
    Updated: 'upload', Deleted: 'system', Login: 'system', Logout: 'system',
  };
  return m[actionType] ?? 'system';
}

function useRecentActivity() {
  const [activities, setActivities] = useState<Activity[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    auditApi.getRecent(15).then((res) => {
      const items: Activity[] = (res.data ?? []).map((log) => ({
        id: log.id,
        user: {
          id: log.id,
          name: log.userName ?? 'System',
          username: (log.userName ?? 'system').toLowerCase().replace(/\s+/g, ''),
          avatar: log.userName
            ? `https://api.dicebear.com/7.x/avataaars/svg?seed=${encodeURIComponent(log.userName)}`
            : '',
          status: 'online' as const,
        },
        action: log.actionType,
        target: log.entityName ?? log.description ?? log.entityType ?? '',
        timestamp: formatDistanceToNow(new Date(log.createdAt), { addSuffix: true }),
        type: mapActionToType(log.actionType),
      }));
      setActivities(items);
      setLoading(false);
    });
  }, []);

  return { activities, loading };
}

function useOnlineMembers() {
  const [members, setMembers] = useState<User[]>([]);

  useEffect(() => {
    attendanceApi.getOnlineMembers().then((res) => {
      const mapped: User[] = (res.data ?? []).map((m) => ({
        id: m.id,
        name: m.displayName,
        username: m.username,
        avatar: m.avatarUrl ?? `https://api.dicebear.com/7.x/avataaars/svg?seed=${encodeURIComponent(m.username)}`,
        status: (m.isOnline ? 'online' : 'offline') as User['status'],
      }));
      setMembers(mapped);
    });
  }, []);

  return members;
}


export function ActivityPanel() {
  const { activityPanelCollapsed, toggleActivityPanel } = useStore();
  const { activities, loading } = useRecentActivity();
  const onlineMembers = useOnlineMembers();


  const feedContent = (compact = false) => (
    loading ? (
      <div className="flex items-center justify-center py-8">
        <Loader2 className="w-5 h-5 animate-spin text-[var(--accent-primary)]" />
      </div>
    ) : activities.length === 0 ? (
      <p className="text-xs text-[var(--text-muted)] text-center py-6">No recent activity.</p>
    ) : (
      activities.map((activity) => (
        <ActivityItem key={activity.id} activity={activity} compact={compact} />
      ))
    )
  );

  // Dark/Light: collapsible right panel
  return (
    <motion.aside
      className={cn('h-full flex-shrink-0 flex flex-row bg-bg-secondary border-l border-border-subtle')}
      variants={activityPanelVariants}
      initial="expanded"
      animate={activityPanelCollapsed ? 'collapsed' : 'expanded'}
    >
      {/* Toggle button rail */}
      <div className="w-12 flex-shrink-0 flex flex-col items-center pt-4">
        <motion.button
          onClick={toggleActivityPanel}
          className="p-1.5 rounded-md text-text-muted hover:text-text-primary hover:bg-bg-tertiary transition-colors duration-200"
          whileHover={{ scale: 1.05 }}
          whileTap={{ scale: 0.95 }}
          title={activityPanelCollapsed ? 'Open Activity Feed' : 'Close Activity Feed'}
        >
          {activityPanelCollapsed
            ? <PanelRightOpen className="w-4 h-4" />
            : <PanelRightClose className="w-4 h-4" />}
        </motion.button>
      </div>

      {/* Collapsible content */}
      <AnimatePresence>
        {!activityPanelCollapsed && (
          <motion.div
            className="flex-1 flex flex-col overflow-hidden min-w-0"
            initial={{ opacity: 0, x: 16 }}
            animate={{ opacity: 1, x: 0 }}
            exit={{ opacity: 0, x: 16 }}
            transition={{ duration: 0.2, ease: [0.4, 0, 0.2, 1] }}
          >
            <div className="flex items-center px-4 py-4 border-b border-border-subtle">
              <h3 className="text-sm font-semibold text-text-primary uppercase tracking-wider">Activity Feed</h3>
            </div>

            <div className="flex-1 overflow-y-auto custom-scrollbar">
              <div className="p-4 space-y-4">
                {feedContent(false)}
              </div>
              <div className="mt-2 pt-4 border-t border-border-subtle">
                <div className="px-4 pb-3">
                  <h4 className="text-sm font-semibold text-text-primary uppercase tracking-wider">
                    Online · {onlineMembers.filter(m => m.status === 'online').length}
                  </h4>
                </div>
                <div className="px-4 pb-4">
                  <OnlineList members={onlineMembers} />
                </div>
              </div>

            </div>
          </motion.div>
        )}
      </AnimatePresence>
    </motion.aside>
  );
}

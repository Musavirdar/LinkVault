import { useState, useEffect, useRef } from 'react';

interface UseCountUpOptions {
  start?: number;
  end: number;
  duration?: number;
  delay?: number;
  decimals?: number;
  enabled?: boolean;
}

export function useCountUp({
  start = 0,
  end,
  duration = 800,
  delay = 0,
  decimals = 0,
  enabled = true,
}: UseCountUpOptions) {
  const [count, setCount] = useState(start);
  const countRef = useRef(start);
  const startTimeRef = useRef<number | null>(null);
  const rafRef = useRef<number | null>(null);

  useEffect(() => {
    if (!enabled) {
      // eslint-disable-next-line react-hooks/set-state-in-effect
      setCount(start);
      return;
    }

    const timeout = setTimeout(() => {
      const animate = (timestamp: number) => {
        if (!startTimeRef.current) {
          startTimeRef.current = timestamp;
        }

        const progress = Math.min((timestamp - startTimeRef.current) / duration, 1);
        
        // Easing function (ease-out)
        const easeOut = 1 - Math.pow(1 - progress, 3);
        
        const currentCount = start + (end - start) * easeOut;
        countRef.current = currentCount;
        setCount(Number(currentCount.toFixed(decimals)));

        if (progress < 1) {
          rafRef.current = requestAnimationFrame(animate);
        }
      };

      rafRef.current = requestAnimationFrame(animate);
    }, delay);

    return () => {
      clearTimeout(timeout);
      if (rafRef.current) {
        cancelAnimationFrame(rafRef.current);
      }
      startTimeRef.current = null;
    };
  }, [start, end, duration, delay, decimals, enabled]);

  return count;
}

export function useFormattedCountUp(options: UseCountUpOptions & { format?: 'number' | 'compact' | 'bytes' }) {
  const { format = 'number', ...countUpOptions } = options;
  const count = useCountUp(countUpOptions);

  const formatNumber = (num: number): string => {
    switch (format) {
      case 'compact':
        if (num >= 1000000) return `${(num / 1000000).toFixed(1)}M`;
        if (num >= 1000) return `${(num / 1000).toFixed(1)}k`;
        return num.toLocaleString();
      case 'bytes':
        if (num >= 1073741824) return `${(num / 1073741824).toFixed(1)} GB`;
        if (num >= 1048576) return `${(num / 1048576).toFixed(1)} MB`;
        if (num >= 1024) return `${(num / 1024).toFixed(1)} KB`;
        return `${num} B`;
      default:
        return num.toLocaleString();
    }
  };

  return formatNumber(count);
}

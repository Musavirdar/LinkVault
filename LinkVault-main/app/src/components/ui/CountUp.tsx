import { useCountUp, useFormattedCountUp } from '@/hooks/useCountUp';

interface CountUpProps {
  end: number;
  start?: number;
  duration?: number;
  delay?: number;
  decimals?: number;
  className?: string;
  format?: 'number' | 'compact' | 'bytes';
  suffix?: string;
  prefix?: string;
}

export function CountUp({
  end,
  start = 0,
  duration = 800,
  delay = 0,
  decimals = 0,
  className = '',
  format = 'number',
  suffix = '',
  prefix = '',
}: CountUpProps) {
  const count = useFormattedCountUp({
    start,
    end,
    duration,
    delay,
    decimals,
    format,
    enabled: true,
  });

  return (
    <span className={className}>
      {prefix}{count}{suffix}
    </span>
  );
}

// Simple number count up without formatting
export function CountUpNumber({
  end,
  start = 0,
  duration = 800,
  delay = 0,
  decimals = 0,
  className = '',
}: Omit<CountUpProps, 'format' | 'suffix' | 'prefix'>) {
  const count = useCountUp({
    start,
    end,
    duration,
    delay,
    decimals,
    enabled: true,
  });

  return (
    <span className={className}>
      {count.toLocaleString()}
    </span>
  );
}

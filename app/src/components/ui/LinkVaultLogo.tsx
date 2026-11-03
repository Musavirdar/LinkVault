import React from 'react';

interface LinkVaultLogoProps {
  className?: string;
  size?: number;
  showText?: boolean;
  textSize?: string;
}

export const LinkVaultLogo: React.FC<LinkVaultLogoProps> = ({
  className = '',
  size = 36,
  showText = false,
  textSize = 'text-[17px]',
}) => {
  return (
    <div className={`inline-flex items-center gap-2.5 select-none ${className}`}>
      <div
        className="relative flex items-center justify-center rounded-2xl bg-gradient-to-br from-[#0071E3] via-[#0077ED] to-[#0051B3] text-white shadow-md shadow-[#0071E3]/25 overflow-hidden flex-shrink-0 transition-transform duration-200 hover:scale-105"
        style={{ width: size, height: size }}
      >
        <svg
          viewBox="0 0 32 32"
          fill="none"
          xmlns="http://www.w3.org/2000/svg"
          className="w-[62%] h-[62%]"
        >
          {/* Subtle background glow */}
          <circle cx="16" cy="16" r="12" fill="white" fillOpacity="0.08" />
          
          {/* Vault Outer Shield / Arch */}
          <path
            d="M16 4L6 8.2V16.5C6 22.8 10.3 28.5 16 30C21.7 28.5 26 22.8 26 16.5V8.2L16 4Z"
            stroke="white"
            strokeWidth="2.2"
            strokeLinejoin="round"
            fill="white"
            fillOpacity="0.16"
          />

          {/* Interlocking Secure Link inside the Shield */}
          <path
            d="M12.5 13.5H15.5C17.15 13.5 18.5 14.85 18.5 16.5C18.5 18.15 17.15 19.5 15.5 19.5H12.5C10.85 19.5 9.5 18.15 9.5 16.5C9.5 14.85 10.85 13.5 12.5 13.5Z"
            stroke="white"
            strokeWidth="2"
            strokeLinecap="round"
          />
          <path
            d="M19.5 13.5H16.5"
            stroke="white"
            strokeWidth="2"
            strokeLinecap="round"
          />
          <circle cx="16" cy="16.5" r="1.5" fill="white" />
        </svg>
      </div>

      {showText && (
        <span className={`font-semibold tracking-tight text-foreground ${textSize}`}>
          Link<span className="text-[#0071E3] dark:text-[#0A84FF]">Vault</span>
        </span>
      )}
    </div>
  );
};


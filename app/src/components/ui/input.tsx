import * as React from "react"

import { cn } from "@/lib/utils"

function Input({ className, type, ...props }: React.ComponentProps<"input">) {
  return (
    <input
      type={type}
      data-slot="input"
      className={cn(
        "file:text-foreground placeholder:text-muted-foreground selection:bg-primary selection:text-white dark:bg-white/[0.05] border-black/[0.08] dark:border-white/12 h-11 min-h-[44px] w-full min-w-0 rounded-xl border bg-black/[0.02] px-3.5 py-2 text-sm shadow-xs transition-all duration-150 outline-none file:inline-flex file:h-7 file:border-0 file:bg-transparent file:text-sm file:font-medium disabled:pointer-events-none disabled:cursor-not-allowed disabled:opacity-40",
        "focus-visible:border-[#0071E3] focus-visible:ring-4 focus-visible:ring-[#0071E3]/15",
        "aria-invalid:ring-destructive/30 aria-invalid:border-destructive",
        className
      )}
      {...props}
    />
  )
}

export { Input }

import * as React from "react"
import { Slot } from "@radix-ui/react-slot"
import { cva, type VariantProps } from "class-variance-authority"

import { cn } from "@/lib/utils"

const buttonVariants = cva(
  "inline-flex items-center justify-center gap-2 whitespace-nowrap rounded-xl text-sm font-medium transition-all duration-150 active:scale-[0.98] disabled:pointer-events-none disabled:opacity-40 [&_svg]:pointer-events-none [&_svg:not([class*='size-'])]:size-4 shrink-0 [&_svg]:shrink-0 outline-none focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2 aria-invalid:ring-destructive/30 aria-invalid:border-destructive select-none",
  {
    variants: {
      variant: {
        default:
          "bg-[#0071E3] text-white hover:bg-[#0077ED] active:bg-[#0066CC] shadow-sm shadow-[#0071E3]/20",
        destructive:
          "bg-[#FF3B30] text-white hover:bg-[#FF453A] active:bg-[#D70015] shadow-sm shadow-red-500/20",
        outline:
          "border border-black/[0.08] dark:border-white/15 bg-white/80 dark:bg-white/[0.06] backdrop-blur-md text-foreground shadow-xs hover:bg-black/[0.04] dark:hover:bg-white/10 active:bg-black/[0.08] dark:active:bg-white/15",
        secondary:
          "bg-black/[0.05] dark:bg-white/10 text-foreground hover:bg-black/[0.08] dark:hover:bg-white/15 active:bg-black/[0.12] dark:active:bg-white/20",
        ghost:
          "text-foreground hover:bg-black/[0.05] dark:hover:bg-white/10 active:bg-black/[0.09] dark:active:bg-white/15",
        link:
          "text-[#0071E3] dark:text-[#0A84FF] underline-offset-4 hover:underline active:opacity-80 p-0 h-auto",
      },
      size: {
        default: "min-h-[44px] h-11 px-4 py-2.5 rounded-xl has-[>svg]:px-3.5",
        sm: "min-h-[36px] h-9 rounded-lg gap-1.5 px-3 text-xs has-[>svg]:px-2.5",
        lg: "min-h-[44px] h-12 rounded-xl px-6 text-base font-medium has-[>svg]:px-4",
        icon: "min-h-[44px] size-11 rounded-xl",
        "icon-sm": "min-h-[36px] size-9 rounded-lg",
        "icon-lg": "min-h-[48px] size-12 rounded-xl",
      },
    },
    defaultVariants: {
      variant: "default",
      size: "default",
    },
  }
)

function Button({
  className,
  variant = "default",
  size = "default",
  asChild = false,
  ...props
}: React.ComponentProps<"button"> &
  VariantProps<typeof buttonVariants> & {
    asChild?: boolean
  }) {
  const Comp = asChild ? Slot : "button"

  return (
    <Comp
      data-slot="button"
      data-variant={variant}
      data-size={size}
      className={cn(buttonVariants({ variant, size, className }))}
      {...props}
    />
  )
}

export { Button, buttonVariants }

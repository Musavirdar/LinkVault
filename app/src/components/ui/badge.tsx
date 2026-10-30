import * as React from "react"
import { Slot } from "@radix-ui/react-slot"
import { cva, type VariantProps } from "class-variance-authority"

import { cn } from "@/lib/utils"

const badgeVariants = cva(
  "inline-flex items-center justify-center rounded-full border px-2.5 py-0.5 text-[11px] font-semibold tracking-tight w-fit whitespace-nowrap shrink-0 [&>svg]:size-3 gap-1 [&>svg]:pointer-events-none focus-visible:ring-2 focus-visible:ring-[#0071E3] transition-all duration-150 overflow-hidden",
  {
    variants: {
      variant: {
        default:
          "border-blue-500/20 bg-blue-500/10 text-[#0071E3] dark:text-[#0A84FF] [a&]:hover:bg-blue-500/20",
        secondary:
          "border-black/[0.06] dark:border-white/10 bg-black/[0.04] dark:bg-white/10 text-foreground [a&]:hover:bg-black/[0.08] dark:[a&]:hover:bg-white/15",
        destructive:
          "border-red-500/20 bg-red-500/10 text-[#FF3B30] dark:text-[#FF453A] [a&]:hover:bg-red-500/20 focus-visible:ring-red-500",
        outline:
          "border-black/[0.1] dark:border-white/15 text-foreground bg-transparent [a&]:hover:bg-black/[0.04] dark:[a&]:hover:bg-white/10",
      },
    },
    defaultVariants: {
      variant: "default",
    },
  }
)

function Badge({
  className,
  variant,
  asChild = false,
  ...props
}: React.ComponentProps<"span"> &
  VariantProps<typeof badgeVariants> & { asChild?: boolean }) {
  const Comp = asChild ? Slot : "span"

  return (
    <Comp
      data-slot="badge"
      className={cn(badgeVariants({ variant }), className)}
      {...props}
    />
  )
}

export { Badge, badgeVariants }

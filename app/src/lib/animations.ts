import type { Variants } from 'framer-motion';

// Easing functions as tuples
const easeDefault: [number, number, number, number] = [0.4, 0, 0.2, 1];
const easeTheme: [number, number, number, number] = [0.4, 0, 0.2, 1];

// Fade in from bottom
export const fadeInUp: Variants = {
  hidden: {
    opacity: 0,
    y: 20
  },
  visible: {
    opacity: 1,
    y: 0,
    transition: {
      duration: 0.4,
      ease: easeDefault
    }
  }
};

// Fade in from left
export const fadeInLeft: Variants = {
  hidden: {
    opacity: 0,
    x: -20
  },
  visible: {
    opacity: 1,
    x: 0,
    transition: {
      duration: 0.4,
      ease: easeDefault
    }
  }
};

// Fade in from right
export const fadeInRight: Variants = {
  hidden: {
    opacity: 0,
    x: 20
  },
  visible: {
    opacity: 1,
    x: 0,
    transition: {
      duration: 0.4,
      ease: easeDefault
    }
  }
};

// Simple fade in
export const fadeIn: Variants = {
  hidden: {
    opacity: 0
  },
  visible: {
    opacity: 1,
    transition: {
      duration: 0.3,
      ease: easeDefault
    }
  }
};

// Scale in
export const scaleIn: Variants = {
  hidden: {
    opacity: 0,
    scale: 0.95
  },
  visible: {
    opacity: 1,
    scale: 1,
    transition: {
      duration: 0.2,
      ease: easeDefault
    }
  }
};

// Stagger container
export const staggerContainer: Variants = {
  hidden: {
    opacity: 0
  },
  visible: {
    opacity: 1,
    transition: {
      staggerChildren: 0.1,
      delayChildren: 0.1
    }
  }
};

// Stagger item
export const staggerItem: Variants = {
  hidden: {
    opacity: 0,
    y: 20
  },
  visible: {
    opacity: 1,
    y: 0,
    transition: {
      duration: 0.4,
      ease: easeDefault
    }
  }
};

// Sidebar variants
export const sidebarVariants: Variants = {
  expanded: {
    width: 240,
    transition: {
      duration: 0.3,
      ease: easeDefault
    }
  },
  collapsed: {
    width: 72,
    transition: {
      duration: 0.3,
      ease: easeDefault
    }
  }
};

// Activity panel variants
export const activityPanelVariants: Variants = {
  expanded: {
    width: 280,
    transition: {
      duration: 0.3,
      ease: easeDefault
    }
  },
  collapsed: {
    width: 48,
    transition: {
      duration: 0.3,
      ease: easeDefault
    }
  }
};

// Server bar variants
export const serverBarVariants: Variants = {
  vertical: {
    width: 72,
    height: '100%',
    flexDirection: 'column',
    transition: {
      duration: 0.6,
      ease: easeTheme
    }
  },
  horizontal: {
    width: '100%',
    height: 80,
    flexDirection: 'row',
    transition: {
      duration: 0.6,
      ease: easeTheme
    }
  }
};

// Layout morph variants
export const layoutMorphVariants: Variants = {
  vertical: {
    flexDirection: 'row',
    transition: {
      duration: 0.6,
      ease: easeTheme
    }
  },
  ultra: {
    flexDirection: 'column',
    transition: {
      duration: 0.6,
      ease: easeTheme
    }
  }
};

// Card hover
export const cardHover = {
  rest: {
    y: 0,
    boxShadow: '0 0 0 rgba(0,0,0,0)'
  },
  hover: {
    y: -2,
    boxShadow: '0 4px 12px rgba(0,0,0,0.1)',
    transition: {
      duration: 0.2,
      ease: easeDefault
    }
  }
};

// Button hover
export const buttonHover = {
  rest: {
    scale: 1,
    y: 0
  },
  hover: {
    scale: 1.02,
    y: -1,
    transition: {
      duration: 0.2,
      ease: easeDefault
    }
  },
  tap: {
    scale: 0.98,
    transition: {
      duration: 0.1
    }
  }
};

// Slide in from right (for activity items)
export const slideInRight: Variants = {
  hidden: {
    x: 20,
    opacity: 0
  },
  visible: {
    x: 0,
    opacity: 1,
    transition: {
      duration: 0.3,
      ease: easeDefault
    }
  },
  exit: {
    x: 20,
    opacity: 0,
    transition: {
      duration: 0.2
    }
  }
};

// Page load sequence
export const pageLoadSequence = {
  container: {
    hidden: { opacity: 0 },
    visible: {
      opacity: 1,
      transition: {
        staggerChildren: 0.1,
        delayChildren: 0.2
      }
    }
  },
  item: {
    hidden: { opacity: 0, y: 20 },
    visible: {
      opacity: 1,
      y: 0,
      transition: {
        duration: 0.4,
        ease: easeDefault
      }
    }
  }
};

// Number count up animation config
export const countUpConfig = {
  duration: 0.8,
  ease: easeDefault
};

// Theme transition
export const themeTransition = {
  duration: 0.6,
  ease: easeTheme
};

import type { ThemeOptions } from '@mui/material';
import { useShell } from '@kododo-dev/way-shell';
import { MONO_FONT } from './utils/fonts.ts';

/** CultureWay's look on top of the shell's theme: monospace type and its own greys. */
export function cultureWayTheme(mode: 'light' | 'dark'): ThemeOptions {
  return {
    palette: mode === 'dark'
      ? { background: { default: '#1a1a1a', paper: '#222' } }
      : { background: { default: '#f5f5f5', paper: '#fafafa' } },
    typography: { fontFamily: MONO_FONT },
  };
}

/** The theme mode the shell shows; switching it is the shell's job. */
export const useThemeMode = () => ({ mode: useShell().themeMode });

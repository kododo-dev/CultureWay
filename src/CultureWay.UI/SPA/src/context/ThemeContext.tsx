import React, { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import { createTheme, type Theme } from '@mui/material/styles';
import { MONO_FONT } from '../utils/fonts.ts';

type ThemeMode = 'dark' | 'light';

interface ThemeModeContextValue {
  mode: ThemeMode;
  toggleMode: () => void;
  muiTheme: Theme;
}

const ThemeModeContext = createContext<ThemeModeContextValue>({
  mode: 'dark',
  toggleMode: () => {},
  muiTheme: createTheme({ palette: { mode: 'dark' } }),
});

// Shared with the host's own pages: they live on the same origin, so they can read the same key.
const THEME_STORAGE_KEY = 'cultureway.theme';

function initialMode(): ThemeMode {
  try {
    const stored = localStorage.getItem(THEME_STORAGE_KEY);
    if (stored === 'dark' || stored === 'light') return stored;
  } catch { /* storage blocked */ }
  return window.matchMedia?.('(prefers-color-scheme: light)').matches ? 'light' : 'dark';
}

export const ThemeContextProvider = ({ children }: { children: React.ReactNode }) => {
  const [mode, setMode] = useState<ThemeMode>(initialMode);

  useEffect(() => {
    document.documentElement.setAttribute('data-theme', mode);
  }, [mode]);

  const toggleMode = useCallback(() => {
    setMode(prev => {
      const next = prev === 'dark' ? 'light' : 'dark';
      try { localStorage.setItem(THEME_STORAGE_KEY, next); } catch { /* storage blocked */ }
      return next;
    });
  }, []);

  const muiTheme = useMemo(
    () =>
      createTheme({
        palette: {
          mode,
          ...(mode === 'dark'
            ? { background: { default: '#1a1a1a', paper: '#222' } }
            : { background: { default: '#f5f5f5', paper: '#fafafa' } }),
        },
        typography: {
          fontFamily: MONO_FONT,
        },
      }),
    [mode],
  );

  return (
    <ThemeModeContext.Provider value={{ mode, toggleMode, muiTheme }}>
      {children}
    </ThemeModeContext.Provider>
  );
};

export const useThemeMode = () => useContext(ThemeModeContext);

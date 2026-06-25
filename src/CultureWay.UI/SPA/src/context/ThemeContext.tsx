import React, { createContext, useCallback, useContext, useMemo, useState } from 'react';
import { createTheme, type Theme } from '@mui/material/styles';

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

export const ThemeContextProvider = ({ children }: { children: React.ReactNode }) => {
  const [mode, setMode] = useState<ThemeMode>('dark');

  const toggleMode = useCallback(() => {
    setMode(prev => {
      const next = prev === 'dark' ? 'light' : 'dark';
      document.documentElement.setAttribute('data-theme', next);
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
          fontFamily: "'IBM Plex Mono', 'Fira Code', monospace",
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

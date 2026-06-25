import React, { useState, useCallback } from 'react';
import Drawer from '@mui/material/Drawer';
import Box from '@mui/material/Box';
import Typography from '@mui/material/Typography';
import IconButton from '@mui/material/IconButton';
import Tooltip from '@mui/material/Tooltip';
import useMediaQuery from '@mui/material/useMediaQuery';
import MenuIcon from '@mui/icons-material/Menu';
import DarkModeIcon from '@mui/icons-material/DarkMode';
import LightModeIcon from '@mui/icons-material/LightMode';
import TranslateIcon from '@mui/icons-material/Translate';
import { useThemeMode } from '../../context/ThemeContext.tsx';
import { useI18n } from '../../i18n/I18nContext.tsx';
import { useTheme } from '@mui/material/styles';

const DRAWER_WIDTH = 248;

const DrawerContent = ({ onClose }: { onClose?: () => void }) => {
  const { mode, toggleMode } = useThemeMode();
  const { t, lang, setLang } = useI18n();
  const theme = useTheme();
  const isDark = mode === 'dark';
  const borderColor = isDark ? '#2e2e2e' : '#e0e0e0';

  void onClose;

  return (
    <>
      <Box sx={{
        px: 2.5, py: 2,
        borderBottom: `1px solid ${borderColor}`,
        display: 'flex',
        alignItems: 'center',
        gap: 1,
        flexShrink: 0,
      }}>
        <TranslateIcon sx={{ fontSize: 18, color: theme.palette.primary.main }} />
        <Typography sx={{
          flex: 1,
          fontSize: '0.9rem',
          fontWeight: 700,
          color: theme.palette.text.primary,
          letterSpacing: '0.05em',
          lineHeight: 1.2,
          fontFamily: "'IBM Plex Mono', monospace",
        }}>
          {t.appTitle}
        </Typography>
        <Tooltip title={isDark ? t.switchToLight : t.switchToDark}>
          <IconButton
            size="small"
            onClick={toggleMode}
            sx={{ color: theme.palette.text.secondary, '&:hover': { color: theme.palette.primary.main } }}
          >
            {isDark ? <LightModeIcon sx={{ fontSize: 16 }} /> : <DarkModeIcon sx={{ fontSize: 16 }} />}
          </IconButton>
        </Tooltip>
      </Box>

      <Box sx={{ px: 2.5, py: 2, borderBottom: `1px solid ${borderColor}` }}>
        <Typography sx={{ fontSize: '0.7rem', color: theme.palette.text.secondary, mb: 1, textTransform: 'uppercase', letterSpacing: '0.08em' }}>
          UI language
        </Typography>
        <Box sx={{ display: 'flex', gap: 1 }}>
          {['en', 'pl'].map(l => (
            <Box
              key={l}
              onClick={() => setLang(l)}
              sx={{
                px: 1.5, py: 0.5,
                borderRadius: 1,
                cursor: 'pointer',
                fontSize: '0.8rem',
                fontWeight: lang === l ? 700 : 400,
                background: lang === l ? theme.palette.primary.main : 'transparent',
                color: lang === l ? '#fff' : theme.palette.text.secondary,
                '&:hover': { background: lang === l ? theme.palette.primary.dark : theme.palette.action.hover },
              }}
            >
              {l}
            </Box>
          ))}
        </Box>
      </Box>

      <Box sx={{ px: 2.5, py: 2 }}>
        <Typography sx={{ fontSize: '0.75rem', color: theme.palette.text.disabled, lineHeight: 1.6 }}>
          Edit and manage localized strings across all supported cultures.
          Changes are applied instantly — no restart required.
        </Typography>
      </Box>
    </>
  );
};

const MainLayout = ({ children }: { children: React.ReactNode }) => {
  const { mode } = useThemeMode();
  const theme = useTheme();
  const isDark = mode === 'dark';
  const isMobile = useMediaQuery(theme.breakpoints.down('md'));
  const [mobileOpen, setMobileOpen] = useState(false);
  const openMobileMenu = useCallback(() => setMobileOpen(true), []);
  const closeMobileMenu = useCallback(() => setMobileOpen(false), []);

  const drawerBg    = isDark ? '#222' : '#fafafa';
  const borderColor = isDark ? '#2e2e2e' : '#e0e0e0';
  const bg          = isDark ? '#1a1a1a' : '#f5f5f5';

  const paperSx = {
    width: DRAWER_WIDTH,
    boxSizing: 'border-box' as const,
    background: drawerBg,
    borderRight: `1px solid ${borderColor}`,
    color: theme.palette.text.primary,
    display: 'flex',
    flexDirection: 'column' as const,
    height: '100%',
    overflow: 'hidden',
  };

  return (
    <Box sx={{ display: 'flex', height: '100vh', overflow: 'hidden', background: bg }}>
      {isMobile ? (
        <>
          <Box sx={{
            position: 'fixed', top: 0, left: 0, zIndex: 1200,
            p: 1,
          }}>
            <IconButton onClick={openMobileMenu} sx={{ color: theme.palette.text.primary }}>
              <MenuIcon />
            </IconButton>
          </Box>
          <Drawer
            variant="temporary"
            open={mobileOpen}
            onClose={closeMobileMenu}
            ModalProps={{ keepMounted: true }}
            sx={{ [`& .MuiDrawer-paper`]: paperSx }}
          >
            <DrawerContent onClose={closeMobileMenu} />
          </Drawer>
        </>
      ) : (
        <Drawer
          variant="permanent"
          sx={{ width: DRAWER_WIDTH, flexShrink: 0, [`& .MuiDrawer-paper`]: paperSx }}
        >
          <DrawerContent />
        </Drawer>
      )}

      <Box component="main" sx={{
        flexGrow: 1, minWidth: 0,
        display: 'flex', flexDirection: 'column',
        height: '100vh', overflow: 'hidden',
        background: bg,
        pt: isMobile ? 6 : 0,
      }}>
        {children}
      </Box>
    </Box>
  );
};

export default MainLayout;

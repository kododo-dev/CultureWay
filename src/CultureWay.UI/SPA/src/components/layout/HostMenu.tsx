import Box from '@mui/material/Box';
import Divider from '@mui/material/Divider';
import IconButton from '@mui/material/IconButton';
import Link from '@mui/material/Link';
import List from '@mui/material/List';
import ListItem from '@mui/material/ListItem';
import ListItemButton from '@mui/material/ListItemButton';
import ListItemIcon from '@mui/material/ListItemIcon';
import ListItemText from '@mui/material/ListItemText';
import Tooltip from '@mui/material/Tooltip';
import Typography from '@mui/material/Typography';
import ArrowOutwardIcon from '@mui/icons-material/ArrowOutward';
import HomeOutlinedIcon from '@mui/icons-material/HomeOutlined';
import KeyOutlinedIcon from '@mui/icons-material/KeyOutlined';
import PeopleOutlinedIcon from '@mui/icons-material/PeopleOutlined';
import TranslateIcon from '@mui/icons-material/Translate';
import LogoutIcon from '@mui/icons-material/Logout';
import PersonOutlinedIcon from '@mui/icons-material/PersonOutlined';
import { useTheme } from '@mui/material/styles';
import { useEditorSettings } from '../../context/EditorSettingsContext.tsx';
import { useI18n } from '../../i18n/I18nContext.tsx';
import { MONO_FONT } from '../../utils/fonts.ts';

const ICONS: Record<string, typeof ArrowOutwardIcon> = {
  home: HomeOutlinedIcon,
  translations: TranslateIcon,
  users: PeopleOutlinedIcon,
  key: KeyOutlinedIcon,
};

const trimSlash = (path: string) => path.replace(/\/+$/, '') || '/';

// True when the link points at the page the editor is on, so it is shown as the current one.
const isCurrent = (url: string) => {
  try {
    return trimSlash(new URL(url, document.baseURI).pathname) === trimSlash(window.location.pathname);
  } catch {
    return false;
  }
};

// The host application's links and the signed-in user, at the bottom of the side menu.
// Renders nothing when the host set neither.
const HostMenu = () => {
  const { links, user } = useEditorSettings();
  const { t } = useI18n();
  const theme = useTheme();
  const isDark = theme.palette.mode === 'dark';
  const borderColor = isDark ? '#2e2e2e' : '#e0e0e0';
  const mutedColor = isDark ? '#666' : '#aaa';

  if (links.length === 0 && !user) return null;

  return (
    <Box sx={{ flexShrink: 0, borderTop: `1px solid ${borderColor}` }}>
      {links.length > 0 && (
        <List dense disablePadding sx={{ py: 1 }}>
          {links.map(link => {
            const current = isCurrent(link.url);
            const Icon = (link.icon && ICONS[link.icon]) || ArrowOutwardIcon;
            return (
              <ListItem key={link.url} disablePadding>
                <ListItemButton component="a" href={link.url} selected={current} aria-current={current ? 'page' : undefined}>
                  <ListItemIcon sx={{ color: current ? theme.palette.primary.main : mutedColor, minWidth: 32, '& svg': { fontSize: 16 } }}>
                    <Icon />
                  </ListItemIcon>
                  <ListItemText
                    primary={link.label}
                    sx={{
                      '& .MuiListItemText-primary': {
                        fontSize: '0.8rem',
                        fontFamily: MONO_FONT,
                        color: current ? theme.palette.text.primary : theme.palette.text.secondary,
                        fontWeight: current ? 600 : 400,
                      },
                    }}
                  />
                </ListItemButton>
              </ListItem>
            );
          })}
        </List>
      )}

      {links.length > 0 && user && <Divider sx={{ borderColor, mx: 2 }} />}

      {user && (
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, px: 2.5, py: 1.5 }}>
          <PersonOutlinedIcon sx={{ fontSize: 16, color: mutedColor }} />
          <Typography noWrap sx={{ flex: 1, minWidth: 0, fontSize: '0.8rem', fontFamily: MONO_FONT }}>
            {user.accountUrl
              ? <Link href={user.accountUrl} underline="hover" color="inherit">{user.name}</Link>
              : user.name}
          </Typography>
          {user.signOutUrl && (
            <form method="post" action={user.signOutUrl} style={{ margin: 0 }}>
              <Tooltip title={t.signOut}>
                <IconButton
                  type="submit"
                  size="small"
                  aria-label={t.signOut}
                  sx={{ color: theme.palette.text.secondary, '&:hover': { color: theme.palette.primary.main } }}
                >
                  <LogoutIcon sx={{ fontSize: 16 }} />
                </IconButton>
              </Tooltip>
            </form>
          )}
        </Box>
      )}
    </Box>
  );
};

export default HostMenu;

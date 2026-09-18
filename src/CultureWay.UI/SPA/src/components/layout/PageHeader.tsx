import Box from '@mui/material/Box';
import Typography from '@mui/material/Typography';
import IconButton from '@mui/material/IconButton';
import InputBase from '@mui/material/InputBase';
import Tooltip from '@mui/material/Tooltip';
import Button from '@mui/material/Button';
import CircularProgress from '@mui/material/CircularProgress';
import Chip from '@mui/material/Chip';
import SearchIcon from '@mui/icons-material/Search';
import CloseIcon from '@mui/icons-material/Close';
import SaveIcon from '@mui/icons-material/Save';
import AddIcon from '@mui/icons-material/Add';
import LanguageIcon from '@mui/icons-material/Language';
import MenuIcon from '@mui/icons-material/Menu';
import useMediaQuery from '@mui/material/useMediaQuery';
import { useTheme } from '@mui/material/styles';
import { useThemeMode } from '../../context/ThemeContext.tsx';
import { useI18n } from '../../i18n/I18nContext.tsx';
import { useLayout } from '../../context/LayoutContext.tsx';

const MONO = { fontFamily: "'IBM Plex Mono', monospace" };

interface PageHeaderProps {
  namespaceLabel: string;
  searchQuery: string;
  onSearchChange: (q: string) => void;
  dirtyCount: number;
  saving: boolean;
  onSave: () => void;
  onDiscard: () => void;
  onAddKey: () => void;
  onManageLanguages: () => void;
}

const PageHeader = ({
  namespaceLabel,
  searchQuery,
  onSearchChange,
  dirtyCount,
  saving,
  onSave,
  onDiscard,
  onAddKey,
  onManageLanguages,
}: PageHeaderProps) => {
  const theme = useTheme();
  const { mode } = useThemeMode();
  const { t } = useI18n();
  const { openMobileMenu } = useLayout();
  const isMobile = useMediaQuery(theme.breakpoints.down('md'));
  const isDark = mode === 'dark';

  const borderColor = isDark ? '#2e2e2e' : '#e0e0e0';
  const headerBg    = isDark ? '#1e1e1e' : '#fff';
  const searchBg    = isDark ? '#2a2a2a' : '#f5f5f5';
  const searchBorder = isDark ? '#363636' : '#ddd';

  const hasChanges = dirtyCount > 0;

  return (
    <Box sx={{ flexShrink: 0 }}>
      <Box sx={{
        display: 'flex',
        alignItems: 'center',
        px: 3, py: 1.5,
        borderBottom: `1px solid ${borderColor}`,
        background: headerBg,
        gap: 1.5,
        minHeight: 59,
      }}>
        {isMobile && (
          <IconButton
            size="small"
            onClick={openMobileMenu}
            sx={{ color: theme.palette.text.secondary, flexShrink: 0, mr: 0.5, '&:hover': { color: theme.palette.primary.main } }}
          >
            <MenuIcon sx={{ fontSize: 20 }} />
          </IconButton>
        )}

        <Box sx={{ minWidth: 0, flexShrink: 0 }}>
          <Typography sx={{
            ...MONO,
            fontSize: '1rem',
            fontWeight: 700,
            color: theme.palette.text.primary,
            lineHeight: 1.2,
            whiteSpace: 'nowrap',
          }}>
            {namespaceLabel}
          </Typography>
        </Box>

        <Box sx={{
          flex: 1,
          mx: 2,
          display: 'flex',
          alignItems: 'center',
          gap: 0.75,
          background: searchBg,
          border: `1px solid ${searchQuery ? theme.palette.primary.main : searchBorder}`,
          borderRadius: '6px',
          px: 1.25, py: 0.5,
          transition: 'border-color 0.15s',
          '&:focus-within': {
            borderColor: theme.palette.primary.main,
            background: isDark ? '#2e2e2e' : '#fff',
          },
        }}>
          <SearchIcon sx={{ fontSize: 15, color: isDark ? '#555' : '#bbb', flexShrink: 0 }} />
          <InputBase
            value={searchQuery}
            onChange={e => onSearchChange(e.target.value)}
            placeholder={t.searchPlaceholder}
            fullWidth
            sx={{
              ...MONO,
              fontSize: '0.78rem',
              color: theme.palette.text.primary,
              '& input': { p: 0 },
              '& input::placeholder': { color: isDark ? '#444' : '#bbb', fontStyle: 'italic', opacity: 1 },
            }}
          />
          {searchQuery && (
            <IconButton
              size="small"
              onClick={() => onSearchChange('')}
              sx={{ p: 0.25, color: isDark ? '#555' : '#bbb', '&:hover': { color: theme.palette.text.primary } }}
            >
              <CloseIcon sx={{ fontSize: 13 }} />
            </IconButton>
          )}
        </Box>

        {hasChanges && (
          <>
            <Chip
              label={`${dirtyCount} ${t.unsavedChanges}`}
              size="small"
              color="warning"
              variant="outlined"
              sx={{ ...MONO, fontSize: '0.7rem', flexShrink: 0 }}
            />
            <Button
              size="small"
              onClick={onDiscard}
              sx={{
                ...MONO,
                fontSize: '0.72rem',
                textTransform: 'none',
                color: isDark ? '#666' : '#999',
                flexShrink: 0,
                '&:hover': { color: theme.palette.text.primary },
              }}
            >
              {t.discard}
            </Button>
          </>
        )}

        <Button
          size="small"
          variant="outlined"
          startIcon={saving
            ? <CircularProgress size={11} sx={{ color: 'inherit' }} />
            : <SaveIcon sx={{ fontSize: '13px !important' }} />
          }
          onClick={onSave}
          disabled={saving || !hasChanges}
          sx={{
            ...MONO,
            fontSize: '0.72rem',
            textTransform: 'none',
            flexShrink: 0,
            borderColor: hasChanges ? theme.palette.primary.main : (isDark ? '#2e2e2e' : '#ddd'),
            color: hasChanges ? theme.palette.primary.main : (isDark ? '#3a3a3a' : '#ccc'),
            '&:hover:not(.Mui-disabled)': {
              borderColor: theme.palette.primary.main,
              background: isDark ? 'rgba(144,202,249,0.06)' : 'rgba(25,118,210,0.06)',
            },
            '&.Mui-disabled': {
              borderColor: isDark ? '#2a2a2a' : '#e0e0e0',
              color: isDark ? '#3a3a3a' : '#ccc',
            },
            transition: 'border-color 0.15s, color 0.15s',
          }}
        >
          {saving ? t.saving : t.save}
        </Button>

        <Tooltip title={t.manageLanguages}>
          <IconButton
            size="small"
            onClick={onManageLanguages}
            sx={{
              color: theme.palette.text.secondary,
              border: `1px solid ${isDark ? '#2e2e2e' : '#ddd'}`,
              borderRadius: '6px',
              p: 0.5,
              flexShrink: 0,
              '&:hover': { color: theme.palette.primary.main, borderColor: theme.palette.primary.main },
            }}
          >
            <LanguageIcon sx={{ fontSize: 18 }} />
          </IconButton>
        </Tooltip>

        <Tooltip title={t.addKey}>
          <IconButton
            size="small"
            onClick={onAddKey}
            sx={{
              color: theme.palette.primary.main,
              border: `1px solid ${theme.palette.primary.main}`,
              borderRadius: '6px',
              p: 0.5,
              flexShrink: 0,
              '&:hover': { background: isDark ? 'rgba(144,202,249,0.1)' : 'rgba(25,118,210,0.06)' },
            }}
          >
            <AddIcon sx={{ fontSize: 18 }} />
          </IconButton>
        </Tooltip>
      </Box>
    </Box>
  );
};

export default PageHeader;

import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Typography from '@mui/material/Typography';
import IconButton from '@mui/material/IconButton';
import Tooltip from '@mui/material/Tooltip';
import DeleteIcon from '@mui/icons-material/Delete';
import RestoreIcon from '@mui/icons-material/Restore';
import UndoIcon from '@mui/icons-material/Undo';
import CloseIcon from '@mui/icons-material/Close';
import { useTheme } from '@mui/material/styles';
import { useThemeMode } from '../../context/ThemeContext.tsx';
import { useI18n } from '../../i18n/I18nContext.tsx';

const MONO = { fontFamily: "'IBM Plex Mono', monospace" };

interface SelectionToolbarProps {
  selectedCount: number;
  deletableCount: number;
  resettableCount: number;
  restorableCount: number;
  onDelete: () => void;
  onReset: () => void;
  onRestore: () => void;
  onClear: () => void;
}

const SelectionToolbar = ({
  selectedCount,
  deletableCount,
  resettableCount,
  restorableCount,
  onDelete,
  onReset,
  onRestore,
  onClear,
}: SelectionToolbarProps) => {
  const theme = useTheme();
  const { mode } = useThemeMode();
  const { t } = useI18n();
  const isDark = mode === 'dark';

  const borderColor = isDark ? '#2e2e2e' : '#e0e0e0';
  const bg          = isDark ? '#1a2a3a' : '#eef5ff';

  const buttonSx = {
    ...MONO,
    fontSize: '0.72rem',
    textTransform: 'none' as const,
  };

  return (
    <Box sx={{
      display: 'flex',
      alignItems: 'center',
      gap: 1,
      px: 3, py: 1,
      borderBottom: `1px solid ${borderColor}`,
      background: bg,
      flexShrink: 0,
    }}>
      <Typography sx={{ ...MONO, fontSize: '0.78rem', color: theme.palette.text.primary, fontWeight: 600 }}>
        {selectedCount} {t.selected}
      </Typography>

      <Box sx={{ flex: 1 }} />

      <Button
        size="small"
        variant="outlined"
        color="error"
        disabled={deletableCount === 0}
        onClick={onDelete}
        startIcon={<DeleteIcon sx={{ fontSize: '14px !important' }} />}
        sx={buttonSx}
      >
        {t.deleteSelected} ({deletableCount})
      </Button>

      <Button
        size="small"
        variant="outlined"
        color="info"
        disabled={resettableCount === 0}
        onClick={onReset}
        startIcon={<RestoreIcon sx={{ fontSize: '14px !important' }} />}
        sx={buttonSx}
      >
        {t.resetSelected} ({resettableCount})
      </Button>

      <Button
        size="small"
        variant="outlined"
        color="warning"
        disabled={restorableCount === 0}
        onClick={onRestore}
        startIcon={<UndoIcon sx={{ fontSize: '14px !important' }} />}
        sx={buttonSx}
      >
        {t.restoreSelected} ({restorableCount})
      </Button>

      <Tooltip title={t.clearSelection}>
        <IconButton size="small" onClick={onClear} sx={{ color: theme.palette.text.secondary }}>
          <CloseIcon sx={{ fontSize: 16 }} />
        </IconButton>
      </Tooltip>
    </Box>
  );
};

export default SelectionToolbar;

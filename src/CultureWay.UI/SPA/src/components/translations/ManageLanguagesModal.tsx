import { useState } from 'react';
import Dialog from '@mui/material/Dialog';
import DialogTitle from '@mui/material/DialogTitle';
import DialogContent from '@mui/material/DialogContent';
import DialogActions from '@mui/material/DialogActions';
import Button from '@mui/material/Button';
import TextField from '@mui/material/TextField';
import CircularProgress from '@mui/material/CircularProgress';
import List from '@mui/material/List';
import ListItem from '@mui/material/ListItem';
import Typography from '@mui/material/Typography';
import Divider from '@mui/material/Divider';
import IconButton from '@mui/material/IconButton';
import Tooltip from '@mui/material/Tooltip';
import Box from '@mui/material/Box';
import StarIcon from '@mui/icons-material/Star';
import StarBorderIcon from '@mui/icons-material/StarBorder';
import VisibilityIcon from '@mui/icons-material/Visibility';
import VisibilityOffIcon from '@mui/icons-material/VisibilityOff';
import DeleteIcon from '@mui/icons-material/Delete';
import { useTheme } from '@mui/material/styles';
import { useThemeMode } from '../../context/ThemeContext.tsx';
import { useTranslations } from '../../context/TranslationsContext.tsx';
import { useI18n } from '../../i18n/I18nContext.tsx';
import { addCulture, deleteCulture, setDefaultCulture } from '../../api/api.ts';
import type { CultureDto } from '../../api/api.model.ts';
import CultureFlag from './CultureFlag.tsx';

interface ManageLanguagesModalProps {
  open: boolean;
  cultures: CultureDto[];
  onChanged: () => void;
  onClose: () => void;
}

const MONO = { fontFamily: "'IBM Plex Mono', monospace" };

const ManageLanguagesModal = ({ open, cultures, onChanged, onClose }: ManageLanguagesModalProps) => {
  const theme = useTheme();
  const { mode } = useThemeMode();
  const { t } = useI18n();
  const { hiddenCultures, toggleCultureVisibility } = useTranslations();
  const isDark = mode === 'dark';

  const [code, setCode] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [addError, setAddError] = useState<string | null>(null);
  const [busyCode, setBusyCode] = useState<string | null>(null);
  const [rowError, setRowError] = useState<string | null>(null);
  const [prevOpen, setPrevOpen] = useState(open);

  if (open !== prevOpen) {
    setPrevOpen(open);
    if (open) {
      setCode('');
      setAddError(null);
      setRowError(null);
    }
  }

  const existingCultures = cultures.map(c => c.code.toLowerCase());
  const trimmed = code.trim();
  const isDuplicate = trimmed.length > 0 && existingCultures.includes(trimmed.toLowerCase());
  const isValid = trimmed.length > 0 && !isDuplicate;

  const handleAdd = async () => {
    if (!isValid) return;
    setSubmitting(true);
    setAddError(null);
    try {
      const errors = await addCulture(trimmed);
      if (errors.length > 0) {
        setAddError(errors[0]);
        return;
      }
      setCode('');
      onChanged();
    } catch (e) {
      setAddError(e instanceof Error ? e.message : 'Failed to add language');
    } finally {
      setSubmitting(false);
    }
  };

  const handleSetDefault = async (cultureCode: string) => {
    setBusyCode(cultureCode);
    setRowError(null);
    try {
      const errors = await setDefaultCulture(cultureCode);
      if (errors.length > 0) {
        setRowError(errors[0]);
        return;
      }
      onChanged();
    } catch (e) {
      setRowError(e instanceof Error ? e.message : 'Failed to set default language');
    } finally {
      setBusyCode(null);
    }
  };

  const handleDelete = async (cultureCode: string) => {
    if (!window.confirm(t.confirmDeleteLanguage)) return;
    setBusyCode(cultureCode);
    setRowError(null);
    try {
      const errors = await deleteCulture(cultureCode);
      if (errors.length > 0) {
        setRowError(errors[0]);
        return;
      }
      onChanged();
    } catch (e) {
      setRowError(e instanceof Error ? e.message : 'Failed to delete language');
    } finally {
      setBusyCode(null);
    }
  };

  const borderColor = isDark ? '#2e2e2e' : '#e0e0e0';
  const rowHover = isDark ? '#252525' : '#fafafa';

  return (
    <Dialog
      open={open}
      onClose={onClose}
      maxWidth="sm"
      fullWidth
      slotProps={{
        paper: {
          sx: {
            background: isDark ? '#222' : '#fff',
            border: `1px solid ${borderColor}`,
            borderRadius: 2,
          },
        },
      }}
    >
      <DialogTitle sx={{ ...MONO, fontSize: '0.95rem', fontWeight: 700, pb: 1 }}>
        {t.manageLanguages}
      </DialogTitle>

      <DialogContent sx={{ pt: '8px !important', display: 'flex', flexDirection: 'column', gap: 1.5 }}>
        <Typography sx={{
          fontSize: '0.68rem',
          color: theme.palette.text.secondary,
          textTransform: 'uppercase',
          letterSpacing: '0.08em',
          ...MONO,
        }}>
          {t.languagesListTitle}
        </Typography>

        <List dense disablePadding sx={{ border: `1px solid ${borderColor}`, borderRadius: 1, overflow: 'hidden' }}>
          {cultures.map((c, i) => (
            <ListItem
              key={c.code}
              disablePadding
              sx={{
                px: 1.5, py: 0.75,
                borderTop: i === 0 ? 'none' : `1px solid ${borderColor}`,
                display: 'flex',
                alignItems: 'center',
                gap: 1,
                '&:hover': { background: rowHover },
              }}
            >
              <CultureFlag code={c.code} size={14} />
              <Typography sx={{
                ...MONO, fontSize: '0.82rem', flex: 1,
                opacity: hiddenCultures.has(c.code) ? 0.45 : 1,
              }}>
                {c.code}
              </Typography>

              <Tooltip title={hiddenCultures.has(c.code) ? t.showLanguage : t.hideLanguage}>
                <IconButton
                  size="small"
                  onClick={() => toggleCultureVisibility(c.code)}
                  sx={{ p: 0.4, color: theme.palette.text.disabled, '&:hover': { color: theme.palette.primary.main } }}
                >
                  {hiddenCultures.has(c.code)
                    ? <VisibilityOffIcon sx={{ fontSize: 16 }} />
                    : <VisibilityIcon sx={{ fontSize: 16 }} />}
                </IconButton>
              </Tooltip>

              {c.isDefault ? (
                <Tooltip title={t.defaultLanguage}>
                  <Box sx={{ p: 0.4, display: 'inline-flex' }}>
                    <StarIcon sx={{ fontSize: 16, color: '#fbc02d' }} />
                  </Box>
                </Tooltip>
              ) : (
                <Tooltip title={t.setAsDefault}>
                  <span>
                    <IconButton
                      size="small"
                      disabled={busyCode === c.code}
                      onClick={() => void handleSetDefault(c.code)}
                      sx={{ p: 0.4, color: theme.palette.text.disabled, '&:hover': { color: theme.palette.primary.main } }}
                    >
                      {busyCode === c.code ? <CircularProgress size={14} /> : <StarBorderIcon sx={{ fontSize: 16 }} />}
                    </IconButton>
                  </span>
                </Tooltip>
              )}
              <Tooltip title={t.deleteLanguage}>
                <span>
                  <IconButton
                    size="small"
                    disabled={c.isDefault || busyCode === c.code}
                    onClick={() => void handleDelete(c.code)}
                    sx={{ p: 0.4, color: theme.palette.text.disabled, '&:hover': { color: theme.palette.error.main } }}
                  >
                    <DeleteIcon sx={{ fontSize: 16 }} />
                  </IconButton>
                </span>
              </Tooltip>
            </ListItem>
          ))}
        </List>

        {rowError && (
          <Typography sx={{ fontSize: '0.75rem', color: theme.palette.error.main }}>
            {rowError}
          </Typography>
        )}

        <Divider sx={{ borderColor, mt: 0.5 }} />

        <Typography sx={{
          fontSize: '0.68rem',
          color: theme.palette.text.secondary,
          textTransform: 'uppercase',
          letterSpacing: '0.08em',
          ...MONO,
        }}>
          {t.addNewLanguage}
        </Typography>

        <Box sx={{ display: 'flex', alignItems: 'flex-start', gap: 1 }}>
          <TextField
            label={t.cultureCodeLabel}
            placeholder={t.cultureCodePlaceholder}
            value={code}
            onChange={e => setCode(e.target.value)}
            onKeyDown={e => { if (e.key === 'Enter') void handleAdd(); }}
            size="small"
            fullWidth
            error={isDuplicate || addError !== null}
            helperText={isDuplicate ? t.duplicateLanguage : (addError ?? undefined)}
            slotProps={{
              inputLabel: { sx: { ...MONO, fontSize: '0.82rem' } },
              input: { sx: { ...MONO, fontSize: '0.82rem' } },
            }}
          />
          <Button
            onClick={() => void handleAdd()}
            disabled={!isValid || submitting}
            variant="contained"
            size="small"
            startIcon={submitting ? <CircularProgress size={11} sx={{ color: 'inherit' }} /> : undefined}
            sx={{ ...MONO, fontSize: '0.78rem', textTransform: 'none', flexShrink: 0, mt: 0.25 }}
          >
            {t.add}
          </Button>
        </Box>
      </DialogContent>

      <DialogActions sx={{ px: 3, pb: 2 }}>
        <Button
          onClick={onClose}
          size="small"
          sx={{ ...MONO, fontSize: '0.78rem', textTransform: 'none', color: theme.palette.text.secondary }}
        >
          {t.close}
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export default ManageLanguagesModal;

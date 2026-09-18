import { useState } from 'react';
import Dialog from '@mui/material/Dialog';
import DialogTitle from '@mui/material/DialogTitle';
import DialogContent from '@mui/material/DialogContent';
import DialogActions from '@mui/material/DialogActions';
import Button from '@mui/material/Button';
import TextField from '@mui/material/TextField';
import { useTheme } from '@mui/material/styles';
import { useThemeMode } from '../../context/ThemeContext.tsx';
import { useI18n } from '../../i18n/I18nContext.tsx';
import { useNamespace, extractNamespace } from '../../context/NamespaceContext.tsx';

interface AddKeyModalProps {
  open: boolean;
  existingKeys: Set<string>;
  onAdd: (key: string) => void;
  onClose: () => void;
}

const MONO = { fontFamily: "'IBM Plex Mono', monospace" };

/** The part of `currentKey` after its namespace prefix — or after the last dot if it doesn't start with `currentNs`. */
function leafOf(currentKey: string, currentNs: string): string {
  if (currentNs && currentKey.startsWith(`${currentNs}.`)) return currentKey.slice(currentNs.length + 1);
  const i = currentKey.lastIndexOf('.');
  return i >= 0 ? currentKey.slice(i + 1) : currentKey;
}

const AddKeyModal = ({ open, existingKeys, onAdd, onClose }: AddKeyModalProps) => {
  const theme = useTheme();
  const { mode } = useThemeMode();
  const { t } = useI18n();
  const { namespacePaths, selectedNamespace } = useNamespace();
  const isDark = mode === 'dark';

  const [key, setKey] = useState('');
  const [prevOpen, setPrevOpen] = useState(open);

  if (open !== prevOpen) {
    setPrevOpen(open);
    if (open) setKey(selectedNamespace ? `${selectedNamespace}.` : '');
  }

  const ns = extractNamespace(key);
  const trimmedKey = key.trim();
  const isDuplicate = trimmedKey.length > 0 && existingKeys.has(trimmedKey);
  const isValid = trimmedKey.length > 0 && !isDuplicate;
  const isNewNamespace = ns !== '' && !namespacePaths.includes(ns);

  const handleNamespaceChange = (newNs: string) => {
    const leaf = leafOf(key, ns);
    setKey(newNs ? `${newNs}.${leaf}` : leaf);
  };

  const handleAdd = () => {
    if (!isValid) return;
    onAdd(trimmedKey);
    onClose();
  };

  const borderColor = isDark ? '#2e2e2e' : '#e0e0e0';

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
        {t.addKey}
      </DialogTitle>

      <DialogContent sx={{ pt: '8px !important', display: 'flex', flexDirection: 'column', gap: 2 }}>
        <TextField
          label={t.namespaceLabel}
          placeholder={t.namespacePlaceholder}
          value={ns}
          onChange={e => handleNamespaceChange(e.target.value)}
          size="small"
          helperText={isNewNamespace ? t.namespaceWillBeCreated : undefined}
          slotProps={{
            htmlInput: { list: 'namespace-suggestions' },
            inputLabel: { sx: { ...MONO, fontSize: '0.82rem' } },
            input: { sx: { ...MONO, fontSize: '0.82rem' } },
            formHelperText: isNewNamespace
              ? { sx: { color: theme.palette.warning.main, fontWeight: 600 } }
              : undefined,
          }}
        />
        <datalist id="namespace-suggestions">
          {namespacePaths.map(path => <option key={path} value={path} />)}
        </datalist>

        <TextField
          label={t.newKeyLabel}
          placeholder={t.newKeyPlaceholder}
          value={key}
          onChange={e => setKey(e.target.value)}
          onKeyDown={e => { if (e.key === 'Enter') handleAdd(); }}
          size="small"
          autoFocus
          error={isDuplicate}
          helperText={isDuplicate ? t.duplicateKey : undefined}
          slotProps={{
            inputLabel: { sx: { ...MONO, fontSize: '0.82rem' } },
            input: { sx: { ...MONO, fontSize: '0.82rem' } },
          }}
        />
      </DialogContent>

      <DialogActions sx={{ px: 3, pb: 2 }}>
        <Button
          onClick={onClose}
          size="small"
          sx={{ ...MONO, fontSize: '0.78rem', textTransform: 'none', color: theme.palette.text.secondary }}
        >
          {t.cancel}
        </Button>
        <Button
          onClick={handleAdd}
          disabled={!isValid}
          variant="contained"
          size="small"
          sx={{ ...MONO, fontSize: '0.78rem', textTransform: 'none' }}
        >
          {t.add}
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export default AddKeyModal;

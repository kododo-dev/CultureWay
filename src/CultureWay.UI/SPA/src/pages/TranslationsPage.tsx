import { useCallback, useEffect, useMemo, useState } from 'react';
import Box from '@mui/material/Box';
import TextField from '@mui/material/TextField';
import Button from '@mui/material/Button';
import IconButton from '@mui/material/IconButton';
import Tooltip from '@mui/material/Tooltip';
import CircularProgress from '@mui/material/CircularProgress';
import Snackbar from '@mui/material/Snackbar';
import Alert from '@mui/material/Alert';
import Typography from '@mui/material/Typography';
import Badge from '@mui/material/Badge';
import Chip from '@mui/material/Chip';
import AddIcon from '@mui/icons-material/Add';
import SaveIcon from '@mui/icons-material/Save';
import DeleteIcon from '@mui/icons-material/Delete';
import SearchOffIcon from '@mui/icons-material/SearchOff';
import { useTheme } from '@mui/material/styles';
import { useThemeMode } from '../context/ThemeContext.tsx';
import { useTranslations } from '../context/TranslationsContext.tsx';
import { useI18n } from '../i18n/I18nContext.tsx';
import { saveTranslations } from '../api/api.ts';
import type { TranslationDto, TranslationKeyDto } from '../api/api.model.ts';
import PageHeader from '../components/layout/PageHeader.tsx';

type LocalMap = Map<string, Map<string, string>>;

function buildLocalMap(translations: TranslationDto[]): LocalMap {
  const map: LocalMap = new Map();
  for (const t of translations) {
    if (!map.has(t.key)) map.set(t.key, new Map());
    map.get(t.key)!.set(t.culture, t.value);
  }
  return map;
}

type SnackState =
  | { open: false }
  | { open: true; severity: 'success' | 'error'; messages: string[] };

const TranslationsPage = () => {
  const { translations, cultures, loading, error, reload } = useTranslations();
  const { t } = useI18n();
  const theme = useTheme();
  const { mode } = useThemeMode();
  const isDark = mode === 'dark';

  const borderColor = isDark ? '#2e2e2e' : '#e0e0e0';
  const headerBg    = isDark ? '#1e1e1e' : '#f0f0f0';
  const rowHover    = isDark ? '#252525' : '#fafafa';
  const dirtyBg     = isDark ? '#2a2500' : '#fffbeb';
  const deletedBg   = isDark ? '#2a0a0a' : '#fff5f5';

  // ── local state ──────────────────────────────────────────────────
  const [localMap, setLocalMap]         = useState<LocalMap>(new Map());
  const [keysToDelete, setKeysToDelete] = useState<Set<string>>(new Set());
  const [originalMap, setOriginalMap]   = useState<LocalMap>(new Map());
  const [newKey, setNewKey]             = useState('');
  const [filter, setFilter]             = useState('');
  const [saving, setSaving]             = useState(false);
  const [snack, setSnack]               = useState<SnackState>({ open: false });

  // Sync from API into local state
  useEffect(() => {
    const m = buildLocalMap(translations);
    setLocalMap(new Map(m));
    setOriginalMap(new Map(m));
    setKeysToDelete(new Set());
  }, [translations]);

  // ── derived ──────────────────────────────────────────────────────
  const allKeys = useMemo(() => {
    const keys = new Set<string>();
    for (const k of localMap.keys()) keys.add(k);
    return [...keys].sort((a, b) => a.localeCompare(b));
  }, [localMap]);

  const filteredKeys = useMemo(() => {
    if (!filter) return allKeys;
    const lo = filter.toLowerCase();
    return allKeys.filter(k => k.toLowerCase().includes(lo));
  }, [allKeys, filter]);

  const isDirtyKey = useCallback((key: string): boolean => {
    if (keysToDelete.has(key)) return true;
    const orig = originalMap.get(key);
    const curr = localMap.get(key);
    if (!orig && curr) return true;
    if (orig && !curr) return true;
    for (const c of cultures) {
      if ((orig?.get(c.code) ?? '') !== (curr?.get(c.code) ?? '')) return true;
    }
    return false;
  }, [keysToDelete, originalMap, localMap, cultures]);

  const dirtyCount = useMemo(
    () => allKeys.filter(k => isDirtyKey(k)).length,
    [allKeys, isDirtyKey],
  );

  const isDirty = dirtyCount > 0;

  // ── handlers ─────────────────────────────────────────────────────
  const handleCellChange = (key: string, culture: string, value: string) => {
    setLocalMap(prev => {
      const next = new Map(prev);
      if (!next.has(key)) next.set(key, new Map());
      next.get(key)!.set(culture, value);
      return next;
    });
  };

  const handleAddKey = () => {
    const k = newKey.trim();
    if (!k) return;
    setLocalMap(prev => {
      if (prev.has(k)) return prev;
      const next = new Map(prev);
      next.set(k, new Map());
      return next;
    });
    setKeysToDelete(prev => { const n = new Set(prev); n.delete(k); return n; });
    setNewKey('');
  };

  const handleDeleteKey = (key: string) => {
    if (!window.confirm(t.confirmDelete)) return;
    setKeysToDelete(prev => new Set([...prev, key]));
  };

  const handleRestoreKey = (key: string) => {
    setKeysToDelete(prev => { const n = new Set(prev); n.delete(key); return n; });
  };

  const handleSave = async () => {
    setSaving(true);
    try {
      const toSave: TranslationDto[] = [];
      for (const [key, cultureMap] of localMap.entries()) {
        if (keysToDelete.has(key)) continue;
        for (const [culture, value] of cultureMap.entries()) {
          if (value.trim() !== '') toSave.push({ key, culture, value });
        }
      }

      const toDelete: TranslationKeyDto[] = [...keysToDelete].flatMap(key =>
        cultures.map(c => ({ key, culture: c.code })),
      );

      const errors = await saveTranslations(toSave, toDelete);

      if (errors.length === 0) {
        setSnack({ open: true, severity: 'success', messages: [t.saved] });
        await reload();
      } else {
        setSnack({ open: true, severity: 'error', messages: errors });
      }
    } catch (e) {
      setSnack({ open: true, severity: 'error', messages: [e instanceof Error ? e.message : 'Save failed'] });
    } finally {
      setSaving(false);
    }
  };

  const KEY_COL_WIDTH = 220;
  const VAL_COL_WIDTH = 220;

  // ── render ───────────────────────────────────────────────────────
  if (loading) {
    return (
      <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'center', flex: 1 }}>
        <CircularProgress size={32} />
        <Typography sx={{ ml: 2, color: theme.palette.text.secondary }}>{t.loading}</Typography>
      </Box>
    );
  }

  if (error) {
    return (
      <Box sx={{ p: 4 }}>
        <Alert severity="error">{error}</Alert>
      </Box>
    );
  }

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', height: '100%', overflow: 'hidden' }}>
      <PageHeader
        title={t.appTitle}
        subtitle={`${allKeys.length} keys · ${cultures.length} cultures`}
      />

      {/* Toolbar */}
      <Box sx={{
        px: 2, py: 1.5,
        borderBottom: `1px solid ${borderColor}`,
        display: 'flex', alignItems: 'center', gap: 1.5, flexWrap: 'wrap',
        flexShrink: 0,
        background: isDark ? '#1c1c1c' : '#fff',
      }}>
        <TextField
          size="small"
          placeholder={t.newKeyPlaceholder}
          value={newKey}
          onChange={e => setNewKey(e.target.value)}
          onKeyDown={e => { if (e.key === 'Enter') handleAddKey(); }}
          sx={{ width: 240, '& .MuiInputBase-input': { fontSize: '0.82rem' } }}
        />
        <Button
          variant="contained"
          size="small"
          startIcon={<AddIcon />}
          onClick={handleAddKey}
          disabled={!newKey.trim()}
          sx={{ textTransform: 'none', fontFamily: "'IBM Plex Mono', monospace" }}
        >
          {t.addKey}
        </Button>

        <Box sx={{ flex: 1 }} />

        <TextField
          size="small"
          placeholder={t.filterPlaceholder}
          value={filter}
          onChange={e => setFilter(e.target.value)}
          sx={{ width: 180, '& .MuiInputBase-input': { fontSize: '0.82rem' } }}
        />

        {isDirty && (
          <Chip
            label={`${dirtyCount} ${t.unsavedChanges}`}
            size="small"
            color="warning"
            variant="outlined"
          />
        )}

        <Badge badgeContent={isDirty ? dirtyCount : 0} color="warning" max={99}>
          <Button
            variant="contained"
            size="small"
            startIcon={saving ? <CircularProgress size={14} color="inherit" /> : <SaveIcon />}
            onClick={() => void handleSave()}
            disabled={!isDirty || saving}
            color="primary"
            sx={{ textTransform: 'none', fontFamily: "'IBM Plex Mono', monospace", minWidth: 90 }}
          >
            {saving ? t.saving : t.save}
          </Button>
        </Badge>
      </Box>

      {/* Table */}
      <Box sx={{ flex: 1, overflow: 'auto' }}>
        <table style={{
          borderCollapse: 'collapse',
          width: '100%',
          tableLayout: 'fixed',
          minWidth: KEY_COL_WIDTH + cultures.length * VAL_COL_WIDTH + 56,
        }}>
          {/* Header */}
          <thead>
            <tr>
              <th style={{
                width: KEY_COL_WIDTH,
                padding: '8px 12px',
                textAlign: 'left',
                fontSize: '0.72rem',
                fontWeight: 700,
                letterSpacing: '0.07em',
                textTransform: 'uppercase',
                color: theme.palette.text.secondary,
                background: headerBg,
                borderBottom: `1px solid ${borderColor}`,
                borderRight: `1px solid ${borderColor}`,
                position: 'sticky',
                top: 0,
                zIndex: 2,
              }}>
                {t.key}
              </th>
              {cultures.map(c => (
                <th key={c.code} style={{
                  width: VAL_COL_WIDTH,
                  padding: '8px 12px',
                  textAlign: 'left',
                  fontSize: '0.72rem',
                  fontWeight: 700,
                  letterSpacing: '0.07em',
                  textTransform: 'uppercase',
                  color: c.isDefault ? theme.palette.primary.main : theme.palette.text.secondary,
                  background: headerBg,
                  borderBottom: `1px solid ${borderColor}`,
                  borderRight: `1px solid ${borderColor}`,
                  position: 'sticky',
                  top: 0,
                  zIndex: 2,
                }}>
                  {c.code}{c.isDefault ? ' ★' : ''}
                </th>
              ))}
              <th style={{
                width: 56,
                background: headerBg,
                borderBottom: `1px solid ${borderColor}`,
                position: 'sticky',
                top: 0,
                zIndex: 2,
              }} />
            </tr>
          </thead>

          <tbody>
            {filteredKeys.length === 0 && (
              <tr>
                <td colSpan={cultures.length + 2} style={{ padding: '48px 24px', textAlign: 'center' }}>
                  {filter ? (
                    <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 1, color: theme.palette.text.secondary }}>
                      <SearchOffIcon sx={{ fontSize: 32, opacity: 0.4 }} />
                      <Typography sx={{ fontSize: '0.875rem' }}>No keys match &quot;{filter}&quot;</Typography>
                      <Button size="small" onClick={() => setFilter('')} sx={{ textTransform: 'none', fontSize: '0.8rem' }}>
                        {t.clearFilter}
                      </Button>
                    </Box>
                  ) : (
                    <Typography sx={{ fontSize: '0.875rem', color: theme.palette.text.secondary }}>
                      {t.noTranslations}
                    </Typography>
                  )}
                </td>
              </tr>
            )}
            {filteredKeys.map(key => {
              const deleted = keysToDelete.has(key);
              const dirty = isDirtyKey(key);
              const rowBg = deleted ? deletedBg : dirty ? dirtyBg : undefined;

              return (
                <tr
                  key={key}
                  style={{ background: rowBg }}
                  onMouseEnter={e => { if (!rowBg) (e.currentTarget as HTMLTableRowElement).style.background = rowHover; }}
                  onMouseLeave={e => { if (!rowBg) (e.currentTarget as HTMLTableRowElement).style.background = ''; }}
                >
                  {/* Key cell */}
                  <td style={{
                    padding: '6px 12px',
                    borderBottom: `1px solid ${borderColor}`,
                    borderRight: `1px solid ${borderColor}`,
                    verticalAlign: 'middle',
                    width: KEY_COL_WIDTH,
                  }}>
                    <Typography sx={{
                      fontSize: '0.78rem',
                      fontFamily: "'IBM Plex Mono', monospace",
                      color: deleted ? theme.palette.error.main : theme.palette.text.primary,
                      textDecoration: deleted ? 'line-through' : 'none',
                      wordBreak: 'break-all',
                    }}>
                      {key}
                    </Typography>
                  </td>

                  {/* Value cells */}
                  {cultures.map(c => {
                    const val = localMap.get(key)?.get(c.code) ?? '';
                    const origVal = originalMap.get(key)?.get(c.code) ?? '';
                    const cellDirty = val !== origVal;

                    return (
                      <td key={c.code} style={{
                        padding: '4px 8px',
                        borderBottom: `1px solid ${borderColor}`,
                        borderRight: `1px solid ${borderColor}`,
                        verticalAlign: 'top',
                        background: cellDirty && !deleted ? (isDark ? '#2a2500' : '#fffbeb') : undefined,
                        width: VAL_COL_WIDTH,
                      }}>
                        <TextField
                          multiline
                          maxRows={3}
                          size="small"
                          fullWidth
                          value={val}
                          disabled={deleted}
                          onChange={e => handleCellChange(key, c.code, e.target.value)}
                          variant="standard"
                          slotProps={{ input: { disableUnderline: !cellDirty } }}
                          sx={{
                            '& .MuiInputBase-input': {
                              fontSize: '0.8rem',
                              fontFamily: "'IBM Plex Mono', monospace",
                              resize: 'none',
                              color: deleted ? theme.palette.text.disabled : theme.palette.text.primary,
                            },
                            '& .MuiInput-underline:before': { borderBottomColor: borderColor },
                          }}
                        />
                      </td>
                    );
                  })}

                  {/* Actions */}
                  <td style={{
                    padding: '4px',
                    borderBottom: `1px solid ${borderColor}`,
                    textAlign: 'center',
                    verticalAlign: 'middle',
                    width: 56,
                  }}>
                    {deleted ? (
                      <Tooltip title="Restore">
                        <IconButton size="small" onClick={() => handleRestoreKey(key)} color="warning" sx={{ fontSize: 14 }}>
                          ↩
                        </IconButton>
                      </Tooltip>
                    ) : (
                      <Tooltip title={t.deleteKey}>
                        <IconButton size="small" onClick={() => handleDeleteKey(key)} color="error">
                          <DeleteIcon sx={{ fontSize: 16 }} />
                        </IconButton>
                      </Tooltip>
                    )}
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </Box>

      {/* Snackbar */}
      <Snackbar
        open={snack.open}
        autoHideDuration={snack.open && snack.severity === 'success' ? 3000 : 8000}
        onClose={() => setSnack({ open: false })}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
      >
        {snack.open ? (
          <Alert
            severity={snack.severity}
            onClose={() => setSnack({ open: false })}
            sx={{ width: '100%', maxWidth: 480 }}
          >
            {snack.severity === 'success'
              ? snack.messages[0]
              : (
                <Box>
                  <strong>{t.errors}:</strong>
                  <ul style={{ margin: '4px 0 0', paddingLeft: 16 }}>
                    {snack.messages.map((m, i) => <li key={i}>{m}</li>)}
                  </ul>
                </Box>
              )
            }
          </Alert>
        ) : undefined}
      </Snackbar>
    </Box>
  );
};

export default TranslationsPage;

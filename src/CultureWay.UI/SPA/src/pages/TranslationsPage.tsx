import { useCallback, useMemo, useState } from 'react';
import Box from '@mui/material/Box';
import Checkbox from '@mui/material/Checkbox';
import CircularProgress from '@mui/material/CircularProgress';
import Snackbar from '@mui/material/Snackbar';
import Alert from '@mui/material/Alert';
import Typography from '@mui/material/Typography';
import SearchOffIcon from '@mui/icons-material/SearchOff';
import { useTheme } from '@mui/material/styles';
import { useThemeMode } from '../context/ThemeContext.tsx';
import { useTranslations } from '../context/TranslationsContext.tsx';
import { useNamespace, extractNamespace } from '../context/NamespaceContext.tsx';
import { useI18n } from '../i18n/I18nContext.tsx';
import { saveTranslations } from '../api/api.ts';
import type { TranslationDto, TranslationKeyDto } from '../api/api.model.ts';
import PageHeader from '../components/layout/PageHeader.tsx';
import AddKeyModal from '../components/translations/AddKeyModal.tsx';
import ManageLanguagesModal from '../components/translations/ManageLanguagesModal.tsx';
import SelectionToolbar from '../components/translations/SelectionToolbar.tsx';
import CultureFlag from '../components/translations/CultureFlag.tsx';

type KeyClassification = 'pending' | 'resettable' | 'deletable' | 'locked';

type LocalMap = Map<string, Map<string, string>>;
type ExternalMap = Map<string, Map<string, string>>;

function buildLocalMap(translations: TranslationDto[]): LocalMap {
  const map: LocalMap = new Map();
  for (const t of translations) {
    if (!map.has(t.key)) map.set(t.key, new Map());
    map.get(t.key)!.set(t.culture, t.value);
  }
  return map;
}

function buildExternalMap(translations: TranslationDto[]): ExternalMap {
  const map: ExternalMap = new Map();
  for (const t of translations) {
    if (t.hasExternalDefault && t.externalDefaultValue !== null) {
      if (!map.has(t.key)) map.set(t.key, new Map());
      map.get(t.key)!.set(t.culture, t.externalDefaultValue);
    }
  }
  return map;
}

type SnackState =
  | { open: false }
  | { open: true; severity: 'success' | 'error'; messages: string[] };

const TranslationsPage = () => {
  const { translations, cultures, loading, error, reload, hiddenCultures } = useTranslations();
  const visibleCultures = useMemo(
    () => cultures.filter(c => !hiddenCultures.has(c.code)),
    [cultures, hiddenCultures],
  );
  const { selectedNamespace } = useNamespace();
  const { t } = useI18n();
  const theme = useTheme();
  const { mode } = useThemeMode();
  const isDark = mode === 'dark';

  const borderColor = isDark ? '#2e2e2e' : '#e0e0e0';
  const headerBg    = isDark ? '#1e1e1e' : '#f0f0f0';
  const rowHover    = isDark ? '#252525' : '#fafafa';
  const dirtyBg     = isDark ? '#2a2500' : '#fffbeb';
  const deletedBg   = isDark ? '#2a0a0a' : '#fff5f5';
  const resetBg     = isDark ? '#0a1a2a' : '#f0f7ff';

  const [localMap, setLocalMap]         = useState<LocalMap>(new Map());
  const [keysToDelete, setKeysToDelete] = useState<Set<string>>(new Set());
  const [keysToReset, setKeysToReset]   = useState<Set<string>>(new Set());
  const [originalMap, setOriginalMap]   = useState<LocalMap>(new Map());
  const [externalMap, setExternalMap]   = useState<ExternalMap>(new Map());
  const [filter, setFilter]             = useState('');
  const [saving, setSaving]             = useState(false);
  const [snack, setSnack]               = useState<SnackState>({ open: false });
  const [addModalOpen, setAddModalOpen] = useState(false);
  const [addLangModalOpen, setAddLangModalOpen] = useState(false);
  const [selectedKeys, setSelectedKeys] = useState<Set<string>>(new Set());
  const [prevTranslations, setPrevTranslations] = useState(translations);

  if (translations !== prevTranslations) {
    setPrevTranslations(translations);
    const m = buildLocalMap(translations);
    setLocalMap(new Map(m));
    setOriginalMap(new Map(m));
    setExternalMap(buildExternalMap(translations));
    setKeysToDelete(new Set());
    setKeysToReset(new Set());
    setSelectedKeys(new Set());
  }

  const allKeys = useMemo(() => {
    const keys = new Set<string>();
    for (const k of localMap.keys()) keys.add(k);
    return [...keys].sort((a, b) => a.localeCompare(b));
  }, [localMap]);

  const namespaceLabel = selectedNamespace === null
    ? t.allNamespaces
    : selectedNamespace === ''
      ? t.rootNamespace
      : selectedNamespace;

  const filteredKeys = useMemo(() => {
    let keys = allKeys;

    if (selectedNamespace !== null) {
      keys = keys.filter(k => {
        const ns = extractNamespace(k);
        return ns === selectedNamespace || ns.startsWith(`${selectedNamespace}.`);
      });
    }

    if (filter) {
      const lo = filter.toLowerCase();
      keys = keys.filter(k => {
        if (k.toLowerCase().includes(lo)) return true;
        const cultureMap = localMap.get(k);
        if (cultureMap) {
          for (const val of cultureMap.values()) {
            if (val.toLowerCase().includes(lo)) return true;
          }
        }
        return false;
      });
    }

    return keys;
  }, [allKeys, filter, selectedNamespace, localMap]);

  const isKeyFromExternal = useCallback((key: string): boolean => {
    return externalMap.has(key);
  }, [externalMap]);

  const hasStoreOverride = useCallback((key: string): boolean => {
    const extCultures = externalMap.get(key);
    if (!extCultures) return false;
    const origCultures = originalMap.get(key);
    if (!origCultures) return false;
    for (const [culture, extVal] of extCultures.entries()) {
      const storeVal = origCultures.get(culture);
      if (storeVal !== undefined && storeVal !== extVal) return true;
    }
    return false;
  }, [externalMap, originalMap]);

  const isDirtyKey = useCallback((key: string): boolean => {
    if (keysToDelete.has(key) || keysToReset.has(key)) return true;
    const orig = originalMap.get(key);
    const curr = localMap.get(key);
    if (!orig && curr) return true;
    if (orig && !curr) return true;
    for (const c of cultures) {
      if ((orig?.get(c.code) ?? '') !== (curr?.get(c.code) ?? '')) return true;
    }
    return false;
  }, [keysToDelete, keysToReset, originalMap, localMap, cultures]);

  const dirtyCount = useMemo(
    () => allKeys.filter(k => isDirtyKey(k)).length,
    [allKeys, isDirtyKey],
  );

  const classifyKey = useCallback((key: string): KeyClassification => {
    if (keysToDelete.has(key) || keysToReset.has(key)) return 'pending';
    if (isKeyFromExternal(key)) return hasStoreOverride(key) ? 'resettable' : 'locked';
    return 'deletable';
  }, [keysToDelete, keysToReset, isKeyFromExternal, hasStoreOverride]);

  const selectedArray = useMemo(() => [...selectedKeys], [selectedKeys]);
  const deletableSelected  = useMemo(() => selectedArray.filter(k => classifyKey(k) === 'deletable'),  [selectedArray, classifyKey]);
  const resettableSelected = useMemo(() => selectedArray.filter(k => classifyKey(k) === 'resettable'), [selectedArray, classifyKey]);
  const pendingSelected    = useMemo(() => selectedArray.filter(k => classifyKey(k) === 'pending'),    [selectedArray, classifyKey]);

  const allVisibleSelected = filteredKeys.length > 0 && filteredKeys.every(k => selectedKeys.has(k));
  const someVisibleSelected = filteredKeys.some(k => selectedKeys.has(k));

  const toggleSelectKey = (key: string) => {
    setSelectedKeys(prev => {
      const next = new Set(prev);
      if (next.has(key)) next.delete(key); else next.add(key);
      return next;
    });
  };

  const toggleSelectAll = () => {
    setSelectedKeys(prev => {
      const next = new Set(prev);
      if (allVisibleSelected) {
        for (const k of filteredKeys) next.delete(k);
      } else {
        for (const k of filteredKeys) next.add(k);
      }
      return next;
    });
  };

  const handleCellChange = (key: string, culture: string, value: string) => {
    setLocalMap(prev => {
      const next = new Map(prev);
      const cultureMap = new Map(next.get(key));
      cultureMap.set(culture, value);
      next.set(key, cultureMap);
      return next;
    });
    setKeysToReset(prev => { const n = new Set(prev); n.delete(key); return n; });
  };

  const handleAddKey = (key: string) => {
    setLocalMap(prev => {
      if (prev.has(key)) return prev;
      const next = new Map(prev);
      next.set(key, new Map());
      return next;
    });
    setKeysToDelete(prev => { const n = new Set(prev); n.delete(key); return n; });
    setKeysToReset(prev => { const n = new Set(prev); n.delete(key); return n; });
  };

  const handleBulkDelete = () => {
    if (deletableSelected.length === 0) return;
    if (!window.confirm(t.confirmDeleteSelected)) return;
    setKeysToDelete(prev => new Set([...prev, ...deletableSelected]));
    setKeysToReset(prev => { const n = new Set(prev); for (const k of deletableSelected) n.delete(k); return n; });
    setSelectedKeys(new Set());
  };

  const handleBulkRestore = () => {
    if (pendingSelected.length === 0) return;
    setKeysToDelete(prev => { const n = new Set(prev); for (const k of pendingSelected) n.delete(k); return n; });
    setKeysToReset(prev => { const n = new Set(prev); for (const k of pendingSelected) n.delete(k); return n; });
    setSelectedKeys(new Set());
  };

  const handleBulkReset = () => {
    if (resettableSelected.length === 0) return;
    if (!window.confirm(t.confirmResetSelected)) return;
    setKeysToReset(prev => new Set([...prev, ...resettableSelected]));
    setKeysToDelete(prev => { const n = new Set(prev); for (const k of resettableSelected) n.delete(k); return n; });
    // Restore cells to external default for visual preview
    setLocalMap(prev => {
      const next = new Map(prev);
      for (const key of resettableSelected) {
        const extCultures = externalMap.get(key);
        if (extCultures) {
          const cultureMap = new Map<string, string>();
          for (const [culture, extVal] of extCultures.entries()) {
            cultureMap.set(culture, extVal);
          }
          next.set(key, cultureMap);
        }
      }
      return next;
    });
    setSelectedKeys(new Set());
  };

  const handleDiscard = () => {
    setLocalMap(new Map(originalMap));
    setKeysToDelete(new Set());
    setKeysToReset(new Set());
    setSelectedKeys(new Set());
  };

  const handleSave = async () => {
    setSaving(true);
    try {
      const toSave: TranslationDto[] = [];
      for (const [key, cultureMap] of localMap.entries()) {
        if (keysToDelete.has(key) || keysToReset.has(key)) continue;
        for (const [culture, value] of cultureMap.entries()) {
          if (value.trim() !== '') toSave.push({ key, culture, value, hasExternalDefault: false, externalDefaultValue: null });
        }
      }

      const toDelete: TranslationKeyDto[] = [
        ...[...keysToDelete].flatMap(key => cultures.map(c => ({ key, culture: c.code }))),
        ...[...keysToReset].flatMap(key => cultures.map(c => ({ key, culture: c.code }))),
      ];

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
  const CHECKBOX_COL_WIDTH = 40;

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

  const existingKeysSet = new Set(allKeys);

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', height: '100%', overflow: 'hidden' }}>
      <PageHeader
        namespaceLabel={namespaceLabel}
        searchQuery={filter}
        onSearchChange={setFilter}
        dirtyCount={dirtyCount}
        saving={saving}
        onSave={() => void handleSave()}
        onDiscard={handleDiscard}
        onAddKey={() => setAddModalOpen(true)}
        onManageLanguages={() => setAddLangModalOpen(true)}
      />

      {selectedKeys.size > 0 && (
        <SelectionToolbar
          selectedCount={selectedKeys.size}
          deletableCount={deletableSelected.length}
          resettableCount={resettableSelected.length}
          restorableCount={pendingSelected.length}
          onDelete={handleBulkDelete}
          onReset={handleBulkReset}
          onRestore={handleBulkRestore}
          onClear={() => setSelectedKeys(new Set())}
        />
      )}

      <Box sx={{ flex: 1, overflow: 'auto' }}>
        <table style={{
          borderCollapse: 'collapse',
          width: '100%',
          tableLayout: 'fixed',
          minWidth: CHECKBOX_COL_WIDTH + KEY_COL_WIDTH + visibleCultures.length * VAL_COL_WIDTH,
        }}>
          <thead>
            <tr>
              <th style={{
                width: CHECKBOX_COL_WIDTH,
                padding: '4px 8px',
                textAlign: 'center',
                background: headerBg,
                borderBottom: `1px solid ${borderColor}`,
                borderRight: `1px solid ${borderColor}`,
                position: 'sticky',
                top: 0,
                zIndex: 2,
              }}>
                <Checkbox
                  size="small"
                  checked={allVisibleSelected}
                  indeterminate={someVisibleSelected && !allVisibleSelected}
                  onChange={toggleSelectAll}
                  disabled={filteredKeys.length === 0}
                  sx={{ p: 0.25 }}
                />
              </th>
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
              {visibleCultures.map(c => (
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
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                    <CultureFlag code={c.code} />
                    <span>{c.code}{c.isDefault ? ' ★' : ''}</span>
                  </Box>
                </th>
              ))}
            </tr>
          </thead>

          <tbody>
            {filteredKeys.length === 0 && (
              <tr>
                <td colSpan={visibleCultures.length + 2} style={{ padding: '48px 24px', textAlign: 'center' }}>
                  {filter ? (
                    <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 1, color: theme.palette.text.secondary }}>
                      <SearchOffIcon sx={{ fontSize: 32, opacity: 0.4 }} />
                      <Typography sx={{ fontSize: '0.875rem' }}>No keys match &quot;{filter}&quot;</Typography>
                      <Typography
                        component="span"
                        onClick={() => setFilter('')}
                        sx={{ fontSize: '0.8rem', color: theme.palette.primary.main, cursor: 'pointer', '&:hover': { textDecoration: 'underline' } }}
                      >
                        {t.clearFilter}
                      </Typography>
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
              const deleted    = keysToDelete.has(key);
              const resetting  = keysToReset.has(key);
              const dirty      = isDirtyKey(key);
              const rowBg = deleted ? deletedBg : resetting ? resetBg : dirty ? dirtyBg : undefined;

              return (
                <tr
                  key={key}
                  style={{ background: rowBg }}
                  onMouseEnter={e => { if (!rowBg) (e.currentTarget as HTMLTableRowElement).style.background = rowHover; }}
                  onMouseLeave={e => { if (!rowBg) (e.currentTarget as HTMLTableRowElement).style.background = ''; }}
                >
                  <td style={{
                    padding: '4px 8px',
                    borderBottom: `1px solid ${borderColor}`,
                    borderRight: `1px solid ${borderColor}`,
                    verticalAlign: 'middle',
                    textAlign: 'center',
                    width: CHECKBOX_COL_WIDTH,
                  }}>
                    <Checkbox
                      size="small"
                      checked={selectedKeys.has(key)}
                      onChange={() => toggleSelectKey(key)}
                      sx={{ p: 0.25 }}
                    />
                  </td>
                  <td style={{
                    padding: '6px 12px',
                    borderBottom: `1px solid ${borderColor}`,
                    borderRight: `1px solid ${borderColor}`,
                    verticalAlign: 'middle',
                    width: KEY_COL_WIDTH,
                  }}>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                      <Typography sx={{
                        fontSize: '0.78rem',
                        fontFamily: "'IBM Plex Mono', monospace",
                        color: deleted ? theme.palette.error.main : resetting ? theme.palette.info.main : theme.palette.text.primary,
                        textDecoration: deleted ? 'line-through' : 'none',
                        wordBreak: 'break-all',
                      }}>
                        {key}
                      </Typography>
                    </Box>
                  </td>

                  {visibleCultures.map(c => {
                    const val = localMap.get(key)?.get(c.code) ?? '';
                    const origVal = originalMap.get(key)?.get(c.code) ?? '';
                    const cellDirty = val !== origVal;

                    return (
                      <td key={c.code} style={{
                        padding: '4px 8px',
                        borderBottom: `1px solid ${borderColor}`,
                        borderRight: `1px solid ${borderColor}`,
                        verticalAlign: 'top',
                        background: cellDirty && !deleted && !resetting ? (isDark ? '#2a2500' : '#fffbeb') : undefined,
                        width: VAL_COL_WIDTH,
                      }}>
                        <Box
                          component="textarea"
                          value={val}
                          disabled={deleted || resetting}
                          onChange={(e: React.ChangeEvent<HTMLTextAreaElement>) => handleCellChange(key, c.code, e.target.value)}
                          rows={1}
                          sx={{
                            width: '100%',
                            border: 'none',
                            outline: 'none',
                            background: 'transparent',
                            resize: 'none',
                            fontFamily: "'IBM Plex Mono', monospace",
                            fontSize: '0.8rem',
                            color: deleted || resetting ? theme.palette.text.disabled : theme.palette.text.primary,
                            p: 0.5,
                            lineHeight: 1.5,
                            overflow: 'hidden',
                            cursor: deleted || resetting ? 'default' : 'text',
                            '&:focus': {
                              outline: `1px solid ${theme.palette.primary.main}`,
                              borderRadius: '2px',
                            },
                          }}
                          onInput={(e: React.FormEvent<HTMLTextAreaElement>) => {
                            const el = e.currentTarget;
                            el.style.height = 'auto';
                            el.style.height = `${el.scrollHeight}px`;
                          }}
                        />
                      </td>
                    );
                  })}
                </tr>
              );
            })}
          </tbody>
        </table>
      </Box>

      <AddKeyModal
        open={addModalOpen}
        existingKeys={existingKeysSet}
        onAdd={handleAddKey}
        onClose={() => setAddModalOpen(false)}
      />

      <ManageLanguagesModal
        open={addLangModalOpen}
        cultures={cultures}
        onChanged={() => void reload()}
        onClose={() => setAddLangModalOpen(false)}
      />

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

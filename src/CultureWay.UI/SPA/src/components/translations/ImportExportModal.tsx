import { useRef, useState } from 'react';
import Dialog from '@mui/material/Dialog';
import DialogTitle from '@mui/material/DialogTitle';
import DialogContent from '@mui/material/DialogContent';
import DialogActions from '@mui/material/DialogActions';
import Button from '@mui/material/Button';
import CircularProgress from '@mui/material/CircularProgress';
import Typography from '@mui/material/Typography';
import Box from '@mui/material/Box';
import Tabs from '@mui/material/Tabs';
import Tab from '@mui/material/Tab';
import TextField from '@mui/material/TextField';
import MenuItem from '@mui/material/MenuItem';
import Radio from '@mui/material/Radio';
import RadioGroup from '@mui/material/RadioGroup';
import FormControlLabel from '@mui/material/FormControlLabel';
import Chip from '@mui/material/Chip';
import Alert from '@mui/material/Alert';
import FileDownloadOutlinedIcon from '@mui/icons-material/FileDownloadOutlined';
import UploadFileOutlinedIcon from '@mui/icons-material/UploadFileOutlined';
import { useTheme } from '@mui/material/styles';
import { useThemeMode } from '../../context/ThemeContext.tsx';
import { useI18n } from '../../i18n/I18nContext.tsx';
import { useEditorSettings } from '../../context/EditorSettingsContext.tsx';
import { addCulture, applyImport, exportTranslations, previewImport } from '../../api/api.ts';
import type { CultureDto, ImportInput, ImportPreviewDto } from '../../api/api.model.ts';
import { MONO_FONT } from '../../utils/fonts.ts';

interface ImportExportModalProps {
  open: boolean;
  cultures: CultureDto[];
  /** The namespace selected in the side menu: null for all keys. */
  selectedNamespace: string | null;
  namespaceLabel: string;
  hasUnsavedChanges: boolean;
  onImported: (added: number, updated: number) => void;
  onCulturesChanged: () => void;
  onClose: () => void;
}

const MONO = { fontFamily: MONO_FONT };
const SHOWN_CHANGES = 100;
const SHOWN_PROBLEMS = 50;

const formatSize = (bytes: number) =>
  bytes >= 1024 * 1024 ? `${(bytes / (1024 * 1024)).toFixed(1)} MB` : `${Math.round(bytes / 1024)} KB`;

function readAsBase64(file: File): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    // A data URL is "data:<type>;base64,<content>"; the API wants only the content.
    reader.onload = () => resolve(String(reader.result).split(',', 2)[1] ?? '');
    reader.onerror = () => reject(reader.error ?? new Error('Failed to read the file'));
    reader.readAsDataURL(file);
  });
}

function download(fileName: string, contentType: string, base64: string) {
  const bytes = Uint8Array.from(atob(base64), c => c.charCodeAt(0));
  const url = URL.createObjectURL(new Blob([bytes], { type: contentType }));
  const link = document.createElement('a');
  link.href = url;
  link.download = fileName;
  link.click();
  URL.revokeObjectURL(url);
}

const ImportExportModal = ({
  open,
  cultures,
  selectedNamespace,
  namespaceLabel,
  hasUnsavedChanges,
  onImported,
  onCulturesChanged,
  onClose,
}: ImportExportModalProps) => {
  const theme = useTheme();
  const { mode } = useThemeMode();
  const { t } = useI18n();
  const { canImport, canManageCultures, maxImportBytes } = useEditorSettings();
  const isDark = mode === 'dark';
  const borderColor = isDark ? '#2e2e2e' : '#e0e0e0';
  const defaultCulture = cultures.find(c => c.isDefault)?.code ?? cultures[0]?.code ?? '';

  const [tab, setTab] = useState<'export' | 'import'>('export');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Export
  const [format, setFormat] = useState<'json' | 'xlsx'>('xlsx');
  const [exportCulture, setExportCulture] = useState('');
  const [onlySelected, setOnlySelected] = useState(false);
  const [missingIn, setMissingIn] = useState('');

  // Import
  const fileInput = useRef<HTMLInputElement>(null);
  const [file, setFile] = useState<{ name: string; base64: string } | null>(null);
  const [fileCulture, setFileCulture] = useState('');
  const [onlyMissing, setOnlyMissing] = useState(false);
  const [preview, setPreview] = useState<ImportPreviewDto | null>(null);

  const [prevOpen, setPrevOpen] = useState(open);
  if (open !== prevOpen) {
    setPrevOpen(open);
    if (open) {
      setTab('export');
      setError(null);
      setExportCulture(defaultCulture);
      setOnlySelected(selectedNamespace !== null);
      setMissingIn('');
      setFile(null);
      setFileCulture(defaultCulture);
      setOnlyMissing(false);
      setPreview(null);
    }
  }

  const isJsonFile = file?.name.toLowerCase().endsWith('.json') ?? false;

  const run = async (action: () => Promise<void>) => {
    setBusy(true);
    setError(null);
    try {
      await action();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Request failed');
    } finally {
      setBusy(false);
    }
  };

  const handleExport = () => run(async () => {
    const result = await exportTranslations({
      format,
      cultures: format === 'json' ? [exportCulture] : [],
      namespace: onlySelected ? selectedNamespace : null,
      missingIn: missingIn || null,
    });
    if (result.error) {
      setError(result.error);
      return;
    }
    download(result.fileName, result.contentType, result.contentBase64);
  });

  const importInput = (): ImportInput | null => file && {
    fileName: file.name,
    contentBase64: file.base64,
    culture: isJsonFile ? fileCulture : null,
    onlyMissing,
  };

  const handleFile = (chosen: File | undefined) => {
    setPreview(null);
    setError(null);
    if (!chosen) return;
    if (chosen.size > maxImportBytes) {
      setFile(null);
      setError(`${t.fileTooLarge} ${formatSize(maxImportBytes)}.`);
      return;
    }
    void run(async () => setFile({ name: chosen.name, base64: await readAsBase64(chosen) }));
  };

  const handlePreview = () => run(async () => {
    const input = importInput();
    if (!input) return;
    const result = await previewImport(input);
    if (result.error) {
      setError(result.error);
      setPreview(null);
      return;
    }
    setPreview(result);
  });

  const handleAddLanguages = () => run(async () => {
    for (const code of preview?.unknownCultures ?? []) await addCulture(code);
    onCulturesChanged();
    const input = importInput();
    if (input) setPreview(await previewImport(input));
  });

  const handleApply = () => run(async () => {
    const input = importInput();
    if (!input) return;
    const result = await applyImport(input);
    if (result.error) {
      setError(result.error);
      return;
    }
    onImported(result.added, result.updated);
    onClose();
  });

  const sectionLabel = (text: string) => (
    <Typography sx={{
      fontSize: '0.68rem',
      color: theme.palette.text.secondary,
      textTransform: 'uppercase',
      letterSpacing: '0.08em',
      ...MONO,
    }}>
      {text}
    </Typography>
  );

  const fieldProps = {
    size: 'small' as const,
    slotProps: {
      inputLabel: { sx: { ...MONO, fontSize: '0.82rem' } },
      input: { sx: { ...MONO, fontSize: '0.82rem' } },
    },
  };
  const optionLabel = { '& .MuiFormControlLabel-label': { fontSize: '0.82rem' } };
  const buttonSx = { ...MONO, fontSize: '0.78rem', textTransform: 'none' };

  const added = preview?.changes.filter(c => c.oldValue === null).length ?? 0;
  const changed = (preview?.changes.length ?? 0) - added;

  return (
    <Dialog
      open={open}
      onClose={onClose}
      maxWidth="md"
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
      <DialogTitle sx={{ ...MONO, fontSize: '0.95rem', fontWeight: 700, pb: 0 }}>
        {t.importExport}
      </DialogTitle>

      <Tabs
        value={tab}
        onChange={(_, value: 'export' | 'import') => { setTab(value); setError(null); }}
        sx={{ px: 3, minHeight: 40, borderBottom: `1px solid ${borderColor}`, '& .MuiTab-root': { ...MONO, fontSize: '0.78rem', textTransform: 'none', minHeight: 40 } }}
      >
        <Tab value="export" label={t.exportTab} />
        {canImport && <Tab value="import" label={t.importTab} />}
      </Tabs>

      <DialogContent sx={{ pt: '16px !important', display: 'flex', flexDirection: 'column', gap: 1.5 }}>
        {tab === 'export' && (
          <>
            {sectionLabel(t.formatLabel)}
            <RadioGroup value={format} onChange={e => setFormat(e.target.value as 'json' | 'xlsx')}>
              <FormControlLabel value="xlsx" control={<Radio size="small" />} label={t.formatXlsx} sx={optionLabel} />
              <FormControlLabel value="json" control={<Radio size="small" />} label={t.formatJson} sx={optionLabel} />
            </RadioGroup>

            {format === 'json' && (
              <TextField select label={t.languageLabel} value={exportCulture} onChange={e => setExportCulture(e.target.value)} {...fieldProps} sx={{ maxWidth: 240 }}>
                {cultures.map(c => <MenuItem key={c.code} value={c.code} sx={MONO}>{c.code}</MenuItem>)}
              </TextField>
            )}

            {sectionLabel(t.keysLabel)}
            <RadioGroup value={onlySelected ? 'selected' : 'all'} onChange={e => setOnlySelected(e.target.value === 'selected')}>
              <FormControlLabel value="all" control={<Radio size="small" />} label={t.everyNamespace} sx={optionLabel} />
              <FormControlLabel
                value="selected"
                disabled={selectedNamespace === null}
                control={<Radio size="small" />}
                label={selectedNamespace === null ? t.selectedNamespaceOnly : `${t.selectedNamespaceOnly}: ${namespaceLabel}`}
                sx={optionLabel}
              />
            </RadioGroup>

            <TextField select label={t.missingInLabel} value={missingIn} onChange={e => setMissingIn(e.target.value)} {...fieldProps} sx={{ maxWidth: 360 }}>
              <MenuItem value="" sx={MONO}>{t.missingInAny}</MenuItem>
              {cultures.map(c => <MenuItem key={c.code} value={c.code} sx={MONO}>{c.code}</MenuItem>)}
            </TextField>
          </>
        )}

        {tab === 'import' && (
          <>
            {hasUnsavedChanges && <Alert severity="warning" sx={{ fontSize: '0.8rem' }}>{t.importSaveFirst}</Alert>}

            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, flexWrap: 'wrap' }}>
              <input
                ref={fileInput}
                type="file"
                accept=".json,.xlsx,application/json,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                hidden
                onChange={e => { handleFile(e.target.files?.[0]); e.target.value = ''; }}
              />
              <Button
                variant="outlined"
                size="small"
                startIcon={<UploadFileOutlinedIcon sx={{ fontSize: '15px !important' }} />}
                onClick={() => fileInput.current?.click()}
                sx={buttonSx}
              >
                {t.chooseFile}
              </Button>
              {file && <Typography sx={{ ...MONO, fontSize: '0.8rem' }}>{file.name}</Typography>}
              <Typography sx={{ fontSize: '0.72rem', color: theme.palette.text.secondary }}>
                ≤ {formatSize(maxImportBytes)}
              </Typography>
            </Box>

            {isJsonFile && (
              <TextField
                select
                label={t.fileLanguage}
                value={fileCulture}
                onChange={e => { setFileCulture(e.target.value); setPreview(null); }}
                {...fieldProps}
                sx={{ maxWidth: 240 }}
              >
                {cultures.map(c => <MenuItem key={c.code} value={c.code} sx={MONO}>{c.code}</MenuItem>)}
              </TextField>
            )}

            <RadioGroup
              value={onlyMissing ? 'missing' : 'overwrite'}
              onChange={e => { setOnlyMissing(e.target.value === 'missing'); setPreview(null); }}
            >
              <FormControlLabel value="overwrite" control={<Radio size="small" />} label={t.modeOverwrite} sx={optionLabel} />
              <FormControlLabel value="missing" control={<Radio size="small" />} label={t.modeOnlyMissing} sx={optionLabel} />
            </RadioGroup>

            {preview && (
              <>
                <Box sx={{ display: 'flex', gap: 0.75, flexWrap: 'wrap' }}>
                  <Chip size="small" color="success" variant="outlined" label={`${added} ${t.summaryNew}`} sx={MONO} />
                  <Chip size="small" color="primary" variant="outlined" label={`${changed} ${t.summaryChanged}`} sx={MONO} />
                  <Chip size="small" variant="outlined" label={`${preview.unchanged} ${t.summaryUnchanged}`} sx={MONO} />
                  {preview.kept > 0 && <Chip size="small" variant="outlined" label={`${preview.kept} ${t.summaryKept}`} sx={MONO} />}
                  {preview.problems.length > 0 && (
                    <Chip size="small" color="warning" variant="outlined" label={`${preview.problems.length} ${t.summarySkipped}`} sx={MONO} />
                  )}
                </Box>

                {preview.unknownCultures.length > 0 && (
                  <Alert
                    severity="info"
                    sx={{ fontSize: '0.8rem' }}
                    action={canManageCultures && (
                      <Button size="small" onClick={() => void handleAddLanguages()} disabled={busy} sx={buttonSx}>
                        {t.addTheseLanguages}
                      </Button>
                    )}
                  >
                    {t.unknownLanguages} <span style={{ fontFamily: MONO_FONT }}>{preview.unknownCultures.join(', ')}</span>
                  </Alert>
                )}

                {preview.changes.length === 0 && preview.problems.length === 0 && (
                  <Typography sx={{ fontSize: '0.82rem', color: theme.palette.text.secondary }}>{t.importNothing}</Typography>
                )}

                {preview.changes.length > 0 && (
                  <>
                    {sectionLabel(t.changesTitle)}
                    <Box sx={{ border: `1px solid ${borderColor}`, borderRadius: 1, maxHeight: 280, overflow: 'auto' }}>
                      <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '0.78rem', fontFamily: MONO_FONT }}>
                        <tbody>
                          {preview.changes.slice(0, SHOWN_CHANGES).map(c => (
                            <tr key={`${c.key}|${c.culture}`} style={{ borderTop: `1px solid ${borderColor}` }}>
                              <td style={{ padding: '6px 10px', verticalAlign: 'top', whiteSpace: 'nowrap' }}>{c.key}</td>
                              <td style={{ padding: '6px 10px', verticalAlign: 'top', color: theme.palette.text.secondary }}>{c.culture}</td>
                              <td style={{ padding: '6px 10px', verticalAlign: 'top', wordBreak: 'break-word' }}>
                                <div style={{ color: theme.palette.error.main, textDecoration: c.oldValue === null ? 'none' : 'line-through' }}>
                                  {c.oldValue ?? t.emptyValue}
                                </div>
                                <div style={{ color: theme.palette.success.main }}>{c.newValue}</div>
                              </td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </Box>
                    {preview.changes.length > SHOWN_CHANGES && (
                      <Typography sx={{ fontSize: '0.75rem', color: theme.palette.text.secondary }}>
                        +{preview.changes.length - SHOWN_CHANGES} {t.andMore}
                      </Typography>
                    )}
                  </>
                )}

                {preview.problems.length > 0 && (
                  <>
                    {sectionLabel(t.skippedTitle)}
                    <Box component="ul" sx={{ m: 0, pl: 2.5, fontSize: '0.78rem', color: theme.palette.text.secondary }}>
                      {preview.problems.slice(0, SHOWN_PROBLEMS).map((p, i) => (
                        <li key={i}><span style={{ fontFamily: MONO_FONT }}>{p.location}</span>: {p.reason}</li>
                      ))}
                      {preview.problems.length > SHOWN_PROBLEMS && <li>+{preview.problems.length - SHOWN_PROBLEMS} {t.andMore}</li>}
                    </Box>
                  </>
                )}
              </>
            )}
          </>
        )}

        {error && <Alert severity="error" sx={{ fontSize: '0.8rem' }}>{error}</Alert>}
      </DialogContent>

      <DialogActions sx={{ px: 3, pb: 2 }}>
        <Button onClick={onClose} size="small" sx={{ ...buttonSx, color: theme.palette.text.secondary }}>
          {t.close}
        </Button>
        {tab === 'export' && (
          <Button
            variant="contained"
            size="small"
            onClick={() => void handleExport()}
            disabled={busy || (format === 'json' && !exportCulture)}
            startIcon={busy ? <CircularProgress size={11} sx={{ color: 'inherit' }} /> : <FileDownloadOutlinedIcon sx={{ fontSize: '15px !important' }} />}
            sx={buttonSx}
          >
            {t.download}
          </Button>
        )}
        {tab === 'import' && !preview && (
          <Button
            variant="contained"
            size="small"
            onClick={() => void handlePreview()}
            disabled={busy || !file || (isJsonFile && !fileCulture)}
            startIcon={busy ? <CircularProgress size={11} sx={{ color: 'inherit' }} /> : undefined}
            sx={buttonSx}
          >
            {t.preview}
          </Button>
        )}
        {tab === 'import' && preview && (
          <Button
            variant="contained"
            size="small"
            onClick={() => void handleApply()}
            disabled={busy || hasUnsavedChanges || preview.changes.length === 0}
            startIcon={busy ? <CircularProgress size={11} sx={{ color: 'inherit' }} /> : undefined}
            sx={buttonSx}
          >
            {t.importApply} ({preview.changes.length})
          </Button>
        )}
      </DialogActions>
    </Dialog>
  );
};

export default ImportExportModal;

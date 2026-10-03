import { useEffect, useState } from 'react';
import Dialog from '@mui/material/Dialog';
import DialogTitle from '@mui/material/DialogTitle';
import DialogContent from '@mui/material/DialogContent';
import DialogContentText from '@mui/material/DialogContentText';
import DialogActions from '@mui/material/DialogActions';
import Button from '@mui/material/Button';
import { onSessionExpired } from '../../api/api.ts';
import { useI18n } from '../../i18n/I18nContext.tsx';
import { MONO_FONT } from '../../utils/fonts.ts';

const MONO = { fontFamily: MONO_FONT };

// Signing in happens in a new tab: the host redirects there to its sign-in page and back to the
// editor, while this page keeps the edits that were not saved yet.
const SessionExpiredDialog = () => {
  const { t } = useI18n();
  const [open, setOpen] = useState(false);

  useEffect(() => onSessionExpired(() => setOpen(true)), []);

  return (
    <Dialog open={open} onClose={() => setOpen(false)} maxWidth="xs" fullWidth>
      <DialogTitle sx={{ ...MONO, fontSize: '1rem' }}>{t.sessionExpiredTitle}</DialogTitle>
      <DialogContent>
        <DialogContentText sx={{ ...MONO, fontSize: '0.8rem' }}>{t.sessionExpiredBody}</DialogContentText>
      </DialogContent>
      <DialogActions>
        <Button
          variant="outlined"
          href={window.location.href}
          target="_blank"
          rel="noopener"
          sx={{ ...MONO, fontSize: '0.75rem', textTransform: 'none' }}
        >
          {t.signInAgain}
        </Button>
        <Button
          onClick={() => setOpen(false)}
          sx={{ ...MONO, fontSize: '0.75rem', textTransform: 'none' }}
        >
          {t.sessionExpiredContinue}
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export default SessionExpiredDialog;

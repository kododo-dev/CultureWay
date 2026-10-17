import { useI18n as useShellI18n } from '@kododo-dev/way-shell';
import { languages, en } from './translations.ts';

/**
 * The editor's strings in the language the shell shows: the user's choice in the shell, then the host's
 * (ShellOptions.Language), then the browser's. Switching it is the shell's job.
 */
export const useI18n = () => {
  const { language, setLanguage } = useShellI18n();
  const lang = languages[language] ? language : 'en';
  return { t: languages[lang] ?? en, lang, setLang: setLanguage };
};

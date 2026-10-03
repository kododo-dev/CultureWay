import React, { createContext, useContext, useEffect, useState } from 'react';
import { type Translations, languages, en } from './translations.ts';
import { useEditorSettings } from '../context/EditorSettingsContext.tsx';

interface I18nContextValue {
  t: Translations;
  lang: string;
  setLang: (lang: string) => void;
}

const I18nContext = createContext<I18nContextValue>({
  t: en,
  lang: 'en',
  setLang: () => {},
});

function supportedCode(tag: string | null | undefined): string | null {
  const code = tag?.split('-')[0].toLowerCase();
  return code && languages[code] ? code : null;
}

function browserLanguage(): string {
  const preferred = navigator.languages?.length ? navigator.languages : [navigator.language];
  for (const tag of preferred) {
    const code = supportedCode(tag);
    if (code) return code;
  }
  return 'en';
}

export const I18nProvider = ({ children }: { children: React.ReactNode }) => {
  const { language } = useEditorSettings();
  const [chosen, setChosen] = useState<string | null>(null);
  const [browser] = useState(browserLanguage);

  // A language picked in the editor wins, then the host's choice, then the browser's language.
  const lang = chosen ?? supportedCode(language) ?? browser;

  const setLang = (l: string) => {
    if (languages[l]) setChosen(l);
  };

  useEffect(() => {
    document.documentElement.lang = lang;
  }, [lang]);

  return (
    <I18nContext.Provider value={{ t: languages[lang] ?? en, lang, setLang }}>
      {children}
    </I18nContext.Provider>
  );
};

export const useI18n = () => useContext(I18nContext);

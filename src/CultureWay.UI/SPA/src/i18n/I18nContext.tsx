import React, { createContext, useContext, useState } from 'react';
import { type Translations, languages, en } from './translations.ts';

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

export const I18nProvider = ({ children }: { children: React.ReactNode }) => {
  const [lang, setLangState] = useState('en');

  const setLang = (l: string) => {
    if (languages[l]) setLangState(l);
  };

  return (
    <I18nContext.Provider value={{ t: languages[lang] ?? en, lang, setLang }}>
      {children}
    </I18nContext.Provider>
  );
};

export const useI18n = () => useContext(I18nContext);

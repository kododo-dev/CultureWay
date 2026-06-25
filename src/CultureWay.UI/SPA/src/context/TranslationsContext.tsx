import React, { createContext, useCallback, useContext, useEffect, useState } from 'react';
import { fetchCultures, fetchTranslations } from '../api/api.ts';
import type { CultureDto, TranslationDto } from '../api/api.model.ts';

interface TranslationsContextValue {
  translations: TranslationDto[];
  cultures: CultureDto[];
  loading: boolean;
  error: string | null;
  reload: () => Promise<void>;
}

const TranslationsContext = createContext<TranslationsContextValue>({
  translations: [],
  cultures: [],
  loading: false,
  error: null,
  reload: async () => {},
});

export const TranslationsProvider = ({ children }: { children: React.ReactNode }) => {
  const [translations, setTranslations] = useState<TranslationDto[]>([]);
  const [cultures, setCultures] = useState<CultureDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const reload = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const [t, c] = await Promise.all([fetchTranslations(), fetchCultures()]);
      setTranslations(t);
      setCultures(c);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { void reload(); }, [reload]);

  return (
    <TranslationsContext.Provider value={{ translations, cultures, loading, error, reload }}>
      {children}
    </TranslationsContext.Provider>
  );
};

export const useTranslations = () => useContext(TranslationsContext);

import React, { createContext, useCallback, useContext, useEffect, useRef, useState } from 'react';
import { fetchCultures, fetchTranslations } from '../api/api.ts';
import type { CultureDto, TranslationDto } from '../api/api.model.ts';

interface TranslationsContextValue {
  translations: TranslationDto[];
  cultures: CultureDto[];
  loading: boolean;
  error: string | null;
  reload: () => Promise<void>;
  hiddenCultures: Set<string>;
  toggleCultureVisibility: (code: string) => void;
}

const HIDDEN_STORAGE_KEY = 'cultureway.hiddenCultures';

function loadHidden(): Set<string> {
  try {
    const raw = localStorage.getItem(HIDDEN_STORAGE_KEY);
    const parsed: unknown = raw ? JSON.parse(raw) : [];
    return new Set(Array.isArray(parsed) ? parsed.filter((x): x is string => typeof x === 'string') : []);
  } catch {
    return new Set();
  }
}

const TranslationsContext = createContext<TranslationsContextValue>({
  translations: [],
  cultures: [],
  loading: false,
  error: null,
  reload: async () => {},
  hiddenCultures: new Set(),
  toggleCultureVisibility: () => {},
});

export const TranslationsProvider = ({ children }: { children: React.ReactNode }) => {
  const [translations, setTranslations] = useState<TranslationDto[]>([]);
  const [cultures, setCultures] = useState<CultureDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const loadedOnce = useRef(false);
  const [hiddenCultures, setHiddenCultures] = useState<Set<string>>(loadHidden);

  const toggleCultureVisibility = useCallback((code: string) => {
    setHiddenCultures(prev => {
      const next = new Set(prev);
      if (next.has(code)) next.delete(code); else next.add(code);
      try { localStorage.setItem(HIDDEN_STORAGE_KEY, JSON.stringify([...next])); } catch { /* ignore */ }
      return next;
    });
  }, []);

  const reload = useCallback(async () => {
    try {
      if (!loadedOnce.current) setLoading(true);
      setError(null);
      const [t, c] = await Promise.all([fetchTranslations(), fetchCultures()]);
      setTranslations(t);
      setCultures(c);
      loadedOnce.current = true;
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { void reload(); }, [reload]);

  return (
    <TranslationsContext.Provider value={{ translations, cultures, loading, error, reload, hiddenCultures, toggleCultureVisibility }}>
      {children}
    </TranslationsContext.Provider>
  );
};

export const useTranslations = () => useContext(TranslationsContext);

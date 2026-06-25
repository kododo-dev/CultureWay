import type { CultureDto, TranslationDto, TranslationKeyDto } from './api.model.ts';

const BASE = `${import.meta.env.BASE_URL}api`;

async function post<T>(path: string, body?: unknown): Promise<T> {
  const res = await fetch(`${BASE}${path}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body ?? {}),
  });

  if (!res.ok) throw new Error(`${res.status} ${res.statusText}`);

  const text = await res.text();
  return (text ? JSON.parse(text) : null) as T;
}

export const fetchTranslations = (): Promise<TranslationDto[]> =>
  post<TranslationDto[]>('/GetTranslations');

export const fetchCultures = (): Promise<CultureDto[]> =>
  post<CultureDto[]>('/GetCultures');

export const saveTranslations = (
  translations: TranslationDto[],
  keysToDelete: TranslationKeyDto[],
): Promise<string[]> =>
  post<string[]>('/UpdateTranslations', { translations, keysToDelete });

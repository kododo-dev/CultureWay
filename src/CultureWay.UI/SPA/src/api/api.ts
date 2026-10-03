import type { CultureDto, EditorSettingsDto, TranslationDto, TranslationKeyDto } from './api.model.ts';

const BASE = `${import.meta.env.BASE_URL}api`;

/** Thrown when the host no longer accepts the user's session, so the request never reached the API. */
export class SessionExpiredError extends Error {
  constructor() {
    super('Session expired');
  }
}

/** Thrown when the host does not let the current user make this request. */
export class ForbiddenError extends Error {
  constructor() {
    super('Forbidden');
  }
}

const sessionExpiredListeners = new Set<() => void>();

export function onSessionExpired(listener: () => void): () => void {
  sessionExpiredListeners.add(listener);
  return () => { sessionExpiredListeners.delete(listener); };
}

async function post<T>(path: string, body?: unknown): Promise<T> {
  const res = await fetch(`${BASE}${path}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body ?? {}),
  });

  // Cookie authentication usually redirects an expired session to a sign-in page, which fetch
  // follows and gets back as HTML; a 401 means the same thing.
  const isJson = res.headers.get('Content-Type')?.includes('application/json') ?? false;
  if (res.status === 401 || (res.ok && res.redirected && !isJson)) {
    sessionExpiredListeners.forEach(listener => listener());
    throw new SessionExpiredError();
  }
  if (res.status === 403) throw new ForbiddenError();
  if (!res.ok) throw new Error(`${res.status} ${res.statusText}`);

  const text = await res.text();
  return (text ? JSON.parse(text) : null) as T;
}

export const fetchEditorSettings = (): Promise<EditorSettingsDto> =>
  post<EditorSettingsDto>('/GetEditorSettings');

export const fetchTranslations = (): Promise<TranslationDto[]> =>
  post<TranslationDto[]>('/GetTranslations');

export const fetchCultures = (): Promise<CultureDto[]> =>
  post<CultureDto[]>('/GetCultures');

export const saveTranslations = (
  translations: TranslationDto[],
  keysToDelete: TranslationKeyDto[],
): Promise<string[]> =>
  post<string[]>('/UpdateTranslations', { translations, keysToDelete });

export const addCulture = (culture: string): Promise<string[]> =>
  post<string[]>('/AddCulture', { culture });

export const deleteCulture = (culture: string): Promise<string[]> =>
  post<string[]>('/DeleteCulture', { culture });

export const setDefaultCulture = (culture: string): Promise<string[]> =>
  post<string[]>('/SetDefaultCulture', { culture });

import type {
  CultureDto,
  EditorSettingsDto,
  ExportFileDto,
  ExportRequest,
  ImportInput,
  ImportPreviewDto,
  ImportResultDto,
  TranslationDto,
  TranslationKeyDto,
} from './api.model.ts';

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

/**
 * The API turned the request down (400, 404, 409) with a problem details response; `messages` are its
 * field errors, or its detail.
 */
export class ApiError extends Error {
  readonly status: number;
  readonly messages: string[];

  constructor(status: number, messages: string[]) {
    super(messages.join(' '));
    this.status = status;
    this.messages = messages;
  }
}

interface ProblemDetails {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
}

async function toApiError(res: Response): Promise<Error> {
  if (!res.headers.get('Content-Type')?.includes('application/problem+json'))
    return new Error(`${res.status} ${res.statusText}`);
  const problem = (await res.json()) as ProblemDetails;
  const messages = problem.errors
    ? Object.values(problem.errors).flat()
    : [problem.detail ?? problem.title ?? `${res.status} ${res.statusText}`];
  return new ApiError(res.status, messages);
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
  if (!res.ok) throw await toApiError(res);

  const text = await res.text();
  return (text ? JSON.parse(text) : null) as T;
}

export const fetchEditorSettings = (): Promise<EditorSettingsDto> =>
  post<EditorSettingsDto>('/GetEditorSettings');

export const fetchTranslations = (): Promise<TranslationDto[]> =>
  post<TranslationDto[]>('/GetTranslations');

export const fetchCultures = (): Promise<CultureDto[]> =>
  post<CultureDto[]>('/GetCultures');

/** Throws {@link ApiError} with the validation messages when the changes are not valid. */
export const saveTranslations = (
  translations: TranslationDto[],
  keysToDelete: TranslationKeyDto[],
): Promise<void> =>
  post<void>('/UpdateTranslations', { translations, keysToDelete });

export const addCulture = (culture: string): Promise<void> =>
  post<void>('/AddCulture', { culture });

export const deleteCulture = (culture: string): Promise<void> =>
  post<void>('/DeleteCulture', { culture });

export const setDefaultCulture = (culture: string): Promise<void> =>
  post<void>('/SetDefaultCulture', { culture });

export const exportTranslations = (request: ExportRequest): Promise<ExportFileDto> =>
  post<ExportFileDto>('/ExportTranslations', request);

export const previewImport = (input: ImportInput): Promise<ImportPreviewDto> =>
  post<ImportPreviewDto>('/PreviewImport', input);

export const applyImport = (input: ImportInput): Promise<ImportResultDto> =>
  post<ImportResultDto>('/ApplyImport', input);

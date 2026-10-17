import { createContext, useContext } from 'react';
import type { WayApi } from '@kododo-dev/way-shell';
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

// Errors come from the shell's API client: ApiError (problem details), ForbiddenError, and SessionExpiredError,
// for which the shell shows its own dialog.
export { ApiError, ForbiddenError, SessionExpiredError } from '@kododo-dev/way-shell';

/** The editor's requests over the shell's API client. */
export function createEditorApi(api: WayApi) {
  return {
    fetchEditorSettings: (): Promise<EditorSettingsDto> =>
      api.post<EditorSettingsDto>('GetEditorSettings'),

    fetchTranslations: (): Promise<TranslationDto[]> =>
      api.post<TranslationDto[]>('GetTranslations'),

    fetchCultures: (): Promise<CultureDto[]> =>
      api.post<CultureDto[]>('GetCultures'),

    /** Throws {@link ApiError} with the validation messages when the changes are not valid. */
    saveTranslations: (translations: TranslationDto[], keysToDelete: TranslationKeyDto[]): Promise<void> =>
      api.post<void>('UpdateTranslations', { translations, keysToDelete }),

    addCulture: (culture: string): Promise<void> =>
      api.post<void>('AddCulture', { culture }),

    deleteCulture: (culture: string): Promise<void> =>
      api.post<void>('DeleteCulture', { culture }),

    setDefaultCulture: (culture: string): Promise<void> =>
      api.post<void>('SetDefaultCulture', { culture }),

    exportTranslations: (request: ExportRequest): Promise<ExportFileDto> =>
      api.post<ExportFileDto>('ExportTranslations', request),

    previewImport: (input: ImportInput): Promise<ImportPreviewDto> =>
      api.post<ImportPreviewDto>('PreviewImport', input),

    applyImport: (input: ImportInput): Promise<ImportResultDto> =>
      api.post<ImportResultDto>('ApplyImport', input),
  };
}

export type EditorApi = ReturnType<typeof createEditorApi>;

export const EditorApiContext = createContext<EditorApi | null>(null);

export function useEditorApi(): EditorApi {
  const api = useContext(EditorApiContext);
  if (!api) throw new Error('useEditorApi() outside of the editor.');
  return api;
}

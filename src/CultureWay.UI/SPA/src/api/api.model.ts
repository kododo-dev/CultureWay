export interface TranslationDto {
  key: string;
  culture: string;
  value: string;
  hasExternalDefault: boolean;
  externalDefaultValue: string | null;
}

export interface CultureDto {
  code: string;
  isDefault: boolean;
}

export interface TranslationKeyDto {
  key: string;
  culture: string;
}

export interface EditorLinkDto {
  label: string;
  url: string;
  icon: string | null;
}

export interface EditorUserDto {
  name: string;
  accountUrl: string | null;
  signOutUrl: string | null;
}

export interface EditorSettingsDto {
  title: string | null;
  homeUrl: string | null;
  language: string | null;
  links: EditorLinkDto[];
  user: EditorUserDto | null;
  canManageCultures: boolean;
  canImport: boolean;
  maxImportBytes: number;
}

export interface ExportFileDto {
  error: string | null;
  fileName: string;
  contentType: string;
  contentBase64: string;
}

export interface ImportChangeDto {
  key: string;
  culture: string;
  oldValue: string | null;
  newValue: string;
}

export interface ImportProblemDto {
  location: string;
  reason: string;
}

export interface ImportPreviewDto {
  error: string | null;
  changes: ImportChangeDto[];
  unchanged: number;
  kept: number;
  problems: ImportProblemDto[];
  unknownCultures: string[];
}

export interface ImportResultDto {
  error: string | null;
  added: number;
  updated: number;
}

/** A file to import, as both preview and apply take it. */
export interface ImportInput {
  fileName: string;
  contentBase64: string;
  culture: string | null;
  onlyMissing: boolean;
}

export interface ExportRequest {
  format: 'json' | 'xlsx';
  cultures: string[];
  /** null: every key; '': keys without a namespace; otherwise that namespace and the nested ones. */
  namespace: string | null;
  missingIn: string | null;
}

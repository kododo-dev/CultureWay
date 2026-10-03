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
}

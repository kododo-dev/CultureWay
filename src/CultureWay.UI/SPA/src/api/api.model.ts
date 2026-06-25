export interface TranslationDto {
  key: string;
  culture: string;
  value: string;
}

export interface CultureDto {
  code: string;
  isDefault: boolean;
}

export interface TranslationKeyDto {
  key: string;
  culture: string;
}

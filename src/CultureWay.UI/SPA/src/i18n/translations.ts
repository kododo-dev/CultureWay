export interface Translations {
  appTitle: string;
  loading: string;
  save: string;
  saving: string;
  saved: string;
  addKey: string;
  deleteKey: string;
  newKeyPlaceholder: string;
  key: string;
  noTranslations: string;
  switchToLight: string;
  switchToDark: string;
  errors: string;
  confirmDelete: string;
  unsavedChanges: string;
  filterPlaceholder: string;
  clearFilter: string;
}

export const en: Translations = {
  appTitle: 'CultureWay',
  loading: 'Loading...',
  save: 'Save',
  saving: 'Saving...',
  saved: 'Saved',
  addKey: 'Add key',
  deleteKey: 'Delete key',
  newKeyPlaceholder: 'New key name...',
  key: 'Key',
  noTranslations: 'No translations yet. Add your first key above.',
  switchToLight: 'Switch to light mode',
  switchToDark: 'Switch to dark mode',
  errors: 'Errors',
  confirmDelete: 'Delete this key for all cultures?',
  unsavedChanges: 'unsaved changes',
  filterPlaceholder: 'Filter keys...',
  clearFilter: 'Clear filter',
};

export const pl: Translations = {
  appTitle: 'CultureWay',
  loading: 'Ładowanie...',
  save: 'Zapisz',
  saving: 'Zapisywanie...',
  saved: 'Zapisano',
  addKey: 'Dodaj klucz',
  deleteKey: 'Usuń klucz',
  newKeyPlaceholder: 'Nazwa nowego klucza...',
  key: 'Klucz',
  noTranslations: 'Brak tłumaczeń. Dodaj pierwszy klucz powyżej.',
  switchToLight: 'Tryb jasny',
  switchToDark: 'Tryb ciemny',
  errors: 'Błędy',
  confirmDelete: 'Usunąć klucz we wszystkich kulturach?',
  unsavedChanges: 'niezapisane zmiany',
  filterPlaceholder: 'Filtruj klucze...',
  clearFilter: 'Wyczyść filtr',
};

export const languages: Record<string, Translations> = { en, pl };

export interface Translations {
  appTitle: string;
  loading: string;
  save: string;
  saving: string;
  saved: string;
  addKey: string;
  deleteKey: string;
  resetToDefault: string;
  resetToDefaultConfirm: string;
  newKeyLabel: string;
  newKeyPlaceholder: string;
  namespaceLabel: string;
  namespacePlaceholder: string;
  namespaceWillBeCreated: string;
  duplicateKey: string;
  key: string;
  noTranslations: string;
  switchToLight: string;
  switchToDark: string;
  errors: string;
  confirmDelete: string;
  unsavedChanges: string;
  searchPlaceholder: string;
  clearFilter: string;
  allNamespaces: string;
  rootNamespace: string;
  namespaces: string;
  cancel: string;
  add: string;
  discard: string;
  selected: string;
  deleteSelected: string;
  resetSelected: string;
  restoreSelected: string;
  clearSelection: string;
  confirmDeleteSelected: string;
  confirmResetSelected: string;
  addLanguage: string;
  cultureCodeLabel: string;
  cultureCodePlaceholder: string;
  duplicateLanguage: string;
  invalidCultureCode: string;
  deleteLanguage: string;
  hideLanguage: string;
  showLanguage: string;
  confirmDeleteLanguage: string;
  manageLanguages: string;
  languagesListTitle: string;
  setAsDefault: string;
  defaultLanguage: string;
  addNewLanguage: string;
  close: string;
}

export const en: Translations = {
  appTitle: 'Translations',
  loading: 'Loading...',
  save: 'Save',
  saving: 'Saving...',
  saved: 'Saved',
  addKey: 'Add key',
  deleteKey: 'Delete key',
  resetToDefault: 'Reset to default',
  resetToDefaultConfirm: 'Remove override and restore the default value?',
  newKeyLabel: 'Key name',
  newKeyPlaceholder: 'e.g. Validation.Required',
  namespaceLabel: 'Namespace',
  namespacePlaceholder: 'e.g. Validation',
  namespaceWillBeCreated: 'This namespace will be created',
  duplicateKey: 'This key already exists',
  key: 'Key',
  noTranslations: 'No translations yet. Add your first key.',
  switchToLight: 'Switch to light mode',
  switchToDark: 'Switch to dark mode',
  errors: 'Errors',
  confirmDelete: 'Delete this key for all cultures?',
  unsavedChanges: 'unsaved',
  searchPlaceholder: 'Search keys and values…',
  clearFilter: 'Clear',
  allNamespaces: 'All',
  rootNamespace: '(root)',
  namespaces: 'Namespaces',
  cancel: 'Cancel',
  add: 'Add',
  discard: 'Discard',
  selected: 'selected',
  deleteSelected: 'Delete',
  resetSelected: 'Reset to default',
  restoreSelected: 'Restore',
  clearSelection: 'Clear selection',
  confirmDeleteSelected: 'Delete the selected keys for all cultures?',
  confirmResetSelected: 'Remove overrides and restore default values for the selected keys?',
  addLanguage: 'Add language',
  cultureCodeLabel: 'Culture code',
  cultureCodePlaceholder: 'e.g. fr, es-ES',
  duplicateLanguage: 'This language is already supported',
  invalidCultureCode: 'Enter a valid culture code (e.g. en, pl, fr-CA)',
  deleteLanguage: 'Delete language',
  hideLanguage: 'Hide in editor',
  showLanguage: 'Show in editor',
  confirmDeleteLanguage: 'Delete this language and all its translations?',
  manageLanguages: 'Manage languages',
  languagesListTitle: 'Languages',
  setAsDefault: 'Set as default',
  defaultLanguage: 'Default',
  addNewLanguage: 'Add new language',
  close: 'Close',
};

export const pl: Translations = {
  appTitle: 'Translations',
  loading: 'Ładowanie...',
  save: 'Zapisz',
  saving: 'Zapisywanie...',
  saved: 'Zapisano',
  addKey: 'Dodaj klucz',
  deleteKey: 'Usuń klucz',
  resetToDefault: 'Przywróć domyślną',
  resetToDefaultConfirm: 'Usunąć nadpisanie i przywrócić wartość domyślną?',
  newKeyLabel: 'Nazwa klucza',
  newKeyPlaceholder: 'np. Walidacja.Wymagane',
  namespaceLabel: 'Namespace',
  namespacePlaceholder: 'np. Walidacja',
  namespaceWillBeCreated: 'Ten namespace zostanie utworzony',
  duplicateKey: 'Ten klucz już istnieje',
  key: 'Klucz',
  noTranslations: 'Brak tłumaczeń. Dodaj pierwszy klucz.',
  switchToLight: 'Tryb jasny',
  switchToDark: 'Tryb ciemny',
  errors: 'Błędy',
  confirmDelete: 'Usunąć klucz we wszystkich kulturach?',
  unsavedChanges: 'niezapisane',
  searchPlaceholder: 'Szukaj kluczy i wartości…',
  clearFilter: 'Wyczyść',
  allNamespaces: 'Wszystkie',
  rootNamespace: '(root)',
  namespaces: 'Przestrzenie nazw',
  cancel: 'Anuluj',
  add: 'Dodaj',
  discard: 'Odrzuć',
  selected: 'zaznaczono',
  deleteSelected: 'Usuń',
  resetSelected: 'Przywróć domyślne',
  restoreSelected: 'Cofnij',
  clearSelection: 'Wyczyść zaznaczenie',
  confirmDeleteSelected: 'Usunąć zaznaczone klucze we wszystkich kulturach?',
  confirmResetSelected: 'Usunąć nadpisania i przywrócić wartości domyślne dla zaznaczonych kluczy?',
  addLanguage: 'Dodaj język',
  cultureCodeLabel: 'Kod kultury',
  cultureCodePlaceholder: 'np. fr, es-ES',
  duplicateLanguage: 'Ten język jest już wspierany',
  invalidCultureCode: 'Podaj poprawny kod kultury (np. en, pl, fr-CA)',
  deleteLanguage: 'Usuń język',
  hideLanguage: 'Ukryj w edytorze',
  showLanguage: 'Pokaż w edytorze',
  confirmDeleteLanguage: 'Usunąć ten język i wszystkie jego tłumaczenia?',
  manageLanguages: 'Zarządzaj językami',
  languagesListTitle: 'Języki',
  setAsDefault: 'Ustaw jako domyślny',
  defaultLanguage: 'Domyślny',
  addNewLanguage: 'Dodaj nowy język',
  close: 'Zamknij',
};

export const languages: Record<string, Translations> = { en, pl };

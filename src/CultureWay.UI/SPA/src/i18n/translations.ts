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
  signOut: string;
  sessionExpiredTitle: string;
  sessionExpiredBody: string;
  sessionExpiredContinue: string;
  signInAgain: string;
  importExport: string;
  exportTab: string;
  importTab: string;
  formatLabel: string;
  formatJson: string;
  formatXlsx: string;
  languageLabel: string;
  keysLabel: string;
  everyNamespace: string;
  selectedNamespaceOnly: string;
  missingInLabel: string;
  missingInAny: string;
  download: string;
  chooseFile: string;
  fileLanguage: string;
  modeOverwrite: string;
  modeOnlyMissing: string;
  preview: string;
  importNothing: string;
  summaryNew: string;
  summaryChanged: string;
  summaryUnchanged: string;
  summaryKept: string;
  summarySkipped: string;
  unknownLanguages: string;
  addTheseLanguages: string;
  importApply: string;
  importDone: string;
  importSaveFirst: string;
  fileTooLarge: string;
  andMore: string;
  skippedTitle: string;
  changesTitle: string;
  emptyValue: string;
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
  signOut: 'Sign out',
  sessionExpiredTitle: 'Session expired',
  sessionExpiredBody: 'You have been signed out. Sign in again in a new tab, then come back here. Your unsaved changes are kept on this page.',
  sessionExpiredContinue: 'Continue',
  signInAgain: 'Sign in in a new tab',
  importExport: 'Import / export',
  exportTab: 'Export',
  importTab: 'Import',
  formatLabel: 'Format',
  formatJson: 'JSON, one language',
  formatXlsx: 'Excel, a column per language',
  languageLabel: 'Language',
  keysLabel: 'Keys',
  everyNamespace: 'All namespaces',
  selectedNamespaceOnly: 'Only the selected namespace',
  missingInLabel: 'Only keys without a translation in',
  missingInAny: '(any key)',
  download: 'Download',
  chooseFile: 'Choose a .json or .xlsx file',
  fileLanguage: 'Language of the file',
  modeOverwrite: 'Add new values and update existing ones',
  modeOnlyMissing: 'Only add missing values',
  preview: 'Preview',
  importNothing: 'Nothing to import. Every value in the file is already here.',
  summaryNew: 'new',
  summaryChanged: 'changed',
  summaryUnchanged: 'unchanged',
  summaryKept: 'kept',
  summarySkipped: 'skipped',
  unknownLanguages: 'Languages the editor does not have (skipped):',
  addTheseLanguages: 'Add these languages',
  importApply: 'Import',
  importDone: 'Imported',
  importSaveFirst: 'Save or discard your changes before importing.',
  fileTooLarge: 'The file is larger than',
  andMore: 'more',
  skippedTitle: 'Skipped',
  changesTitle: 'Changes',
  emptyValue: '(none)',
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
  signOut: 'Wyloguj',
  sessionExpiredTitle: 'Sesja wygasła',
  sessionExpiredBody: 'Sesja się zakończyła. Zaloguj się ponownie w nowej karcie i wróć tutaj. Niezapisane zmiany zostają na tej stronie.',
  sessionExpiredContinue: 'Kontynuuj',
  signInAgain: 'Zaloguj w nowej karcie',
  importExport: 'Import / eksport',
  exportTab: 'Eksport',
  importTab: 'Import',
  formatLabel: 'Format',
  formatJson: 'JSON, jeden język',
  formatXlsx: 'Excel, kolumna na język',
  languageLabel: 'Język',
  keysLabel: 'Klucze',
  everyNamespace: 'Wszystkie przestrzenie nazw',
  selectedNamespaceOnly: 'Tylko wybrana przestrzeń nazw',
  missingInLabel: 'Tylko klucze bez tłumaczenia na',
  missingInAny: '(dowolny klucz)',
  download: 'Pobierz',
  chooseFile: 'Wybierz plik .json lub .xlsx',
  fileLanguage: 'Język pliku',
  modeOverwrite: 'Dodaj nowe wartości i zaktualizuj istniejące',
  modeOnlyMissing: 'Dodaj tylko brakujące wartości',
  preview: 'Podgląd',
  importNothing: 'Nie ma czego importować. Wszystkie wartości z pliku już tu są.',
  summaryNew: 'nowe',
  summaryChanged: 'zmienione',
  summaryUnchanged: 'bez zmian',
  summaryKept: 'pozostawione',
  summarySkipped: 'pominięte',
  unknownLanguages: 'Języki, których nie ma w edytorze (pominięte):',
  addTheseLanguages: 'Dodaj te języki',
  importApply: 'Importuj',
  importDone: 'Zaimportowano',
  importSaveFirst: 'Zapisz lub odrzuć zmiany przed importem.',
  fileTooLarge: 'Plik jest większy niż',
  andMore: 'więcej',
  skippedTitle: 'Pominięte',
  changesTitle: 'Zmiany',
  emptyValue: '(brak)',
};

export const languages: Record<string, Translations> = { en, pl };

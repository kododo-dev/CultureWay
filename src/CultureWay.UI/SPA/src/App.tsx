import { ThemeProvider, CssBaseline } from '@mui/material';
import { ThemeContextProvider, useThemeMode } from './context/ThemeContext.tsx';
import { I18nProvider } from './i18n/I18nContext.tsx';
import { EditorSettingsProvider } from './context/EditorSettingsContext.tsx';
import { TranslationsProvider } from './context/TranslationsContext.tsx';
import { NamespaceProvider } from './context/NamespaceContext.tsx';
import MainLayout from './components/layout/MainLayout.tsx';
import TranslationsPage from './pages/TranslationsPage.tsx';

function AppInner() {
  const { muiTheme } = useThemeMode();
  return (
    <ThemeProvider theme={muiTheme}>
      <CssBaseline />
      <TranslationsProvider>
        <NamespaceProvider>
          <MainLayout>
            <TranslationsPage />
          </MainLayout>
        </NamespaceProvider>
      </TranslationsProvider>
    </ThemeProvider>
  );
}

function App() {
  return (
    <ThemeContextProvider>
      <EditorSettingsProvider>
        <I18nProvider>
          <AppInner />
        </I18nProvider>
      </EditorSettingsProvider>
    </ThemeContextProvider>
  );
}

export default App;

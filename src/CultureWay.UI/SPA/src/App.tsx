import { useMemo } from 'react';
import { useShell } from '@kododo-dev/way-shell';
import { EditorApiContext, createEditorApi } from './api/api.ts';
import { EditorSettingsProvider } from './context/EditorSettingsContext.tsx';
import { TranslationsProvider } from './context/TranslationsContext.tsx';
import { NamespaceProvider } from './context/NamespaceContext.tsx';
import SideMenu from './components/layout/SideMenu.tsx';
import TranslationsPage from './pages/TranslationsPage.tsx';

// The editor inside Way.Shell: the shell provides the router, theme, language, layout and the session dialog; the
// namespaces go into the shell's side menu.
function App() {
  const { api } = useShell();
  const editorApi = useMemo(() => createEditorApi(api), [api]);
  return (
    <EditorApiContext.Provider value={editorApi}>
      <EditorSettingsProvider>
        <TranslationsProvider>
          <NamespaceProvider>
            <SideMenu />
            <TranslationsPage />
          </NamespaceProvider>
        </TranslationsProvider>
      </EditorSettingsProvider>
    </EditorApiContext.Provider>
  );
}

export default App;

import React, { createContext, useContext, useEffect, useState } from 'react';
import { fetchEditorSettings } from '../api/api.ts';
import type { EditorSettingsDto } from '../api/api.model.ts';

/** What the editor shows when the host sets nothing, or before its settings arrive. */
const defaults: EditorSettingsDto = {
  title: null,
  homeUrl: null,
  language: null,
  links: [],
  user: null,
  canManageCultures: true,
};

const EditorSettingsContext = createContext<EditorSettingsDto>(defaults);

export const EditorSettingsProvider = ({ children }: { children: React.ReactNode }) => {
  const [settings, setSettings] = useState<EditorSettingsDto>(defaults);

  useEffect(() => {
    // Without the host's settings the editor still works, so a failure keeps the defaults.
    fetchEditorSettings().then(setSettings, () => {});
  }, []);

  return (
    <EditorSettingsContext.Provider value={settings}>
      {children}
    </EditorSettingsContext.Provider>
  );
};

export const useEditorSettings = () => useContext(EditorSettingsContext);

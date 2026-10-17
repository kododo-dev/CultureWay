import { defineWay } from '@kododo-dev/way-shell'
import App from './App.tsx'
import { cultureWayTheme } from './theme.ts'

// The module Way.Shell loads (built by `npm run build` into dist/way.js).
export default defineWay({ App, theme: cultureWayTheme })

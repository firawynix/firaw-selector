# Browser Extension Flow
> Links internos chegam ao seletor pelo Native Messaging

Entry: `extensions/shared/content.js`
Flow: captura de `click`/`auxclick` → `extensions/shared/background.js` →
`FirawSelectorHost.cs:ProgramaHost.Main()` → `FirawSelector.exe --ask` →
`FirawSelector.cs:Programa.Main()` → `Escolha` → `Navegador.Abrir()`

Pacotes: `extensions/chrome/manifest.json`, `extensions/edge/manifest.json`,
`extensions/firefox/manifest.json`
- Chrome + Edge local ID: `Amb.ExtensaoChromiumId`
- Firefox ID: `Amb.ExtensaoFirefoxId`
- IDs de lojas Chromium podem exigir nova origem em
  `FirawSelectorSetup.cs:RegistraHostNativo()`

Instalação: `FirawSelectorSetup.cs:Instalador.Instalar()`
- Extrai host + extensões em `Extensoes/`
- Gera manifestos nativos em `Integracao/`
- Registra Chrome, Edge e Firefox em HKCU

Build: `build.cmd` → executáveis + três ZIPs em `dist/`

Updated: 2026-09-13

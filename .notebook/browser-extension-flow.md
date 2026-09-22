# Browser Extension Flow
> Links internos chegam ao seletor pelo Native Messaging

Entry: `extensions/shared/content.js`
Flow: captura de `click`/`auxclick` → `extensions/shared/background.js` →
`FirawSelectorHost.cs:ProgramaHost.Main()` → `FirawSelector.exe --ask` →
`FirawSelector.cs:Programa.Main()` → `Escolha` → `Navegador.Abrir()`

Pacotes: `extensions/chrome/manifest.json`, `extensions/edge/manifest.json`,
`extensions/opera/manifest.json`, `extensions/firefox/manifest.json`
- Chrome + Edge + Opera local ID: `Amb.ExtensaoChromiumId`
- Firefox ID: `Amb.ExtensaoFirefoxId`
- IDs de lojas Chromium podem exigir nova origem em
  `FirawSelectorSetup.cs:RegistraHostNativo()`
- Mozilla: URL clicada passa pelo Native Messaging local. O manifesto declara
  `websiteActivity` e requer Firefox 142+; veja
  `extensions/firefox/manifest.json`.

Instalação: `FirawSelectorSetup.cs:Instalador.Instalar()`
- Extrai host + extensões em `Extensoes/`
- Gera manifestos nativos em `Integracao/`
- Registra Chrome, Edge e Firefox em HKCU. Opera lê a ponte Chromium no teste
  local; o ID atribuído pela loja ainda precisa ser confirmado.

Build: `build.cmd` → executáveis + quatro ZIPs em `dist/`

Updated: 2026-09-22

# Browser Extension Flow
> Links internos via Native Messaging (clássico) ou protocolo MSIX

Entry: `extensions/shared/content.js`
Flow: captura de `click`/`auxclick` → `extensions/shared/background.js` →
`FirawSelectorHost.cs:ProgramaHost.Main()` → `FirawSelector.exe URL` →
`FirawSelector.cs:Programa.Main()` → regras/lembrados ou `Escolha` → `Navegador.Abrir()`

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

Store MSIX: `tools/build-msix.ps1` inclui motor + Studio, mas não inclui
`FirawSelector Host.exe`. `packaging/windows/msix/AppxManifest.xml.in` declara
associações de URL/HTML, não Native Messaging. Em 2026-10-14, instalação
MSIX 1.1.12 estava presente e as chaves HKCU de NativeMessagingHosts apontavam
para manifestos ausentes em `%LOCALAPPDATA%\FirawSelector\Integracao`; portanto
as extensões não alcançam o app Store por essa ponte. A edição clássica do Center
usa `FirawSelectorSetup.cs:RegistraHostNativo()` e funciona quando seus arquivos
estão presentes. Compatibilidade Store exige um canal adicional verificável;
alterar apenas o manifesto da extensão não recria o host ausente.

1.1.13: manifesto MSIX registra `firawselector://open/?url=...` →
`Programa.LinkDoProtocolo()` aceita só HTTP(S) → segue as regras existentes.
Extensão 1.1.3: falha nativa mostra link real para protocolo + caixa lembrar.
`content.js`: `storage.local` guarda `siteRoute:<origin>` = store/normal;
cache no clique preserva gesto; `onChanged` atualiza abas abertas.
Opera/WhatsApp lateral: usuário relatou falha no segundo clique via assign
direto (1.1.1) e via href temporário do clique padrão (1.1.2).
1.1.3: todo clique confiável chama background, inclusive rota Store lembrada.
`background.js` valida origem/destino HTTP(S) e devolve protocolo codificado;
renderer navega depois da resposta. Não altera href/target da página.
Hipótese: ativação externa/foco no painel lateral. Fonte Chromium
`chrome/browser/external_protocol/external_protocol_handler.cc` documenta
controle anti-flood e chamadas de extensão; não há mudança de política.
Testes: cliques repetidos acionam background, sintéticos não; validação
do background testada em `tests/background-routing.test.js`.
Resultado interativo da 1.1.3 ainda pendente; não declarar resolvido.
`options.html/js`: popup permite trocar/esquecer por site de origem.
Origem é o site onde clicou; regras no Studio são para o destino/perfil.
Permissão de abrir aplicativo externo pertence ao navegador.
`--ask` continua forçando escolha, mas protocolo e host não o usam mais.
Modo compacto também mostra a caixa de lembrar o domínio do destino.
Testes: `tests/extension-fallback.test.js`; parsing HTTP(S), domínio lembrado,
isolamento entre sites e Shift verificados por reflexão no motor compilado.
Usuário confirmou protocolo MSIX interativo; teste de lembrar ainda pendente.

Build: `build.cmd` → executáveis + quatro ZIPs em `dist/`
- Store ZIPs for Chrome, Edge, and Opera omit the manifest `key` field.
  Local extension copies retain it for the existing native-host identity.
- The Firefox Store manifest uses `FirawSelector` as its display name;
  Mozilla rejects browser trademarks in an add-on name.
- `tools/prepare-store-screenshot.ps1` prepares the Chrome listing image
  and the 64 × 64 Opera icon from existing project artwork.
- The installer native-host manifest now accepts the local Chromium extension
  ID and the assigned Chrome, Edge, and Opera store IDs. Firefox keeps its
  fixed extension ID. Verify the installed store extensions against these IDs
  before promoting the updated installer to the site or Microsoft Store.

Updated: 2026-10-14

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

Store 1.1.12 não incluía host; HKCU apontava para manifestos clássicos ausentes.
Store 1.1.14: `tools/build-msix.ps1` inclui motor, Studio, host e
`native-host-alias.txt`. `NativeIntegration.cs:IntegracaoStore.Conectar()` exige
confirmação no Studio, salva apontamentos anteriores e registra o próprio host.
Windows 11: exclusões finas em `AppxManifest.xml.in` para três chaves HKCU
NativeMessagingHosts e LocalAppData/FirawSelectorStore. Capacidade nova:
`unvirtualizedResources`; não desativa virtualização globalmente.
`AtualizaSeAtiva()` não substitui apontamentos alterados por instalação clássica.
`Desconectar()` restaura somente os apontamentos que ainda sejam seus.
Host via caminho direto WindowsApps falhou com Access Denied no teste.
Alias console MSIX estável respondeu ping stdio com versão 1.1.14.
Alias Lab separado evita conflito com produção. Host ativa protocolo via Shell
para manter identidade/configuração do pacote; não navega no renderer do browser.

1.1.13: manifesto MSIX registra `firawselector://open/?url=...` →
`Programa.LinkDoProtocolo()` aceita só HTTP(S) → segue as regras existentes.
Extensão 1.1.4: todo clique usa Native Messaging; falha mostra protocolo manual.
`content.js`: `storage.local` guarda `siteRoute:<origin>` = store/normal;
cache no clique preserva gesto; `onChanged` atualiza abas abertas.
Opera/WhatsApp lateral: usuário relatou falha no segundo clique nas tentativas
por protocolo 1.1.1, 1.1.2 e 1.1.3. Não publicar essas tentativas.
`background.js` valida origem/destino HTTP(S) e chama host local em cada clique.
Não altera href/target nem navega automaticamente por protocolo no renderer.
Hipótese: ativação externa/foco no painel lateral. Fonte Chromium
`chrome/browser/external_protocol/external_protocol_handler.cc` documenta
controle anti-flood e chamadas de extensão; não há mudança de política.
Testes: cliques repetidos acionam background, sintéticos não; validação
do background testada em `tests/background-routing.test.js`.
2026-10-14: usuário confirmou seletor em vários cliques/links no WhatsApp lateral
após trocar cópia local sem key pela cópia com key; erro do diagnóstico separado.
`options.html/js`: popup permite trocar/esquecer por site de origem.
Origem é o site onde clicou; regras no Studio são para o destino/perfil.
Permissão de abrir aplicativo externo pertence ao navegador.
`--ask` continua forçando escolha, mas protocolo e host não o usam mais.
Modo compacto também mostra a caixa de lembrar o domínio do destino.
Testes: `tests/extension-fallback.test.js`; parsing HTTP(S), domínio lembrado,
isolamento entre sites e Shift verificados por reflexão no motor compilado.
Usuário confirmou protocolo MSIX, cliques repetidos e ping direto versão 1.1.14.
Teste local Chromium deve carregar `dist/extensions/chrome` (key/ID estável).
`chrome-store` sem key gera ID local fora do allowed_origins; só usar ZIP sem
key na loja, que atribui seu próprio ID conhecido.
ID local kolmilnfjchhnciifefpkhnoacppjpjm corresponde ao hash do caminho UTF-16
de dist/extensions/chrome-store. Não adicionar ID aleatório à lista pública.
`options.js`: ping usa sendNativeMessage diretamente (API disponível em página
da extensão); canal interno sendMessage fechava sem resposta no Opera do usuário.
`background.js`: bridgeLastOpen guarda só último horário/estado/versão, não URL.
`tests/options-diagnostic.test.js`: ping direto, falha e horário completo.
Usuário confirmou ping direto e último clique aceito pelo host 1.1.14.

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

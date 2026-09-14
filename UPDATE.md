# FirawSelector 1.1.1 — instalação sem janela (Firawynix Center)

O instalador passou a aceitar a linha de comando no formato do NSIS, que é como
o Firawynix Center instala e **atualiza por cima** sem abrir janela:

| Comando | O que faz |
|---|---|
| `FirawSelector-Setup.exe /S` | instala ou atualiza na pasta já instalada (ou na padrão), sem janela |
| `FirawSelector-Setup.exe /S /D=C:\Outra pasta` | idem, na pasta dada (`/D=` por último, sem aspas) |
| `Desinstalar.exe /S` | remove sem perguntar e sem aviso no fim |

- Sem janela não cria atalho: o Center cria os dele, e quem instalou à mão já
  tem os seus. O registro como navegador e o host das extensões são feitos
  normalmente.
- Código de saída: `0` ok, `2` arquivo em uso (feche o FirawSelector), `1`
  outra falha. Cada execução deixa uma linha em `%TEMP%\firawselector-setup.log`.
- `Desinstalar.exe` se reconhece pelo nome: chamado só com `/S` (sem o
  `--uninstall` do registro), ele remove em vez de reinstalar.
- Cada atualização regrava `%LOCALAPPDATA%\FirawSelector\Extensoes`: quem
  carregou a extensão por essa pasta recebe a versão nova ao reiniciar o
  navegador.

No Center o item passou a `win_instalador=nsis` (antes `manual`): ele compara a
`DisplayVersion` instalada com a da última release e roda o instalador novo
com `/S` antes de abrir. Os três ZIPs das extensões aparecem no item como
links para `releases/latest/download/`, sempre a versão mais nova.

# FirawSelector 1.1.0 — extensões de navegador

Esta atualização adiciona integração com Google Chrome, Microsoft Edge e
Mozilla Firefox. Links clicados dentro desses navegadores agora podem chamar a
mesma janela de escolha do FirawSelector usada pelos links externos do Windows.

## Arquivos para publicar

Depois de executar `build.cmd`, publique os arquivos da pasta `dist`:

| Arquivo | Destino |
|---|---|
| `FirawSelector-Setup.exe` | Instalador principal recomendado |
| `FirawSelector.exe` | Motor portátil/avulso |
| `FirawSelector-Studio.exe` | Configurador avulso |
| `FirawSelector-Host.exe` | Ponte nativa avulsa para diagnóstico |
| `FirawSelector-Chrome.zip` | Pacote da extensão do Chrome |
| `FirawSelector-Edge.zip` | Pacote da extensão do Edge |
| `FirawSelector-Firefox.zip` | Pacote da extensão do Firefox |

O instalador principal já contém o motor, o Studio, o host nativo e as três
extensões. Não é necessário distribuir o host separadamente para uma instalação
normal.

## O que mudou no aplicativo

- Versão atualizada de `1.0.6` para `1.1.0`.
- Novo comando `FirawSelector.exe --ask <url>` para abrir a janela de escolha
  sem executar regras automáticas.
- Novo `FirawSelector Host.exe`, compatível com o protocolo Native Messaging.
- O instalador registra o host para Chrome, Edge e Firefox no perfil do usuário.
- O instalador copia as extensões para
  `%LOCALAPPDATA%\FirawSelector\Extensoes`.
- A desinstalação também remove os três registros de Native Messaging.
- As regras, navegadores, perfis e o `config.ini` existentes são preservados na
  atualização.

## Instalação para teste local

Primeiro execute `FirawSelector-Setup.exe`. Depois carregue a extensão desejada:

### Google Chrome

1. Abra `chrome://extensions`.
2. Ative **Modo do desenvolvedor**.
3. Clique em **Carregar sem compactação**.
4. Selecione `%LOCALAPPDATA%\FirawSelector\Extensoes\Chrome`.

ID fixo do pacote local: `ffjnechlggkdfgiooihhflkbbmlcblho`.

### Microsoft Edge

1. Abra `edge://extensions`.
2. Ative **Modo do desenvolvedor**.
3. Clique em **Carregar sem pacote**.
4. Selecione `%LOCALAPPDATA%\FirawSelector\Extensoes\Edge`.

ID fixo do pacote local: `ffjnechlggkdfgiooihhflkbbmlcblho`.

### Mozilla Firefox

1. Abra `about:debugging#/runtime/this-firefox`.
2. Clique em **Carregar extensão temporária**.
3. Selecione
   `%LOCALAPPDATA%\FirawSelector\Extensoes\Firefox\manifest.json`.

ID da extensão: `firaw-selector@firawynix.com.br`. A instalação permanente no
Firefox exige assinatura pela Mozilla.

## Publicação nas lojas

Os três ZIPs têm o `manifest.json` na raiz e estão prontos para envio aos
portais das lojas. Existe, porém, uma etapa obrigatória depois do primeiro envio:

1. Anote o ID definitivo atribuído pela Chrome Web Store e pelo Microsoft Edge
   Add-ons.
2. Se algum ID for diferente de `ffjnechlggkdfgiooihhflkbbmlcblho`, adicione a
   origem `chrome-extension://ID_DA_LOJA/` à lista `allowed_origins` criada em
   `FirawSelectorSetup.cs:RegistraHostNativo()`.
3. Gere e publique um novo instalador. O Native Messaging não aceita curingas e
   só autoriza IDs declarados explicitamente.
4. Envie o ZIP do Firefox para assinatura no AMO mantendo o ID
   `firaw-selector@firawynix.com.br`.

O campo `key` dos pacotes Chromium mantém o ID estável quando a extensão é
carregada localmente. A loja ainda pode atribuir um ID próprio.

## Comportamento e limites

- Intercepta links HTTP/HTTPS acionados por clique comum, clique modificado,
  teclado ou botão do meio.
- Funciona em frames nos quais a extensão tem acesso.
- Não intercepta âncoras que apenas movimentam a página atual.
- Não intercepta a barra de endereços, páginas internas do navegador ou
  navegações realizadas inteiramente por JavaScript sem um link HTML.
- Se a ponte nativa estiver indisponível, a extensão informa o erro e oferece
  abrir o endereço normalmente.

## Checklist antes de publicar

- [ ] Executar `build.cmd` sem erros.
- [ ] Instalar `dist\FirawSelector-Setup.exe` sobre uma instalação existente.
- [ ] Confirmar que regras e navegadores existentes foram preservados.
- [ ] Carregar cada extensão e clicar em um link HTTP/HTTPS comum.
- [ ] Confirmar que a janela de escolha aparece mesmo quando existe uma regra.
- [ ] Escolher Chrome, Edge, Firefox e pelo menos um perfil configurado.
- [ ] Cancelar a escolha e confirmar que o link não abre.
- [ ] Desativar o host ou remover o aplicativo e validar a mensagem de fallback.
- [ ] Conferir os IDs definitivos das lojas antes do lançamento público.

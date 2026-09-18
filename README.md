# FirawSelector

Cada link abre no navegador **certo** — por regra, ou perguntando na hora.

O FirawSelector se registra como navegador do Windows. Quando qualquer programa abre um link (Outlook, Teams, Slack, WhatsApp, um `.html` clicado, a Pesquisa do Windows), ele intercepta e decide:

Também há uma edição nativa para Linux em `linux/`: ela registra um manipulador
XDG para HTTP/HTTPS, detecta os navegadores instalados e usa o mesmo modelo de
regras. O AppImage publicado pode ser instalado pelo Firawynix Center.

- **casou com uma regra** → vai direto para o navegador daquela regra, sem janela nenhuma
- **nenhuma regra casou** → abre a janela de escolha, com os navegadores instalados

![Janela de escolha](docs/janela-de-escolha.png)

## Por que isso existe

O Windows só aceita **um** navegador padrão. Mas o dia a dia não é assim:

- o sistema interno da empresa só funciona no Edge
- a conta do cliente está no perfil "Trabalho" do Chrome, e a sua no "Pessoal"
- link de rede social você prefere abrir em janela privativa
- e o `microsoft-edge:` da Pesquisa do Windows ignora o navegador padrão de propósito

## O que ele faz

| Recurso | Detalhe |
|---|---|
| **Registro como navegador** | Entra na lista de Aplicativos padrão do Windows (`http`, `https`, `ftp`, `.htm`, `.html`, `.xhtml`) |
| **Regras por trecho** | `contém`: cole um pedaço do link e **tudo que vier depois entra na regra** — é o tipo padrão |
| **Regras por domínio** | `intranet.local`, `*.google.com` — a primeira que casar vence |
| **Regras por endereço inteiro** | `https://*/docs/*`: casa do começo ao fim, com o curinga `*` |
| **Safe Links** | Desembrulha o link do Teams/Outlook **antes de decidir** — a regra é sobre o seu site, não sobre a página de proteção |
| **Regras no navegador** | Dá para criar a regra de dentro da caixa do navegador, sem ir até a aba Regras |
| **Regras por expressão** | Regex ECMAScript no endereço inteiro, quando o curinga não basta |
| **Janela de escolha** | Ícone real de cada navegador, atalho `1`-`9`, `Enter` para o padrão, `Esc` cancela |
| **Modo enxuto** | Só ícone (grande), nome e número — sem endereço, sem caixas, sem botões |
| **Sem moldura** | No modo enxuto, só os navegadores flutuando: fundo, barra de título e botões voltam quando o mouse chega, junto com **Configurações** e **Modo completo** |
| **Troca rápida de modo** | Use **Modo completo** na janela compacta ou **Modo compacto** na janela completa; o seletor reabre o mesmo link e salva a escolha |
| **Ícone por navegador** | Troque por um `.ico`, ou pelo ícone nº _n_ de dentro de um `.exe`/`.dll` |
| **Ordem da lista** | Subir/Descer: quem está em primeiro é o `1` na janela de escolha |
| **Importar / exportar** | Leva a configuração inteira para outra máquina em um `.ini` |
| **Perfis** | Chrome, Edge, Brave e Vivaldi (`--profile-directory`) e Firefox (`-P`) viram entradas próprias |
| **Janela privativa** | Por regra, por marcação na hora, ou segurando `Ctrl` no clique |
| **Lembrar por site** | "Sempre usar este navegador para `dominio.com`" — e dá para esquecer tudo depois |
| **Escolha automática** | Conta regressiva opcional; qualquer tecla ou movimento do mouse cancela |
| **Segurar Shift** | Força a janela de escolha mesmo quando existe regra |
| **Limpar rastreio** | Remove `utm_*`, `fbclid`, `gclid`, `msclkid` e cia. antes de abrir |
| **Expandir encurtados** | Segue o redirecionamento (HEAD, 5 saltos, 2,5 s) e decide pelo destino real |
| **Capturar `microsoft-edge:`** | Assume o protocolo que a Pesquisa e os widgets usam para furar o padrão |
| **Links dentro do navegador** | Extensões para Chrome, Edge e Firefox chamam a mesma janela de escolha em cada clique |
| **Editar / copiar o link** | Antes de abrir, na própria janela de escolha |
| **Atalhos de edição** | Todos os campos aceitam `Ctrl+A`, `Ctrl+C`, `Ctrl+X`, `Ctrl+V`, `Ctrl+Z` e `Ctrl+Y` |
| **Registro em arquivo** | Endereço, navegador e o **motivo** da decisão |
| **Português e inglês** | Troca na hora, ou segue o Windows |
| **Tema ciano** | Escuro, claro ou seguindo o Windows — com barra de título própria |

## Baixar

| Arquivo | Para quê |
|---|---|
| **FirawSelector-Setup.exe** | **Comece por aqui.** Instala, cria atalhos, registra a desinstalação e (opcional) já registra como navegador |
| FirawSelector-Studio.exe | Só o configurador, sem instalar |
| FirawSelector.exe | Só o motor — é ele que o Windows chama com o link |
| FirawSelector-Chrome.zip | Extensão para publicar ou carregar no Google Chrome |
| FirawSelector-Edge.zip | Extensão para publicar ou carregar no Microsoft Edge |
| FirawSelector-Firefox.zip | Extensão para publicar ou carregar no Mozilla Firefox |

Sem assinatura de código: o SmartScreen avisa na primeira vez. **Mais informações › Executar assim mesmo**.

## Requisitos

- Windows 10 ou 11 (funciona no 8.1; abaixo do build 17763 cai no tema claro do shell)
- .NET Framework 4.x — já vem no Windows 10 1903+ e no 11
- 32 ou 64 bits: os binários são **AnyCPU**, o mesmo arquivo serve nos dois

Não precisa de administrador. Instala em `%LOCALAPPDATA%\FirawSelector`.

## Como usar

1. rode o **FirawSelector-Setup.exe** e deixe marcado *Registrar como navegador do Windows*
2. o Windows abre **Configurações › Aplicativos › Aplicativos padrão** — escolha o **FirawSelector** para `http`, `https` e `.html`
3. abra o **FirawSelector Studio**: a aba **Navegadores** já lista o que existe na máquina
4. marque um como **padrão** (é o do `Enter` e o da escolha automática)
5. na aba **Regras**, crie o que for direto: `*.suaempresa.com.br` → Edge, `*.google.com` → Chrome Trabalho
6. teste sem abrir nada: campo **Testar um endereço** mostra quem venceria e por quê

O passo 2 é manual porque a Microsoft não deixa nenhum programa se tornar padrão sozinho — só a tela de Configurações define isso.

![Navegadores](docs/studio-navegadores.png)

![Opções](docs/opcoes.png)

Passo a passo interativo, sem instalar nada: **[firawselector.firawynix.com.br](https://firawselector.firawynix.com.br)**.

## Linha de comando

```
FirawSelector.exe <url> [<url>...]   decide e abre (é o que o Windows chama)
FirawSelector.exe --register         registra como navegador e abre as Configurações
FirawSelector.exe --register --silent  registra sem abrir nada (implantação)
FirawSelector.exe --unregister       tira o registro
FirawSelector.exe --capture-edge     assume o protocolo microsoft-edge:
FirawSelector.exe --release-edge     devolve o protocolo
FirawSelector.exe --settings         abre o Studio
FirawSelector.exe --ask <url>        força a janela de escolha, ignorando regras
FirawSelector.exe --slim <url>       força o modo enxuto só nesta chamada
FirawSelector.exe --completo <url>   força o modo cheio só nesta chamada
```

`--slim` e `--completo` não gravam nada: servem para um atalho que precisa do
outro modo sem mexer na sua preferência.

Vários endereços de uma vez: o primeiro pergunta, os outros seguem a mesma escolha.

## Extensões para links dentro do navegador

O instalador copia três extensões para `%LOCALAPPDATA%\FirawSelector\Extensoes` e
registra automaticamente a ponte nativa usada por elas. A extensão não duplica
regras nem configurações: ela entrega o endereço ao aplicativo e o
FirawSelector abre a mesma janela de escolha de sempre.

Para testar uma versão ainda não publicada:

- **Chrome:** abra `chrome://extensions`, ative o modo do desenvolvedor, escolha
  **Carregar sem compactação** e selecione a pasta `Extensoes\Chrome`.
- **Edge:** abra `edge://extensions`, ative o modo do desenvolvedor, escolha
  **Carregar sem pacote** e selecione a pasta `Extensoes\Edge`.
- **Firefox:** abra `about:debugging#/runtime/this-firefox`, escolha
  **Carregar extensão temporária** e selecione `Extensoes\Firefox\manifest.json`.
  A instalação permanente no Firefox exige uma extensão assinada.

São capturados cliques comuns, `Ctrl`/`Shift` + clique, clique do meio e links em
frames HTTP/HTTPS. Âncoras da própria página continuam funcionando normalmente.
Páginas internas do navegador, links digitados na barra e navegações iniciadas
inteiramente por JavaScript não podem ser capturados pelo script da extensão.

## Configuração

Um arquivo INI, editável à mão:

```
%APPDATA%\FirawSelector\config.ini
```

**Modo portátil**: se existir um `config.ini` na mesma pasta do `.exe`, é ele que vale — o programa roda de um pendrive sem tocar no perfil da máquina.

```ini
[geral]
modo=regras            ; regras | perguntar
padrao=chrome
tempo=5                ; escolhe sozinho depois de 5 s (0 desliga)
limparRastreio=1

[navegadores]
0001=chrome|Google Chrome|C:\...\chrome.exe|||chromium|0|0

[regras]
0001=gitlab.suaempresa.com.br/meu-grupo|msedge|contem|0|1
0002=*.suaempresa.com.br|msedge|host|0|1
0003=https://*/admin/*|firefox|url|1|1

[lembrados]
github.com=firefox
```

A numeração (`0001`, `0002`) é a ordem de avaliação: a primeira regra que casar vence.

Editando o arquivo à mão, o `|` separa os campos e por isso um `|` dentro de um valor vira `%7C`, e um `%` vira `%25`. Quem escreve pelo Studio não precisa saber disso — mas quem digita `%253A` direto no arquivo tem que escrever `%25253A`.

**Link do Teams / Outlook (Safe Links).** O que chega ao clicar num link dentro do Teams não é o seu link — é uma página da Microsoft com o endereço de verdade codificado dentro (`atp-safelinks.html?url=https%3A%2F%2F…`). Nenhuma regra sobre o seu site casaria com isso. O FirawSelector **desembrulha antes de decidir**, então uma regra `contém` com `gitlab.suaempresa.com.br` pega o link direto **e** o embrulhado, com codificação simples ou dupla. Em *Opções* há a chave para também **abrir** o endereço de verdade, pulando a página de proteção — desligada por padrão, porque ligá-la tira o link da verificação da Microsoft.

Só o `*` é curinga. O `?` é literal: todo endereço com consulta tem um, e tratá-lo como curinga mudaria o sentido do padrão colado.

A barra da raiz não atrapalha: `https://site.com` e `https://site.com/` são o mesmo lugar para as regras, então o padrão copiado da barra do navegador (que vem com a barra) casa com o link clicado (que às vezes vem sem).

**Qual tipo usar:** na dúvida, `contem`. Ele pega o link e tudo que vier depois — `gitlab.suaempresa.com.br/meu-grupo` vale para `/meu-grupo/projeto/-/merge_requests/12`. Já `url` compara **do começo ao fim**: sem um `*` no final, ele só casaria com aquele link exato. Regra antiga de `url` sem curinga é convertida para `contem` na primeira leitura, e o Studio avisa quando faz isso.

## Compilar

Não precisa de Visual Studio nem de SDK — o compilador C# já vem no Windows:

```
build.cmd
```

Gera `FirawSelector.exe`, `FirawSelector Studio.exe`, `FirawSelector Host.exe` e `FirawSelector Setup.exe`, e copia os quatro para `dist\`. O mesmo ícone (`firawselector.ico`) é regenerado por `tools\mkico.cs` em toda compilação e aplicado aos quatro executáveis.

Arquivos:

| Arquivo | O que é |
|---|---|
| `Core.cs` | Ambiente, paleta ciano e as duas tabelas de idioma |
| `Modelo.cs` | Navegador, Regra, INI, motor de decisão e registro no Windows |
| `Janela.cs` | Barra de título própria e os controles no tema |
| `FirawSelector.cs` | O motor e a janela de escolha |
| `FirawSelectorHost.cs` | Ponte Native Messaging entre as extensões e o motor |
| `FirawSelectorStudio.cs` | O configurador |
| `FirawSelectorSetup.cs` | O instalador (carrega os outros dois dentro) |
| `extensions/` | Fontes e manifestos das extensões Chrome, Edge e Firefox |

## Desinstalar

Configurações › Aplicativos › **FirawSelector** › Desinstalar. Ele tira o registro de navegador **antes** de apagar os arquivos — sem isso o Windows continuaria mandando links para um programa que não existe mais. As regras e o `config.ini` ficam em `%APPDATA%\FirawSelector`.

---

Firawynix · [firawselector.firawynix.com.br](https://firawselector.firawynix.com.br)

## Privacidade e assinatura

- [Política de privacidade](privacy.md)
- [Política de assinatura de código](code-signing-policy.md)

## Licença

O FirawSelector é software livre distribuído sob a [licença MIT](LICENSE).

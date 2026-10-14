# MSIX do FirawSelector

O empacotamento usa a identidade reservada no Partner Center:

- `Package/Identity/Name`: `Firawynix.FirawSelector`
- `Package/Identity/Publisher`: `CN=1FDE3668-C222-4506-AFE6-E2E425EAECD8`
- `PublisherDisplayName`: `Firawynix`
- Store ID: `9N8MV05X1NVP`

## Gerar o pacote da Microsoft Store

```powershell
.\tools\build-msix.ps1 -Channel Store
```

Esse pacote mantém a identidade oficial e fica sem assinatura local; a
Microsoft aplica a assinatura pública depois da certificação.

## Gerar o pacote de teste/sideload

```powershell
.\tools\build-msix.ps1 -Channel Lab
```

O pacote de laboratório usa identidade separada e é assinado com o certificado
`CN=Firawynix Laboratorio` instalado no perfil atual. Ele não substitui o pacote
da Store e não deve ser distribuído como assinatura pública. O parâmetro
`-LabCertificateThumbprint` permite escolher um certificado de teste existente;
o script não altera a confiança do Windows.

O pacote 1.1.14 declara `runFullTrust` e `unvirtualizedResources`. Os protocolos
HTTP/HTTPS/FTP, `microsoft-edge`, `firawselector` e os arquivos HTML são
associados pelo próprio manifesto. O usuário escolhe as associações padrão
nas configurações do Windows; o aplicativo não as altera automaticamente.

A ponte Native Messaging está incluída no MSIX e é opcional. No Windows 11,
abra **Opções → Conectar extensões** no Studio e confirme a integração por
usuário. As exclusões da virtualização se limitam às três chaves HKCU do
próprio host e a `%LOCALAPPDATA%\FirawSelectorStore`; não há desativação global.
O alias `FirawSelectorNativeHost.exe` oferece um caminho estável ao navegador.
O laboratório usa um alias separado. No Windows 10, use as associações padrão
ou o protocolo manual; a ponte nativa do MSIX requer Windows 11.

Para testes sem loja, carregue `dist\extensions\chrome` no Chromium/Opera.
Essa pasta mantém a chave do ID local autorizado. A pasta `chrome-store` e
os ZIPs de loja não têm chave: carregá-los sem compactação gera outro ID, não
autorizado pela ponte. As lojas atribuem seus IDs conhecidos na publicação.

Consulte `certification-notes-1.1.14.md` para o escopo da capacidade restrita
e as instruções de teste destinadas aos avaliadores da Microsoft.

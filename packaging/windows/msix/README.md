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
`CN=Firawynix Laboratorio` instalado no perfil atual. Ele não substitui nem
conflita com a versão da Store e não deve ser distribuído como assinatura
pública.

O pacote da Store declara somente `runFullTrust`. Os protocolos HTTP/HTTPS/FTP,
o protocolo `microsoft-edge` e os arquivos HTML são associados pelo próprio
manifesto, sem desabilitar a virtualização do Windows.

A ponte de Native Messaging para extensões do Chrome, Edge e Firefox não é
incluída no MSIX: esses navegadores precisam enxergar chaves HKCU globais, que
ficam isoladas no contêiner do pacote. Quem precisa capturar também os cliques
dentro do navegador deve usar o instalador clássico disponibilizado no site.

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

O manifesto desabilita a virtualização do registro e do AppData porque o
FirawSelector precisa registrar a ponte de Native Messaging e compartilhar a
configuração com as extensões e a instalação clássica. Por isso o pacote declara
as capacidades restritas `runFullTrust` e `unvirtualizedResources`.

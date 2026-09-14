# Linux port

The Linux application lives under `linux/`. It detects installed Firefox,
Chromium-family and Edge/Vivaldi browsers, applies first-match URL rules and can
register an XDG desktop handler for HTTP and HTTPS. Configuration is stored in
the Electron user-data directory as `config-linux.json` with user-only mode.

Run `npm test` and `npm run build:linux` from `linux/`. The stable Center URL is
`https://jogos.firawynix.com.br/api/games/firawselector/linux/arquivo`.

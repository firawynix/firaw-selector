(() => {
  "use strict";

  const HTTP = new Set(["http:", "https:"]);

  function linkDoEvento(evento) {
    for (const no of evento.composedPath()) {
      if (no instanceof Element) {
        const link = no.closest("a[href]");
        if (link) return link;
      }
    }
    return null;
  }

  function apenasAncoraLocal(destino) {
    const atual = new URL(location.href);
    return destino.origin === atual.origin &&
      destino.pathname === atual.pathname &&
      destino.search === atual.search &&
      destino.hash !== atual.hash;
  }

  function entrega(url) {
    chrome.runtime.sendMessage({ type: "firaw-open", url }, (resposta) => {
      const falha = chrome.runtime.lastError;
      if (!falha && resposta && resposta.ok) return;

      const detalhe = falha ? falha.message :
        (resposta && resposta.error ? resposta.error : "integração indisponível");
      const abrir = window.confirm(
        "O FirawSelector não conseguiu abrir a janela de escolha.\n\n" +
        detalhe + "\n\nAbrir este link normalmente?"
      );
      if (abrir) location.assign(url);
    });
  }

  function intercepta(evento) {
    if (!evento.isTrusted || evento.defaultPrevented) return;
    if (evento.type === "click" && evento.button !== 0) return;
    if (evento.type === "auxclick" && evento.button !== 1) return;

    const link = linkDoEvento(evento);
    if (!link) return;

    let destino;
    try { destino = new URL(link.href, document.baseURI); }
    catch (_) { return; }

    if (!HTTP.has(destino.protocol) || apenasAncoraLocal(destino)) return;

    evento.preventDefault();
    evento.stopImmediatePropagation();
    entrega(destino.href);
  }

  document.addEventListener("click", intercepta, true);
  document.addEventListener("auxclick", intercepta, true);
})();

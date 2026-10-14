(() => {
  "use strict";

  const HTTP = new Set(["http:", "https:"]);
  const chaveSite = "siteRoute:" + location.origin;
  let rota;
  chrome.storage.local.get(chaveSite, (dados) => {
    if (!chrome.runtime.lastError) rota = dados[chaveSite];
  });
  chrome.storage.onChanged.addListener((mudancas, area) => {
    if (area === "local" && mudancas[chaveSite]) rota = mudancas[chaveSite].newValue;
  });

  function protocolo(url) {
    return "firawselector://open/?url=" + encodeURIComponent(url);
  }

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
      const painel = document.createElement("div");
      painel.setAttribute("role", "dialog");
      painel.setAttribute("aria-label", "Abrir link com FirawSelector");
      painel.style.cssText = "position:fixed;z-index:2147483647;right:16px;bottom:16px;" +
        "max-width:340px;padding:16px;background:#101827;color:white;" +
        "font:14px Arial,sans-serif;border:1px solid #27c5d9;border-radius:10px;" +
        "box-shadow:0 8px 30px #0008";

      const aviso = document.createElement("p");
      aviso.textContent = "Como abrir os links deste site? Use a opção da Store se instalou o FirawSelector por ela.";
      aviso.title = detalhe;
      painel.appendChild(aviso);

      const lembrarLabel = document.createElement("label");
      const lembrar = document.createElement("input");
      lembrar.type = "checkbox";
      lembrarLabel.appendChild(lembrar);
      lembrarLabel.appendChild(document.createTextNode(" Lembrar para " + location.host));
      painel.appendChild(lembrarLabel);

      function memoriza(escolha) {
        if (lembrar.checked) {
          rota = escolha;
          chrome.storage.local.set({ [chaveSite]: escolha });
        }
        painel.remove();
      }

      const store = document.createElement("a");
      store.href = protocolo(url);
      store.textContent = "Abrir no FirawSelector da Store";
      store.style.cssText = "display:block;color:#27c5d9;margin:12px 0";
      store.addEventListener("click", (evento) => {
        if (evento.isTrusted) memoriza("store");
        else evento.preventDefault();
      });
      painel.appendChild(store);

      const normal = document.createElement("button");
      normal.textContent = "Abrir link normalmente";
      normal.style.cssText = "color:#101827;background:#eee;border:0;padding:8px;border-radius:4px";
      normal.addEventListener("click", (evento) => {
        if (!evento.isTrusted) return;
        memoriza("normal");
        location.assign(url);
      });
      painel.appendChild(normal);

      const fechar = document.createElement("button");
      fechar.textContent = "Fechar";
      fechar.style.marginLeft = "8px";
      fechar.style.color = "#101827";
      fechar.addEventListener("click", () => painel.remove());
      painel.appendChild(fechar);
      (document.body || document.documentElement).appendChild(painel);
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
    // Shift permite escolher de novo sem apagar a preferência salva.
    if (rota === "normal" && !evento.shiftKey) return;

    evento.preventDefault();
    evento.stopImmediatePropagation();
    if (rota === "store" && !evento.shiftKey) {
      // Mantém a ativação dentro do clique real, sem esperar a ponte nativa.
      location.assign(protocolo(destino.href));
      return;
    }
    entrega(destino.href);
  }

  document.addEventListener("click", intercepta, true);
  document.addEventListener("auxclick", intercepta, true);
})();

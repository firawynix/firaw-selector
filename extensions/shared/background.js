"use strict";

const HOST = "com.firawynix.firaw_selector";

chrome.runtime.onMessage.addListener((mensagem, remetente, responder) => {
  if (!mensagem || mensagem.type !== "firaw-open") return false;

  const pagina = remetente && remetente.url ? remetente.url : "";
  const permitido = /^https?:\/\//i.test(pagina) && /^https?:\/\//i.test(mensagem.url);
  if (!permitido) {
    responder({ ok: false, error: "Origem ou endereço não permitido." });
    return false;
  }

  chrome.runtime.sendNativeMessage(
    HOST,
    { action: "open", url: mensagem.url },
    (resposta) => {
      const falha = chrome.runtime.lastError;
      // Apenas o último resultado técnico: nunca guardar o endereço clicado.
      const versao = resposta && /^\d+\.\d+\.\d+(\.\d+)?$/.test(resposta.version)
        ? resposta.version : "";
      chrome.storage.local.set({ bridgeLastOpen: {
        at: Date.now(), version: versao,
        state: falha ? "browser-error" : resposta && resposta.ok ? "accepted" : "host-error"
      } }, () => { void chrome.runtime.lastError; });
      if (falha) {
        responder({ ok: false, error: falha.message });
        return;
      }
      responder(resposta || { ok: false, error: "O aplicativo não respondeu." });
    }
  );
  return true;
});

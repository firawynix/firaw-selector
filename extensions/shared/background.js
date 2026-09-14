"use strict";

const HOST = "com.firawynix.firaw_selector";

chrome.runtime.onMessage.addListener((mensagem, remetente, responder) => {
  if (!mensagem || mensagem.type !== "firaw-open") return false;

  const pagina = remetente && remetente.url ? remetente.url : "";
  if (!/^https?:\/\//i.test(pagina) || !/^https?:\/\//i.test(mensagem.url)) {
    responder({ ok: false, error: "Origem ou endereço não permitido." });
    return false;
  }

  chrome.runtime.sendNativeMessage(
    HOST,
    { action: "open", url: mensagem.url },
    (resposta) => {
      const falha = chrome.runtime.lastError;
      if (falha) {
        responder({ ok: false, error: falha.message });
        return;
      }
      responder(resposta || { ok: false, error: "O aplicativo não respondeu." });
    }
  );
  return true;
});

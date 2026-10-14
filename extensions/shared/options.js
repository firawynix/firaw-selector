"use strict";

const prefixo = "siteRoute:";
const sites = document.getElementById("sites");
const status = document.getElementById("status");
const ponte = document.getElementById("ponte");
const ultimaAbertura = document.getElementById("ultima-abertura");
document.getElementById("extensao-id").textContent = "ID desta extensão: " + chrome.runtime.id;

function mostraUltima(dado) {
  if (!dado) return;
  const estados = {
    accepted: "a ponte aceitou a abertura",
    "browser-error": "o navegador não conseguiu chamar a ponte",
    "host-error": "a ponte recusou a abertura"
  };
  const horario = new Date(dado.at).toLocaleTimeString("pt-BR", {
    hour: "2-digit", minute: "2-digit", second: "2-digit",
    hour12: false, fractionalSecondDigits: 3
  });
  ultimaAbertura.textContent = "Último clique às " + horario + ": " +
    (estados[dado.state] || "resultado desconhecido") +
    (dado.version ? " (host " + dado.version + ")" : "") + ".";
}

document.getElementById("testar-ponte").addEventListener("click", () => {
  ponte.textContent = "Testando...";
  chrome.runtime.sendNativeMessage("com.firawynix.firaw_selector", { action: "ping" }, (resposta) => {
    const falha = chrome.runtime.lastError;
    ponte.textContent = falha ? "Falha: " + falha.message :
      resposta && resposta.ok ? "Ponte respondeu: versão " + resposta.version + "." :
      "Falha: " + (resposta && resposta.error || "o host não respondeu");
  });
});
chrome.storage.onChanged.addListener((mudancas, area) => {
  if (area === "local" && mudancas.bridgeLastOpen)
    mostraUltima(mudancas.bridgeLastOpen.newValue);
});

function mostra() {
  chrome.storage.local.get(null, (dados) => {
    if (chrome.runtime.lastError) {
      status.textContent = "Não foi possível ler as preferências.";
      return;
    }
    sites.replaceChildren();
    mostraUltima(dados.bridgeLastOpen);
    const chaves = Object.keys(dados).filter((chave) => chave.startsWith(prefixo)).sort();
    if (!chaves.length) sites.textContent = "Nenhuma escolha lembrada ainda.";
    for (const chave of chaves) {
      const linha = document.createElement("div");
      linha.className = "site";
      const nome = document.createElement("strong");
      nome.textContent = chave.slice(prefixo.length);
      linha.appendChild(nome);
      const escolha = document.createElement("select");
      escolha.setAttribute("aria-label", "Como abrir links de " + nome.textContent);
      for (const [valor, texto] of [["store", "FirawSelector da Store"], ["normal", "Abrir normalmente"]]) {
        const opcao = document.createElement("option");
        opcao.value = valor;
        opcao.textContent = texto;
        escolha.appendChild(opcao);
      }
      escolha.value = dados[chave];
      escolha.addEventListener("change", () => {
        chrome.storage.local.set({ [chave]: escolha.value }, () => {
          status.textContent = chrome.runtime.lastError ? "Não foi possível salvar." : "Preferência salva.";
        });
      });
      linha.appendChild(escolha);
      const esquecer = document.createElement("button");
      esquecer.textContent = "Esquecer";
      esquecer.addEventListener("click", () => {
        chrome.storage.local.remove(chave, () => {
          status.textContent = chrome.runtime.lastError ? "Não foi possível remover." : "O site perguntará novamente.";
          mostra();
        });
      });
      linha.appendChild(esquecer);
      sites.appendChild(linha);
    }
  });
}

mostra();

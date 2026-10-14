"use strict";

const prefixo = "siteRoute:";
const sites = document.getElementById("sites");
const status = document.getElementById("status");

function mostra() {
  chrome.storage.local.get(null, (dados) => {
    if (chrome.runtime.lastError) {
      status.textContent = "Não foi possível ler as preferências.";
      return;
    }
    sites.replaceChildren();
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

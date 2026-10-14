const assert = require("node:assert/strict");
const fs = require("node:fs");
const vm = require("node:vm");

function carrega(resposta, erro) {
  const elementos = new Map();
  let chamada;
  function elemento(id) {
    if (!elementos.has(id)) elementos.set(id, {
      textContent: "", eventos: {}, replaceChildren() {},
      addEventListener(tipo, callback) { this.eventos[tipo] = callback; }
    });
    return elementos.get(id);
  }
  vm.runInNewContext(fs.readFileSync("extensions/shared/options.js", "utf8"), {
    Date,
    document: { getElementById: elemento },
    chrome: {
      storage: {
        local: { get: (_, callback) => callback({ bridgeLastOpen: {
          at: 1792000000775, version: "1.1.14", state: "accepted"
        } }) },
        onChanged: { addListener() {} }
      },
      runtime: {
        id: "ffjnechlggkdfgiooihhflkbbmlcblho", lastError: null,
        sendMessage() { throw new Error("O ping não deve usar a porta interna do background"); },
        sendNativeMessage(host, mensagem, callback) {
          chamada = { host, mensagem };
          this.lastError = erro;
          callback(resposta);
          this.lastError = null;
        }
      }
    }
  });
  elemento("testar-ponte").eventos.click();
  return { elemento, get chamada() { return chamada; } };
}

const sucesso = carrega({ ok: true, version: "1.1.14" }, null);
assert.equal(sucesso.chamada.host, "com.firawynix.firaw_selector");
assert.equal(sucesso.chamada.mensagem.action, "ping");
assert.equal(sucesso.chamada.mensagem.url, undefined, "Diagnóstico não abre links");
assert.match(sucesso.elemento("ponte").textContent, /Ponte respondeu: versão 1\.1\.14/);
assert.match(sucesso.elemento("extensao-id").textContent, /ffjnechlggkdfgiooihhflkbbmlcblho/);
assert.match(sucesso.elemento("ultima-abertura").textContent, /\d{2}:\d{2}:\d{2}/,
  "O horário deve incluir hora, minuto e segundo, não apenas milissegundos");
const falha = carrega(null, { message: "Access to the specified native messaging host is forbidden." });
assert.match(falha.elemento("ponte").textContent, /^Falha: Access to the specified/);
console.log("Diagnóstico: ping direto, erro nativo, ID e horário completo verificados.");

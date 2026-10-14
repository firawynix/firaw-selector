const assert = require("node:assert/strict");
const fs = require("node:fs");
const vm = require("node:vm");
let recebe;
let chamadasNativas = 0;
let ultimaMensagem;
let diagnostico;
let erroNativo = null;
let instante = 0;
vm.runInNewContext(fs.readFileSync("extensions/shared/background.js", "utf8"), {
  encodeURIComponent,
  Date: { now: () => ++instante },
  chrome: { storage: { local: { set: (dados, callback) => { diagnostico = dados; callback(); } } }, runtime: {
    onMessage: { addListener: (callback) => { recebe = callback; } },
    get lastError() { return erroNativo; },
    getURL: (path) => "chrome-extension://extensao-local/" + path,
    sendNativeMessage: (_, mensagem, callback) => {
      chamadasNativas++;
      ultimaMensagem = mensagem;
      callback({ ok: !erroNativo, version: "1.1.14" });
    }
  } }
});
function solicita(type, url, origem = "https://web.whatsapp.com/") {
  let resposta;
  recebe({ type, url }, { url: origem }, (valor) => { resposta = valor; });
  return resposta;
}
const destino = "https://example.com/?a=1&b=2";
assert.equal(solicita("firaw-open", destino).ok, true);
assert.equal(chamadasNativas, 1);
assert.equal(diagnostico.bridgeLastOpen.at, 1);
assert.equal(diagnostico.bridgeLastOpen.state, "accepted");
assert.equal(JSON.stringify(diagnostico).includes(destino), false, "Diagnóstico não guarda URL");
assert.equal(solicita("firaw-open", destino).ok, true);
assert.equal(chamadasNativas, 2, "Cada clique inicia uma solicitação nativa independente");
assert.equal(diagnostico.bridgeLastOpen.at, 2, "Segundo clique atualiza resultado observável");
assert.equal(solicita("firaw-open", "javascript:alert(1)").ok, false);
assert.equal(solicita("firaw-open", destino, "file:///pagina.html").ok, false);
assert.equal(solicita("firaw-ping", ""), undefined, "Diagnóstico não passa pelo canal de abertura");
assert.equal(chamadasNativas, 2);
assert.equal(ultimaMensagem.action, "open");
erroNativo = { message: "Native host not found" };
assert.equal(solicita("firaw-open", destino).ok, false);
assert.equal(diagnostico.bridgeLastOpen.state, "browser-error");
console.log("Background: cliques repetidos pela ponte nativa e rejeição de origem/destino inválidos verificados.");

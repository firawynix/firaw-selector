const assert = require("node:assert/strict");
const fs = require("node:fs");
const vm = require("node:vm");
let recebe;
let chamadasNativas = 0;
vm.runInNewContext(fs.readFileSync("extensions/shared/background.js", "utf8"), {
  encodeURIComponent,
  chrome: { runtime: {
    onMessage: { addListener: (callback) => { recebe = callback; } },
    lastError: null,
    sendNativeMessage: (_, mensagem, callback) => {
      chamadasNativas++;
      callback({ ok: true });
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
assert.equal(solicita("firaw-protocol", destino).protocol,
  "firawselector://open/?url=" + encodeURIComponent(destino));
assert.equal(chamadasNativas, 1, "Store não depende do host clássico");
assert.equal(solicita("firaw-protocol", "javascript:alert(1)").ok, false);
assert.equal(solicita("firaw-protocol", destino, "file:///pagina.html").ok, false);
console.log("Background: ponte clássica, protocolo Store e rejeição de origem/destino inválidos verificados.");

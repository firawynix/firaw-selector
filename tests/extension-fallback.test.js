const assert = require("node:assert/strict");
const fs = require("node:fs");
const vm = require("node:vm");

const script = fs.readFileSync("extensions/shared/content.js", "utf8");

function simulaClique(resposta, preferencias = {}) {
  let respostaAtual = resposta;
  let aoClicar;
  let mudou;
  let chamadas = 0;
  let navegou;
  let preveniu;
  const gravados = {};
  const elementos = [];
  class Elemento {
    constructor(tag) {
      this.tag = tag;
      this.style = {};
      this.children = [];
      this.eventos = {};
    }
    closest() { return this.link || null; }
    setAttribute() {}
    addEventListener(tipo, callback) { this.eventos[tipo] = callback; }
    appendChild(child) { this.children.push(child); }
    remove() {}
  }
  const corpo = { appendChild: (elemento) => elementos.push(elemento) };
  const documento = {
    body: corpo,
    baseURI: "https://origem.example/",
    createElement: (tag) => new Elemento(tag),
    createTextNode: (texto) => ({ texto }),
    addEventListener: (tipo, callback) => {
      if (tipo === "click") aoClicar = callback;
    }
  };
  const chrome = {
    runtime: {
      lastError: null,
      sendMessage: (mensagem, callback) => {
        chamadas++;
        assert.equal(mensagem.type, "firaw-open");
        callback(respostaAtual);
      }
    },
    storage: {
      local: {
        get: (_, callback) => callback(preferencias),
        set: (dados) => Object.assign(gravados, dados)
      },
      onChanged: { addListener: (callback) => { mudou = callback; } }
    }
  };
  vm.runInNewContext(script, {
    chrome, document: documento, Element: Elemento, URL,
    location: {
      href: "https://origem.example/", origin: "https://origem.example", host: "origem.example",
      assign: (url) => { navegou = url; }
    }, encodeURIComponent
  });
  const origem = new Elemento("a");
  const atributos = { href: "https://example.com/a?x=1&y=2", target: "_blank" };
  origem.link = {
    get href() { return atributos.href; },
    getAttribute: (nome) => atributos[nome] ?? null,
    setAttribute: (nome, valor) => { atributos[nome] = valor; },
    removeAttribute: (nome) => { delete atributos[nome]; }
  };
  function clica(extra = {}) {
    preveniu = false;
    aoClicar({
    isTrusted: true, defaultPrevented: false, type: "click", button: 0,
    composedPath: () => [origem],
    preventDefault() { preveniu = true; }, stopImmediatePropagation() {}, ...extra
  }); }
  clica();
  return { elementos, gravados, clica, mudou,
    conecta() { respostaAtual = { ok: true }; },
    get navegou() { return navegou; }, get chamadas() { return chamadas; },
    get href() { return atributos.href; }, get target() { return atributos.target; },
    get preveniu() { return preveniu; } };
}

assert.equal(simulaClique({ ok: true }).elementos.length, 0);
const teste = simulaClique({ ok: false, error: "Host ausente" });
const dialogos = teste.elementos;
assert.equal(dialogos.length, 1);
const linkStore = dialogos[0].children.find((filho) => filho.tag === "a");
assert.equal(linkStore.href,
  "firawselector://open/?url=https%3A%2F%2Fexample.com%2Fa%3Fx%3D1%26y%3D2");
const checkbox = dialogos[0].children.find((filho) => filho.tag === "label").children[0];
checkbox.checked = true;
linkStore.eventos.click({ isTrusted: false, preventDefault() {} });
assert.equal(Object.keys(teste.gravados).length, 0, "Clique sintético não salva escolha");
linkStore.eventos.click({ isTrusted: true });
assert.equal(teste.gravados["siteRoute:https://origem.example"], "store");
teste.conecta();
teste.clica();
assert.equal(teste.chamadas, 2, "Site lembrado encaminha cada clique pelo background");
assert.equal(teste.navegou, undefined, "Ponte nativa não depende de navegação externa do browser");
assert.equal(teste.elementos.length, 1, "Ponte conectada não repete a janela extra");
assert.equal(teste.preveniu, true);
assert.equal(teste.href, "https://example.com/a?x=1&y=2");
assert.equal(teste.target, "_blank", "Os atributos da página não são alterados");
teste.clica({ shiftKey: true });
assert.equal(teste.chamadas, 3, "Shift permite rever a escolha");

const restaurado = simulaClique({ ok: true }, { "siteRoute:https://origem.example": "store" });
assert.equal(restaurado.chamadas, 1);
assert.equal(restaurado.navegou, undefined);
assert.equal(restaurado.elementos.length, 0);
const ponteAusente = simulaClique({ ok: false, error: "Host ausente" },
  { "siteRoute:https://origem.example": "store" });
const avisoSalvo = ponteAusente.elementos[0];
assert.equal(avisoSalvo.children.find((filho) => filho.tag === "label").children[0].checked,
  true, "Falha da ponte não desmarca uma preferência já salva");
assert.match(avisoSalvo.children.find((filho) => filho.tag === "p").textContent,
  /escolha.*salva/i, "O aviso distingue falha de integração de escolha esquecida");
restaurado.clica({ isTrusted: false });
assert.equal(restaurado.chamadas, 1, "Clique sintético não abre aplicativo externo");
restaurado.clica();
assert.equal(restaurado.chamadas, 2, "Cliques repetidos passam pelo background individualmente");
const normal = simulaClique(null, { "siteRoute:https://origem.example": "normal" });
assert.equal(normal.chamadas, 0);
assert.equal(normal.navegou, undefined, "Abrir normalmente deixa o evento original seguir");
normal.mudou({ "siteRoute:https://origem.example": { newValue: undefined } }, "local");
normal.clica();
assert.equal(normal.chamadas, 1, "Esquecer no painel afeta abas já abertas");
const outroSite = simulaClique({ ok: true }, { "siteRoute:https://outro.example": "store" });
assert.equal(outroSite.chamadas, 1, "Preferência não vaza para outro site");
console.log("Extensão: ponte nativa, Store, preferências por origem, Shift e redefinição verificados.");

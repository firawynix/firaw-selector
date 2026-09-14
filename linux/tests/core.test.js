const test=require('node:test');const assert=require('node:assert/strict');const {validUrl,ruleMatches,decide,shellQuote}=require('../core');
test('aceita apenas http e https',()=>{assert.ok(validUrl('https://firawynix.com.br'));assert.equal(validUrl('file:///etc/passwd'),null)});
test('regras seguem contains, domínio, curinga e regex',()=>{assert.ok(ruleMatches({type:'contains',pattern:'docs/'},'https://a.test/docs/1'));assert.ok(ruleMatches({type:'domain',pattern:'*.google.com'},'https://mail.google.com/x'));assert.ok(ruleMatches({type:'exact',pattern:'https://*/docs/*'},'https://a.test/docs/1'));assert.ok(ruleMatches({type:'regex',pattern:'^https://a\\.'},'https://a.test'))});
test('a primeira regra vence',()=>{assert.equal(decide({rules:[{type:'contains',pattern:'x',browserId:'firefox'},{type:'contains',pattern:'x',browserId:'chrome'}]},'https://x.test'),'firefox')});
test('escapa caminho em desktop entry',()=>{assert.equal(shellQuote("/tmp/a'b"),"'/tmp/a'\\''b'")});

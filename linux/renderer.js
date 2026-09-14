const api=window.selector; let state={browsers:[],config:{rules:[]},pendingUrl:null}; const $=id=>document.getElementById(id);
function status(text,bad=false){$('status').textContent=text;$('status').classList.toggle('bad',bad)}
function browserButton(browser,choose=false){const b=document.createElement('button');b.className='browser';const icon=document.createElement('b');icon.textContent=browser.name.slice(0,2).toUpperCase();const name=document.createElement('strong');name.textContent=browser.name;const cmd=document.createElement('small');cmd.textContent=browser.path;b.append(icon,name,cmd);if(choose)b.onclick=()=>api.open(browser.id,$('private').checked).catch(error=>status(error.message,true));return b}
function render(){
  $('chooser').hidden=!state.pendingUrl;$('url').textContent=state.pendingUrl||'';
  $('browserChoices').replaceChildren(...state.browsers.map(x=>browserButton(x,true)));
  $('detected').replaceChildren(...state.browsers.map(x=>browserButton(x,false)));
  $('ruleBrowser').replaceChildren(...state.browsers.map(x=>{const o=document.createElement('option');o.value=x.id;o.textContent=x.name;return o}));
  $('rules').replaceChildren(...state.config.rules.map((rule,index)=>{const row=document.createElement('div');row.className='rule';const type=document.createElement('span');type.textContent=({contains:'CONTÉM',domain:'DOMÍNIO',exact:'ENDEREÇO',regex:'REGEX'})[rule.type]||'CONTÉM';const pattern=document.createElement('code');pattern.textContent=rule.pattern;const browser=document.createElement('strong');browser.textContent=state.browsers.find(x=>x.id===rule.browserId)?.name||rule.browserId;const del=document.createElement('button');del.textContent='Remover';del.onclick=async()=>{state.config.rules.splice(index,1);await persist()};row.append(type,pattern,browser,del);return row}));
}
async function load(){try{state=await api.state();render();status(`${state.config.rules.length} regra(s) · ${state.browsers.length} navegador(es) detectado(s)`)}catch(e){status(e.message,true)}}
async function persist(){try{state.config=await api.save(state.config);render();status('Regras salvas.')}catch(e){status(e.message,true)}}
$('register').onclick=async()=>{try{await api.register();status('FirawSelector definido para links HTTP e HTTPS.')}catch(e){status(`Não foi possível registrar: ${e.message}`,true)}};
$('add').onclick=()=>{$('pattern').value='';$('ruleDialog').showModal()};
$('saveRule').onclick=async event=>{event.preventDefault();const pattern=$('pattern').value.trim();if(!pattern)return;state.config.rules.push({type:$('ruleType').value,pattern,browserId:$('ruleBrowser').value});$('ruleDialog').close();await persist()};
api.onRefresh(load);load();

const { app, BrowserWindow, ipcMain } = require('electron');
const { execFile, spawn } = require('node:child_process');
const fs = require('node:fs/promises');
const os = require('node:os');
const path = require('node:path');
const { promisify } = require('node:util');
const { decide, shellQuote, validUrl } = require('./core');

const exec = promisify(execFile);
app.setDesktopName('br.com.firawynix.firawselector.desktop');
let mainWindow = null;
let pendingUrl = null;

const candidates = [
  { id: 'firefox', name: 'Firefox', command: 'firefox', privateArgs: ['--private-window'] },
  { id: 'chrome', name: 'Google Chrome', command: 'google-chrome', privateArgs: ['--incognito'] },
  { id: 'chromium', name: 'Chromium', command: 'chromium', privateArgs: ['--incognito'] },
  { id: 'brave', name: 'Brave', command: 'brave-browser', privateArgs: ['--incognito'] },
  { id: 'edge', name: 'Microsoft Edge', command: 'microsoft-edge', privateArgs: ['--inprivate'] },
  { id: 'vivaldi', name: 'Vivaldi', command: 'vivaldi', privateArgs: ['--incognito'] },
];

function configPath() { return path.join(app.getPath('userData'), 'config-linux.json'); }
async function loadConfig() {
  try { return { rules: [], defaultBrowser: null, ...(JSON.parse(await fs.readFile(configPath(), 'utf8'))) }; }
  catch { return { rules: [], defaultBrowser: null }; }
}
async function saveConfig(config) {
  await fs.mkdir(path.dirname(configPath()), { recursive: true });
  await fs.writeFile(configPath(), `${JSON.stringify(config, null, 2)}\n`, { mode: 0o600 });
  return config;
}
async function browsers() {
  const found = [];
  for (const item of candidates) {
    try { const { stdout } = await exec('which', [item.command]); found.push({ ...item, path: stdout.trim() }); } catch { /* ausente */ }
  }
  return found;
}
function openBrowser(browser, url, privateMode = false) {
  if (!browser || !validUrl(url)) throw new Error('Navegador ou endereço inválido.');
  const child = spawn(browser.path, [...(privateMode ? browser.privateArgs : []), url], { detached: true, stdio: 'ignore' });
  child.unref();
}
function urlFromArgs(args) { return args.find((arg) => validUrl(arg)) || null; }

async function route(url, forceAsk = false) {
  const available = await browsers(); const config = await loadConfig();
  const browserId = forceAsk ? null : decide(config, url);
  const selected = available.find((item) => item.id === browserId);
  if (selected) { openBrowser(selected, url); if (mainWindow) mainWindow.hide(); return; }
  pendingUrl = url; showWindow();
}

function showWindow() {
  if (!mainWindow) {
    mainWindow = new BrowserWindow({
      width: 940, height: 660, minWidth: 720, minHeight: 500, title: 'FirawSelector',
      backgroundColor: '#071116', autoHideMenuBar: true,
      webPreferences: { preload: path.join(__dirname, 'preload.js'), contextIsolation: true, sandbox: true },
    });
    mainWindow.loadFile('index.html'); mainWindow.on('closed', () => { mainWindow = null; });
  } else { mainWindow.show(); mainWindow.focus(); mainWindow.webContents.send('selector:refresh'); }
}

async function registerHandler() {
  const executable = process.env.APPIMAGE || process.execPath;
  const desktopDir = path.join(os.homedir(), '.local', 'share', 'applications');
  const desktop = path.join(desktopDir, 'br.com.firawynix.firawselector.desktop');
  const content = `[Desktop Entry]\nType=Application\nName=FirawSelector\nComment=Escolha o navegador certo para cada link\nExec=${shellQuote(executable)} %u\nTerminal=false\nNoDisplay=true\nCategories=Network;WebBrowser;\nMimeType=x-scheme-handler/http;x-scheme-handler/https;text/html;\nStartupWMClass=br.com.firawynix.firawselector\n`;
  await fs.mkdir(desktopDir, { recursive: true }); await fs.writeFile(desktop, content, { mode: 0o755 });
  await exec('update-desktop-database', [desktopDir]).catch(() => {});
  await exec('xdg-mime', ['default', path.basename(desktop), 'x-scheme-handler/http']);
  await exec('xdg-mime', ['default', path.basename(desktop), 'x-scheme-handler/https']);
  return desktop;
}

ipcMain.handle('selector:state', async () => ({ browsers: await browsers(), config: await loadConfig(), pendingUrl }));
ipcMain.handle('selector:save', (_event, config) => saveConfig({ rules: Array.isArray(config.rules) ? config.rules.slice(0, 200) : [], defaultBrowser: config.defaultBrowser || null }));
ipcMain.handle('selector:open', async (_event, browserId, privateMode) => {
  const browser = (await browsers()).find((item) => item.id === browserId); const url = pendingUrl;
  if (!url) throw new Error('Nenhum link aguardando escolha.'); openBrowser(browser, url, Boolean(privateMode)); pendingUrl = null; mainWindow?.hide(); return true;
});
ipcMain.handle('selector:register', () => registerHandler());

const lock = app.requestSingleInstanceLock();
if (!lock) app.quit();
else {
  app.on('second-instance', (_event, argv) => { const url = urlFromArgs(argv); if (url) route(url, argv.includes('--ask')); else showWindow(); });
  app.whenReady().then(() => { const url = urlFromArgs(process.argv); if (url) route(url, process.argv.includes('--ask')); else showWindow(); });
}
app.on('window-all-closed', () => { if (process.platform !== 'darwin') app.quit(); });

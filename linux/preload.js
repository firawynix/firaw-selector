const { contextBridge, ipcRenderer } = require('electron');
contextBridge.exposeInMainWorld('selector', {
  state: () => ipcRenderer.invoke('selector:state'),
  save: (config) => ipcRenderer.invoke('selector:save', config),
  open: (browserId, privateMode) => ipcRenderer.invoke('selector:open', browserId, privateMode),
  register: () => ipcRenderer.invoke('selector:register'),
  onRefresh: (callback) => ipcRenderer.on('selector:refresh', callback),
});

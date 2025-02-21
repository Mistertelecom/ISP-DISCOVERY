const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('electronAPI', {
  startScan: (interfaceName) => ipcRenderer.invoke('start-scan', interfaceName),
  stopScan: () => ipcRenderer.invoke('stop-scan'),
  getInterfaces: () => ipcRenderer.invoke('get-interfaces'),
  onDeviceDiscovered: (callback) => {
    ipcRenderer.on('device-discovered', (event, device) => callback(device));
    return () => {
      ipcRenderer.removeAllListeners('device-discovered');
    };
  },
  onPacketCaptured: (callback) => {
    ipcRenderer.on('packet-captured', (event, packet) => callback(packet));
    return () => {
      ipcRenderer.removeAllListeners('packet-captured');
    };
  }
});

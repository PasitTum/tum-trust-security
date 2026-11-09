const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('tumKioskHalApi', {
  getHealth: () => ipcRenderer.invoke('hal:get-health'),
  getReaders: () => ipcRenderer.invoke('hal:get-readers'),
  transmitApdu: (readerName, apduHex) => ipcRenderer.invoke('hal:transmit-apdu', { readerName, apduHex }),
  readThaiId: (readerName) => ipcRenderer.invoke('hal:read-thai-id', { readerName }),
  inspectPassport: () => ipcRenderer.invoke('hal:inspect-passport'),
  checkLiveness: (imageBase64, faceAnalysis) => ipcRenderer.invoke('hal:check-liveness', { imageBase64, faceAnalysis }),
  getFaceCascade: () => ipcRenderer.invoke('hal:get-face-cascade'),
  runHilBenchmark: () => ipcRenderer.invoke('hal:run-hil-benchmark'),
  simulateDisconnect: (deviceName) => ipcRenderer.invoke('hal:simulate-disconnect', { deviceName }),
  simulateReconnect: (deviceName) => ipcRenderer.invoke('hal:simulate-reconnect', { deviceName }),
  exportReport: (reportData) => ipcRenderer.invoke('hal:export-report', reportData),
  toggleKioskMode: () => ipcRenderer.invoke('app:toggle-kiosk-mode'),

  onTriggerRefresh: (callback) => {
    ipcRenderer.on('hal:trigger-refresh', () => callback());
  },
  onTriggerExport: (callback) => {
    ipcRenderer.on('hal:trigger-export', () => callback());
  }
});

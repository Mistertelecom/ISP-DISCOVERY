// DOM Elements
const interfaceSelect = document.getElementById('interfaceSelect');
const scanButton = document.getElementById('scanButton');
const themeToggle = document.getElementById('themeToggle');
const settingsButton = document.getElementById('settingsButton');
const deviceTableBody = document.getElementById('deviceTableBody');
const logOutput = document.getElementById('logOutput');
const connectionStatus = document.getElementById('connectionStatus');

// Brand-specific colors and icons
const brandConfig = {
    'Ubiquiti': {
        icon: 'wifi',
        rowClass: 'table-row-ubiquiti'
    },
    'MikroTik': {
        icon: 'router',
        rowClass: 'table-row-mikrotik'
    },
    'Mimosa': {
        icon: 'settings_input_antenna',
        rowClass: 'table-row-mimosa'
    }
};

// Settings Modal Elements
let settingsModal = null;
let autoScrollEnabled = true;
let isScanning = false;

// Backend API Configuration
const API_BASE_URL = 'http://localhost:35123';
const WS_URL = 'ws://localhost:35123/ws';
let ws = null;
let reconnectAttempts = 0;
const MAX_RECONNECT_ATTEMPTS = 5;

// Theme Management
function initializeTheme() {
    const savedTheme = localStorage.getItem('theme') || 
                      (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');
    updateTheme(savedTheme);
}

function updateTheme(theme) {
    document.body.classList.toggle('dark', theme === 'dark');
    themeToggle.querySelector('.material-icons').textContent = 
        theme === 'dark' ? 'light_mode' : 'dark_mode';
    localStorage.setItem('theme', theme);
}

function toggleTheme() {
    const currentTheme = localStorage.getItem('theme') || 
                        (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');
    const newTheme = currentTheme === 'dark' ? 'light' : 'dark';
    updateTheme(newTheme);
}

// WebSocket Management
function updateConnectionStatus(status, type = 'info') {
    connectionStatus.textContent = status;
    connectionStatus.className = `text-sm ${
        type === 'success' ? 'text-green-500 dark:text-green-400' :
        type === 'error' ? 'text-red-500 dark:text-red-400' :
        'text-gray-500 dark:text-gray-400'
    }`;
}

function connectWebSocket() {
    if (ws && ws.readyState === WebSocket.OPEN) return;

    ws = new WebSocket(WS_URL);
    
    ws.onopen = () => {
        appendToLog('Connected to server', 'success');
        updateConnectionStatus('Connected', 'success');
        reconnectAttempts = 0;
    };

    ws.onclose = () => {
        appendToLog('Disconnected from server', 'error');
        updateConnectionStatus('Disconnected', 'error');
        if (reconnectAttempts < MAX_RECONNECT_ATTEMPTS) {
            reconnectAttempts++;
            updateConnectionStatus(`Reconnecting (${reconnectAttempts}/${MAX_RECONNECT_ATTEMPTS})...`);
            setTimeout(connectWebSocket, 5000);
        }
    };

    ws.onmessage = (event) => {
        try {
            const data = JSON.parse(event.data);
            handleWebSocketMessage(data);
        } catch (error) {
            console.error('Error parsing WebSocket message:', error);
        }
    };

    ws.onerror = (error) => {
        appendToLog('WebSocket error: ' + error.message, 'error');
        updateConnectionStatus('Connection Error', 'error');
    };
}

function handleWebSocketMessage(data) {
    switch (data.type) {
        case 'device':
            const device = data.data;
            if (device.brand && brandConfig[device.brand]) {
                addDeviceToTable(device);
                appendToLog(`Found ${device.brand} ${device.model || 'device'} at ${device.ipAddress}`, 'success');
            }
            break;
        case 'packet':
            appendToLog(`Packet captured from: ${data.data.sourceIP}`, 'info');
            break;
        case 'status':
            handleStatusUpdate(data.data);
            break;
    }
}

// Network Interface Management
async function loadInterfaces() {
    try {
        const response = await fetch(`${API_BASE_URL}/interfaces`);
        if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
        const data = await response.json();
        
        interfaceSelect.innerHTML = '<option value="">Select Interface</option>';
        data.forEach(iface => {
            const option = document.createElement('option');
            option.value = iface.name;
            option.textContent = iface.description;
            interfaceSelect.appendChild(option);
        });
    } catch (error) {
        appendToLog('Error loading interfaces: ' + error.message, 'error');
    }
}

// Scanning Management
async function toggleScan() {
    try {
        if (!isScanning) {
            const selectedInterface = interfaceSelect.value;
            if (!selectedInterface) {
                appendToLog('Please select a network interface', 'error');
                return;
            }

            const response = await fetch(
                `${API_BASE_URL}/scan/start?interfaceName=${encodeURIComponent(selectedInterface)}`,
                { method: 'POST' }
            );

            if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
            isScanning = true;
            updateScanButton(true);
            appendToLog('Scanning started', 'success');
        } else {
            const response = await fetch(`${API_BASE_URL}/scan/stop`, { method: 'POST' });
            if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
            isScanning = false;
            updateScanButton(false);
            appendToLog('Scanning stopped', 'success');
        }
    } catch (error) {
        appendToLog(`Error ${isScanning ? 'stopping' : 'starting'} scan: ${error.message}`, 'error');
    }
}

function updateScanButton(scanning) {
    const icon = scanButton.querySelector('.material-icons');
    const text = scanButton.querySelector('span:not(.material-icons)');
    
    if (scanning) {
        scanButton.classList.remove('btn-primary');
        scanButton.classList.add('btn-danger');
        icon.textContent = 'stop';
        text.textContent = 'Stop Scan';
    } else {
        scanButton.classList.remove('btn-danger');
        scanButton.classList.add('btn-primary');
        icon.textContent = 'wifi_tethering';
        text.textContent = 'Start Scan';
    }
    
    interfaceSelect.disabled = scanning;
}

// Device Table Management
function addDeviceToTable(device) {
    const existingRow = Array.from(deviceTableBody.getElementsByTagName('tr'))
        .find(row => row.cells[2].textContent === device.macAddress);

    const row = existingRow || deviceTableBody.insertRow();
    const brandInfo = brandConfig[device.brand];
    row.className = `table-row ${brandInfo.rowClass}`;
    
    row.innerHTML = `
        <td class="table-cell">
            <div class="flex items-center space-x-2">
                <span class="material-icons text-lg">${brandInfo.icon}</span>
                <span class="font-medium">${device.brand}</span>
            </div>
        </td>
        <td class="table-cell font-mono">${device.ipAddress}</td>
        <td class="table-cell font-mono">${device.macAddress}</td>
        <td class="table-cell">${device.name || '-'}</td>
        <td class="table-cell">${device.discoveryMethod}</td>
        <td class="table-cell">${device.model || '-'}</td>
    `;

    if (!existingRow) {
        row.style.animation = 'fadeIn 0.5s';
    }
}

// Logging with Auto-scroll
function scrollLogToBottom() {
    if (autoScrollEnabled) {
        requestAnimationFrame(() => {
            const lastEntry = logOutput.lastElementChild;
            if (lastEntry) {
                lastEntry.scrollIntoView({ behavior: 'smooth' });
            }
        });
    }
}

function appendToLog(message, type = 'info') {
    const timestamp = new Date().toLocaleTimeString('en-US', { 
        hour12: false,
        hour: '2-digit',
        minute: '2-digit',
        second: '2-digit',
        fractionalSecondDigits: 3
    });

    const logEntry = document.createElement('div');
    logEntry.className = `log-entry log-entry-${type} opacity-0`;
    logEntry.innerHTML = `
        <span class="text-gray-500 dark:text-gray-400">[${timestamp}]</span>
        ${message}
    `;
    
    logOutput.appendChild(logEntry);

    // Fade in animation
    requestAnimationFrame(() => {
        logEntry.classList.add('opacity-100');
        logEntry.style.transition = 'opacity 0.2s ease-in-out';
    });

    scrollLogToBottom();

    // Keep only the last 1000 log entries
    while (logOutput.children.length > 1000) {
        logOutput.removeChild(logOutput.firstChild);
    }
}

// Initialize settings from localStorage
function initializeSettings() {
    autoScrollEnabled = localStorage.getItem('autoScrollEnabled') !== 'false';
}

// Event Listeners
scanButton.addEventListener('click', toggleScan);
themeToggle.addEventListener('click', toggleTheme);
settingsButton.addEventListener('click', () => {
    appendToLog('Settings button clicked', 'info');
});

// Initialize
document.addEventListener('DOMContentLoaded', () => {
    initializeTheme();
    initializeSettings();
    loadInterfaces();
    connectWebSocket();
    appendToLog('Application started', 'info');
});

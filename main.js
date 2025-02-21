const { app, BrowserWindow, ipcMain } = require('electron');
const path = require('path');
const { spawn } = require('child_process');
let backendProcess = null;
let mainWindow = null;

function startBackendService() {
    console.log('Starting backend service...');
    
    // Start the .NET backend service
    backendProcess = spawn('dotnet', ['run', '--project', 'NetworkDiscovery.csproj'], {
        cwd: process.cwd(),
    });

    backendProcess.stdout.on('data', (data) => {
        console.log(`Backend output: ${data}`);
        if (data.includes('Now listening on: http://localhost:35123')) {
            console.log('Backend service is ready');
            createWindow();
        }
    });

    backendProcess.stderr.on('data', (data) => {
        console.error(`Backend error: ${data}`);
    });

    backendProcess.on('close', (code) => {
        console.log(`Backend process exited with code ${code}`);
        backendProcess = null;
    });

    // Handle backend process errors
    backendProcess.on('error', (err) => {
        console.error('Failed to start backend process:', err);
    });
}

function createWindow() {
    // Create the browser window
    mainWindow = new BrowserWindow({
        width: 1200,
        height: 800,
        minWidth: 1000,
        minHeight: 600,
        webPreferences: {
            nodeIntegration: true,
            contextIsolation: false,
            webSecurity: false // Required for local development
        },
        icon: path.join(__dirname, 'network.ico')
    });

    // Load the index.html file
    mainWindow.loadFile('index.html');

    // Open DevTools in development mode
    if (process.argv.includes('--debug')) {
        mainWindow.webContegits.openDevTools();
    }

    // Handle window closed event
    mainWindow.on('closed', () => {
        mainWindow = null;
    });
}

// This method will be called when Electron has finished initialization
app.whenReady().then(() => {
    startBackendService();
});

// Quit when all windows are closed
app.on('window-all-closed', () => {
    if (process.platform !== 'darwin') {
        if (backendProcess) {
            backendProcess.kill();
        }
        app.quit();
    }
});

app.on('activate', () => {
    if (mainWindow === null && backendProcess) {
        createWindow();
    }
});

// Handle app quit
app.on('before-quit', () => {
    if (backendProcess) {
        backendProcess.kill();
    }
});

// Error handling
process.on('uncaughtException', (error) => {
    console.error('Uncaught Exception:', error);
});

process.on('unhandledRejection', (reason, promise) => {
    console.error('Unhandled Rejection at:', promise, 'reason:', reason);
});

// Clean up on exit
process.on('SIGINT', () => {
    if (backendProcess) {
        backendProcess.kill();
    }
    app.quit();
});

process.on('SIGTERM', () => {
    if (backendProcess) {
        backendProcess.kill();
    }
    app.quit();
});

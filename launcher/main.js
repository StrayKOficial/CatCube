const { app, BrowserWindow, ipcMain } = require('electron');
const path = require('path');
const { spawn } = require('child_process');

function createWindow() {
    const win = new BrowserWindow({
        width: 1100,
        height: 700,
        frame: false,
        backgroundColor: '#1a1b26',
        webPreferences: {
            nodeIntegration: true,
            contextIsolation: false
        }
    });

    win.loadFile('index.html');
}

app.whenReady().then(() => {
    createWindow();

    app.on('activate', () => {
        if (BrowserWindow.getAllWindows().length === 0) createWindow();
    });
});

app.on('window-all-closed', () => {
    if (process.platform !== 'darwin') app.quit();
});

ipcMain.on('launch-game', (event, serverIp) => {
    console.log(`Launching CatCube for server: ${serverIp}`);
    const gamePath = path.resolve(__dirname, '../build/CatCube');
    const args = ['--client', serverIp];

    const child = spawn(gamePath, args, {
        cwd: path.dirname(gamePath),
        detached: true,
        stdio: 'ignore'
    });

    child.unref();
});

// IPC handler to get online servers from C# backend
ipcMain.on('get-servers', (event) => {
    const discoveryPath = path.resolve(__dirname, 'Discovery.exe');
    const child = spawn('mono', [discoveryPath]);

    let output = '';
    child.stdout.on('data', (data) => {
        output += data.toString();
    });

    child.on('close', () => {
        try {
            const servers = JSON.parse(output);
            event.reply('servers-data', servers);
        } catch (e) {
            console.error("Failed to parse C# output", e);
            event.reply('servers-data', []);
        }
    });
});

ipcMain.on('close-app', () => {
    app.quit();
});

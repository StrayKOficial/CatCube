const { exec } = require('child_process');

async function getOnlineServers() {
    return new Promise((resolve) => {
        // Find processes listening on port 53640 (CatCube default)
        exec('ss -ulpn | grep 53640', (err, stdout) => {
            const servers = [];
            
            // Always add a "Local Test" if it exists or for debug
            // In a real production app, this would query a web API.
            
            if (stdout.includes('CatCube')) {
                servers.push({
                    name: "Local Multiplayer Server",
                    host: "127.0.0.1",
                    players: "?",
                    thumb: "🏠",
                    status: "ONLINE"
                });
            }

            // Let's add some "Public" candidates that we verify via ping later
            // For now, if we find the local one, it's "real".
            
            resolve(servers);
        });
    });
}

module.exports = { getOnlineServers };

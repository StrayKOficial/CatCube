-- Hub Minigame Logic (FUNCTIONAL)
print("[Hub] Initializing minigame systems...")

local killBrickY = 3  -- Y position of kill bricks
local respawnPos = Vector3.new(0, 12, 0)
local bouncePower = 30

-- Track recent bounces to avoid spam
local lastBounceTime = 0
local bounceCooldown = 0.5

-- Main Update Loop
game.BindToUpdate(function(dt)
    local player = workspace:FindFirstChild("Player")
    if not player then return end
    
    local px, py, pz = player.Position.X, player.Position.Y, player.Position.Z
    
    -- ==============================
    -- KILLBRICK DETECTION (West Zone: X around -100)
    -- ==============================
    if px < -75 and px > -125 and pz > -25 and pz < 25 then
        -- Player is in the KillBrick zone
        -- Check if touching a red brick (simple Y proximity)
        if py < 4.5 and py > 2 then
            -- Check specific kill brick positions
            local killPositions = {
                {-110, 10}, {-90, 10}, {-100, -10},
                {-115, -5}, {-85, 0}, {-95, 15}, {-105, -15}
            }
            
            for _, pos in ipairs(killPositions) do
                local dx = math.abs(px - pos[1])
                local dz = math.abs(pz - pos[2])
                if dx < 3 and dz < 3 then
                    print("[KillBrick] Player touched! Respawning...")
                    player.Position = respawnPos
                    break
                end
            end
        end
    end
    
    -- ==============================
    -- BOUNCE PAD DETECTION (North Zone: Z around 100-200)
    -- ==============================
    if pz > 100 and pz < 210 then
        local bouncePads = {
            {0, 120}, {-5, 140}, {5, 160}, {0, 180}
        }
        
        local now = os.clock()
        if now - lastBounceTime > bounceCooldown then
            for _, pad in ipairs(bouncePads) do
                local dx = math.abs(px - pad[1])
                local dz = math.abs(pz - pad[2])
                if dx < 4 and dz < 4 and py < 4 then
                    print("[BouncePad] BOOST!")
                    -- Fake bounce by teleporting up (impulse not available in current API)
                    player.Position = Vector3.new(px, py + bouncePower, pz)
                    lastBounceTime = now
                    break
                end
            end
        end
    end
    
    -- ==============================
    -- VOID KILL (Fall off the map)
    -- ==============================
    if py < -20 then
        print("[Void] Player fell! Respawning...")
        player.Position = respawnPos
    end
    
    -- ==============================
    -- VICTORY DETECTION
    -- ==============================
    -- Parkour Victory (East, Y ~43)
    if px > 95 and px < 105 and py > 40 and pz > 50 and pz < 60 then
        print("[Victory] Parkour Challenge Complete!")
    end
    
    -- Tower Victory (Southwest, Y ~83)
    if px > -58 and px < -42 and py > 80 and pz > -108 and pz < -92 then
        print("[Victory] Tower Climb Complete!")
    end
    
    -- Race Victory (North, Z ~200)
    if pz > 195 and pz < 205 and px > -12 and px < 12 then
        print("[Victory] Race Complete!")
    end
end)

print("[Hub] Minigame systems ready!")
print("  - KillBricks: Active in West Zone")
print("  - BouncePads: Active in North Race Track")
print("  - Void Kill: Active everywhere")
print("  - Victory Zones: 3 locations")

-- Void Kill Script
print("Initializing Void Kill...")

local voidY = -50

-- Connect to Engine Update Loop
game.BindToUpdate(function(dt)
    local player = workspace:FindFirstChild("Player")
    if player then
        if player.Position.Y < voidY then
            print("Player fell into the Void! Respawning...")
            
            -- Teleport Player to safe zone
            player.Position = Vector3.new(0, 5, 0)
        end
    end
end)

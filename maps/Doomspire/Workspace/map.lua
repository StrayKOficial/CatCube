-- Doomspire Brickbattle Geometry (Single Tower)
print("Loading Doomspire...")

-- Tower Dimensions
local floorHeight = 15
local towerSize = 40

-- Layers
for f = 0, 3 do
    local y = f * floorHeight
    
    -- Floor Plate
    local plate = Instance.new("Part")
    plate.Name = "FloorPlate" .. f
    plate.Size = Vector3.new(towerSize, 1, towerSize)
    plate.Position = Vector3.new(0, y, 0)
    plate.Color = Vector3.new(0.2, 0.2, 0.8) -- Blue Team
    plate.Anchored = true
    plate.Parent = workspace
    
    -- Walls
    if f < 3 then
        for i = 1, 4 do
            local wall = Instance.new("Part")
            wall.Name = "SpireWall" .. f .. "_" .. i
            
            local offset = towerSize / 2 - 2
            local wx = (i % 2 == 0) and offset or -offset
            local wz = (i > 2) and offset or -offset
            
            -- Make walls only on edges
            if i <= 2 then
                wall.Size = Vector3.new(towerSize, floorHeight, 2)
                wall.Position = Vector3.new(0, y + floorHeight/2, wz)
            else
                wall.Size = Vector3.new(2, floorHeight, towerSize)
                wall.Position = Vector3.new(wx, y + floorHeight/2, 0)
            end
            
            wall.Color = Vector3.new(0.3, 0.3, 0.9)
            wall.Anchored = true
            wall.Parent = workspace
        end
    end
end

-- Central Pillar
local core = Instance.new("Part")
core.Name = "TowerCore"
core.Size = Vector3.new(10, 60, 10)
core.Position = Vector3.new(0, 30, 0)
core.Color = Vector3.new(0.1, 0.1, 0.5)
core.Anchored = true
core.Parent = workspace

print("Doomspire Spire Loaded!")

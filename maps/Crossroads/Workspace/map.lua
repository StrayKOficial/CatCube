-- Crossroads Map Geometry
print("Loading Crossroads...")

-- Large green field
local base = Instance.new("Part")
base.Name = "GreenField"
base.Size = Vector3.new(256, 1, 256)
base.Position = Vector3.new(0, -0.5, 0)
base.Color = Vector3.new(0.3, 0.5, 0.3)
base.Anchored = true
base.Parent = workspace

-- The "Cross" (Stone Paths)
local pathHorizontal = Instance.new("Part")
pathHorizontal.Name = "PathHorizontal"
pathHorizontal.Size = Vector3.new(200, 1.1, 20)
pathHorizontal.Position = Vector3.new(0, -0.4, 0)
pathHorizontal.Color = Vector3.new(0.6, 0.6, 0.6)
pathHorizontal.Anchored = true
pathHorizontal.Parent = workspace

local pathVertical = Instance.new("Part")
pathVertical.Name = "PathVertical"
pathVertical.Size = Vector3.new(20, 1.1, 200)
pathVertical.Position = Vector3.new(0, -0.4, 0)
pathVertical.Color = Vector3.new(0.6, 0.6, 0.6)
pathVertical.Anchored = true
pathVertical.Parent = workspace

-- Central Tower (Basic brick structure)
local towerBase = Instance.new("Part")
towerBase.Name = "TowerBase"
towerBase.Size = Vector3.new(30, 2, 30)
towerBase.Position = Vector3.new(0, 1, 0)
towerBase.Color = Vector3.new(0.4, 0.4, 0.4)
towerBase.Anchored = true
towerBase.Parent = workspace

for i = 1, 4 do
    local wall = Instance.new("Part")
    wall.Name = "TowerWall" .. i
    wall.Size = Vector3.new(5, 40, 5)
    
    local x = (i % 2 == 0) and 12.5 or -12.5
    local z = (i > 2) and 12.5 or -12.5
    
    wall.Position = Vector3.new(x, 20, z)
    wall.Color = Vector3.new(0.3, 0.3, 0.3)
    wall.Anchored = true
    wall.Parent = workspace
end

local towerTop = Instance.new("Part")
towerTop.Name = "TowerTop"
towerTop.Size = Vector3.new(35, 2, 35)
towerTop.Position = Vector3.new(0, 40, 0)
towerTop.Color = Vector3.new(0.5, 0.2, 0.2)
towerTop.Anchored = true
towerTop.Parent = workspace

-- Some decorative pillars
for i = 1, 8 do
    local pill = Instance.new("Part")
    pill.Name = "Pillar" .. i
    pill.Size = Vector3.new(4, 15, 4)
    local angle = (i / 8) * math.pi * 2
    local dist = 60
    pill.Position = Vector3.new(math.cos(angle) * dist, 7.5, math.sin(angle) * dist)
    pill.Color = Vector3.new(0.7, 0.7, 0.5)
    pill.Anchored = true
    pill.Parent = workspace
end

print("Crossroads Geometry Loaded!")

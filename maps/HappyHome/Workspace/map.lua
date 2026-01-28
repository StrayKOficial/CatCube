-- Happy Home in Robloxia Map Geometry
print("Loading Happy Home...")

-- Baseplate
local base = Instance.new("Part")
base.Name = "Baseplate"
base.Size = Vector3.new(128, 1, 128)
base.Position = Vector3.new(0, -0.5, 0)
base.Color = Vector3.new(0.3, 0.6, 0.3)
base.Anchored = true
base.Parent = workspace

-- House Floor
local floor = Instance.new("Part")
floor.Name = "HouseFloor"
floor.Size = Vector3.new(30, 0.2, 30)
floor.Position = Vector3.new(0, 0.1, 0)
floor.Color = Vector3.new(0.8, 0.7, 0.5)
floor.Anchored = true
floor.Parent = workspace

-- Walls
local wallHeight = 15
local wallOffset = 15

local walls = {
    {pos = Vector3.new(0, wallHeight/2, wallOffset), size = Vector3.new(30, wallHeight, 1)},
    {pos = Vector3.new(0, wallHeight/2, -wallOffset), size = Vector3.new(30, wallHeight, 1)},
    {pos = Vector3.new(wallOffset, wallHeight/2, 0), size = Vector3.new(1, wallHeight, 30)},
    {pos = Vector3.new(-wallOffset, wallHeight/2, 0), size = Vector3.new(1, wallHeight, 30)}
}

for i, w in ipairs(walls) do
    local wall = Instance.new("Part")
    wall.Name = "Wall" .. i
    wall.Size = w.size
    wall.Position = w.pos
    wall.Color = Vector3.new(0.9, 0.9, 0.95)
    wall.Anchored = true
    wall.Parent = workspace
end

-- Roof
local roof = Instance.new("Part")
roof.Name = "Roof"
roof.Size = Vector3.new(35, 1, 35)
roof.Position = Vector3.new(0, wallHeight + 0.5, 0)
roof.Color = Vector3.new(0.5, 0.2, 0.1)
roof.Anchored = true
roof.Parent = workspace

-- Path
local path = Instance.new("Part")
path.Name = "MainPath"
path.Size = Vector3.new(6, 0.2, 40)
path.Position = Vector3.new(0, 0.05, 35)
path.Color = Vector3.new(0.6, 0.6, 0.6)
path.Anchored = true
path.Parent = workspace

-- Some simple trees
for i = 1, 4 do
    local tx = (i > 2) and 40 or -40
    local tz = (i % 2 == 0) and 40 or -40
    
    local trunk = Instance.new("Part")
    trunk.Name = "TreeTrunk" .. i
    trunk.Size = Vector3.new(2, 10, 2)
    trunk.Position = Vector3.new(tx, 5, tz)
    trunk.Color = Vector3.new(0.4, 0.2, 0.1)
    trunk.Anchored = true
    trunk.Parent = workspace
    
    local leaves = Instance.new("Part")
    leaves.Name = "TreeLeaves" .. i
    leaves.Size = Vector3.new(8, 8, 8)
    leaves.Position = Vector3.new(tx, 13, tz)
    leaves.Color = Vector3.new(0.1, 0.6, 0.1)
    leaves.Anchored = true
    leaves.Parent = workspace
end

print("Happy Home Geometry Loaded!")

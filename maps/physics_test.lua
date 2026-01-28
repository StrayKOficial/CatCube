-- Physics Test Map
print("Loading Physics Test...")

-- Baseplate
local floor = Instance.new('Part')
floor.Size = Vector3.new(200, 1, 200)
floor.Position = Vector3.new(0, -0.5, 0)
floor.Color = Vector3.new(0.2, 0.2, 0.2)
floor.Anchored = true
floor.Parent = workspace

-- Wall of Cubes
for y = 0, 9 do
    for x = 0, 9 do
        local p = Instance.new('Part')
        p.Position = Vector3.new(x * 2.1 - 10, y * 2.1 + 1, 10)
        p.Size = Vector3.new(2, 2, 2)
        p.Color = Vector3.new(x/10, y/10, 1)
        p.Anchored = false
        p.Parent = workspace
    end
end

-- Ramp
local ramp = Instance.new('Part')
ramp.Size = Vector3.new(10, 1, 20)
ramp.Position = Vector3.new(-15, 5, 0)
ramp.Rotation = Vector3.new(0, 0, -30) -- TODO: Rotation support in Engine needed!
ramp.Color = Vector3.new(0.8, 0.5, 0)
ramp.Anchored = true
-- ramp.Parent = workspace -- Uncomment when Rotation is supported

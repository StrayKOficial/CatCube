-- Default Map
print("Loading Default Map...")

-- Green Baseplate
local baseplate = Instance.new('Part')
baseplate.Name = 'Baseplate'
baseplate.Size = Vector3.new(100, 1, 100)
baseplate.Position = Vector3.new(0, -0.5, 0)
baseplate.Color = Vector3.new(0.3, 0.5, 0.3)
baseplate.Anchored = true
baseplate.Parent = workspace

-- Falling Cubes
for i = 1, 5 do
    local p = Instance.new('Part')
    p.Name = 'Cube' .. i
    p.Position = Vector3.new(i * 3, 10, 5)
    p.Size = Vector3.new(2, 2, 2)
    p.Color = Vector3.new(1, 0, 0)
    p.Anchored = false
    p.Parent = workspace
end

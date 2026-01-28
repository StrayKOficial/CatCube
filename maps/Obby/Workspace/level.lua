-- Obby Level Generation
print("Loading Obby Level...")

-- Obby Level Generation
print("Loading Obby Level...")

-- Void (No Floor)
-- The script 'void_kill.lua' will handle death below Y = -50

-- Checkpoint 1 (Green)
local startPad = Instance.new("Part")
startPad.Name = "Start"
startPad.Size = Vector3.new(10, 1, 10)
startPad.Position = Vector3.new(0, 0, 0)
startPad.Color = Vector3.new(0, 1, 0)
startPad.Anchored = true
startPad.Parent = workspace

-- Parkour Jumps
for i = 1, 10 do
    local p = Instance.new("Part")
    p.Name = "JumpPad" .. i
    p.Size = Vector3.new(4, 1, 4)
    p.Position = Vector3.new(0, i * 1.5, i * 6) -- Rising steps
    p.Color = Vector3.new(0.5, 0.5, 1)
    p.Anchored = true
    p.Parent = workspace
end

print("Obby Level Loaded!")

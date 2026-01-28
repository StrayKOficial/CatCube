-- Classic 2009 HUD Logic
print("Initializing Classic HUD...")

local coreGui = game:GetService("CoreGui")

-- Classic Health Bar Background (Red)
local healthBg = Instance.new("Frame")
healthBg.Name = "HealthBackground"
healthBg.Position = Vector2.new(10, 10)
healthBg.Size = Vector2.new(200, 20)
healthBg.Color = Vector3.new(0.5, 0, 0)
healthBg.Parent = coreGui

-- Health Fill (Green)
local healthFill = Instance.new("Frame")
healthFill.Name = "HealthFill"
healthFill.Position = Vector2.new(10, 10)
healthFill.Size = Vector2.new(200, 20)
healthFill.Color = Vector3.new(0, 0.8, 0)
healthFill.Parent = coreGui

print("Classic HUD Loaded!")

-- CatCube Monumental Hub with MINIGAMES
local Workspace = game:GetService("Workspace")

-- ============================================
-- SECTION 1: CENTRAL HUB STRUCTURE
-- ============================================

-- Center Base
local base = Instance.new("Part")
base.Name = "HubBase"
base.Position = Vector3.new(0, 0, 0)
base.Size = Vector3.new(300, 2, 300)
base.Color = Vector3.new(0.2, 0.2, 0.2)
base.Anchored = true
base.Parent = Workspace

-- Monument Pedestal
local pedestal = Instance.new("Part")
pedestal.Name = "Pedestal"
pedestal.Position = Vector3.new(0, 5, 0)
pedestal.Size = Vector3.new(20, 10, 20)
pedestal.Color = Vector3.new(0.4, 0.4, 0.4)
pedestal.Anchored = true
pedestal.Parent = Workspace

-- Main Spawn
local spawn = Instance.new("SpawnLocation")
spawn.Name = "MainSpawn"
spawn.Position = Vector3.new(0, 11, 0)
spawn.Size = Vector3.new(12, 1, 12)
spawn.Anchored = true
spawn.Parent = Workspace

-- Decorative Pillars
local function createPillar(x, z, h, color)
    local p = Instance.new("Part")
    p.Name = "Pillar"
    p.Position = Vector3.new(x, h/2, z)
    p.Size = Vector3.new(4, h, 4)
    p.Color = color or Vector3.new(0.6, 0.6, 0.6)
    p.Anchored = true
    p.Parent = Workspace
end

createPillar(40, 40, 30)
createPillar(-40, 40, 30)
createPillar(40, -40, 30)
createPillar(-40, -40, 30)

-- ============================================
-- SECTION 2: MINIGAME 1 - PARKOUR CHALLENGE
-- ============================================

local function createParkourPlatform(x, y, z, sizeX, sizeZ, color)
    local p = Instance.new("Part")
    p.Name = "ParkourPlatform"
    p.Position = Vector3.new(x, y, z)
    p.Size = Vector3.new(sizeX, 1, sizeZ)
    p.Color = color or Vector3.new(0.2, 0.6, 1.0)
    p.Anchored = true
    p.Parent = Workspace
end

-- Parkour Start Sign
local parkourSign = Instance.new("Part")
parkourSign.Name = "ParkourSign"
parkourSign.Position = Vector3.new(100, 8, 0)
parkourSign.Size = Vector3.new(6, 12, 2)
parkourSign.Color = Vector3.new(0.0, 0.8, 1.0)
parkourSign.Anchored = true
parkourSign.Parent = Workspace

-- Parkour Tower
createParkourPlatform(100, 3, 0, 12, 12, Vector3.new(0.3, 0.3, 0.3))
createParkourPlatform(105, 8, 5, 6, 6, Vector3.new(0.2, 0.7, 0.9))
createParkourPlatform(100, 13, 12, 6, 6, Vector3.new(0.2, 0.7, 0.9))
createParkourPlatform(95, 18, 18, 6, 6, Vector3.new(0.2, 0.7, 0.9))
createParkourPlatform(100, 23, 25, 6, 6, Vector3.new(0.2, 0.7, 0.9))
createParkourPlatform(108, 28, 30, 6, 6, Vector3.new(0.2, 0.7, 0.9))
createParkourPlatform(100, 33, 38, 6, 6, Vector3.new(0.2, 0.7, 0.9))
createParkourPlatform(92, 38, 45, 6, 6, Vector3.new(0.2, 0.7, 0.9))
createParkourPlatform(100, 43, 55, 10, 10, Vector3.new(1.0, 0.8, 0.0)) -- Victory Platform (Gold)

-- ============================================
-- SECTION 3: MINIGAME 2 - KILLBRICK SURVIVAL
-- ============================================

local killZoneBase = Instance.new("Part")
killZoneBase.Name = "KillZoneBase"
killZoneBase.Position = Vector3.new(-100, 1, 0)
killZoneBase.Size = Vector3.new(50, 2, 50)
killZoneBase.Color = Vector3.new(0.3, 0.3, 0.3)
killZoneBase.Anchored = true
killZoneBase.Parent = Workspace

-- Kill Bricks (Red = Danger!)
local function createKillBrick(x, y, z)
    local k = Instance.new("Part")
    k.Name = "KillBrick"
    k.Position = Vector3.new(x, y, z)
    k.Size = Vector3.new(4, 1, 4)
    k.Color = Vector3.new(1.0, 0.0, 0.0)
    k.Anchored = true
    k.Parent = Workspace
end

createKillBrick(-110, 3, 10)
createKillBrick(-90, 3, 10)
createKillBrick(-100, 3, -10)
createKillBrick(-115, 3, -5)
createKillBrick(-85, 3, 0)
createKillBrick(-95, 3, 15)
createKillBrick(-105, 3, -15)

-- Safe Platforms (Green = Safe!)
local function createSafePlatform(x, y, z)
    local s = Instance.new("Part")
    s.Name = "SafePlatform"
    s.Position = Vector3.new(x, y, z)
    s.Size = Vector3.new(6, 1, 6)
    s.Color = Vector3.new(0.0, 1.0, 0.3)
    s.Anchored = true
    s.Parent = Workspace
end

createSafePlatform(-100, 3, 0)
createSafePlatform(-110, 3, -10)
createSafePlatform(-90, 3, -10)
createSafePlatform(-100, 3, 20)

-- ============================================
-- SECTION 4: MINIGAME 3 - BOUNCE RACE TRACK
-- ============================================

local raceStart = Instance.new("Part")
raceStart.Name = "RaceStart"
raceStart.Position = Vector3.new(0, 1, 100)
raceStart.Size = Vector3.new(20, 2, 10)
raceStart.Color = Vector3.new(0.0, 1.0, 0.0)
raceStart.Anchored = true
raceStart.Parent = Workspace

local raceFinish = Instance.new("Part")
raceFinish.Name = "RaceFinish"
raceFinish.Position = Vector3.new(0, 1, 200)
raceFinish.Size = Vector3.new(20, 2, 10)
raceFinish.Color = Vector3.new(1.0, 0.8, 0.0)
raceFinish.Anchored = true
raceFinish.Parent = Workspace

-- Race Track Walls
local function createWall(x, y, z, sx, sy, sz)
    local w = Instance.new("Part")
    w.Name = "RaceWall"
    w.Position = Vector3.new(x, y, z)
    w.Size = Vector3.new(sx, sy, sz)
    w.Color = Vector3.new(0.5, 0.5, 0.5)
    w.Anchored = true
    w.Parent = Workspace
end

createWall(-12, 3, 150, 2, 4, 100)  -- Left wall
createWall(12, 3, 150, 2, 4, 100)   -- Right wall

-- Bounce Pads (Cyan = Boost!)
local function createBouncePad(x, z)
    local b = Instance.new("Part")
    b.Name = "BouncePad"
    b.Position = Vector3.new(x, 2, z)
    b.Size = Vector3.new(6, 0.5, 6)
    b.Color = Vector3.new(0.0, 1.0, 1.0)
    b.Anchored = true
    b.Parent = Workspace
end

createBouncePad(0, 120)
createBouncePad(-5, 140)
createBouncePad(5, 160)
createBouncePad(0, 180)

-- ============================================
-- SECTION 5: MINIGAME 4 - TOWER CLIMB
-- ============================================

local towerBase = Instance.new("Part")
towerBase.Name = "TowerBase"
towerBase.Position = Vector3.new(-50, 1, -100)
towerBase.Size = Vector3.new(30, 2, 30)
towerBase.Color = Vector3.new(0.4, 0.35, 0.3)
towerBase.Anchored = true
towerBase.Parent = Workspace

-- Spiral Platforms going up
for i = 1, 20 do
    local angle = i * 0.5
    local radius = 12
    local x = -50 + math.cos(angle) * radius
    local z = -100 + math.sin(angle) * radius
    local y = 3 + i * 4

    local platform = Instance.new("Part")
    platform.Name = "TowerStep" .. i
    platform.Position = Vector3.new(x, y, z)
    platform.Size = Vector3.new(6, 1, 6)
    platform.Color = Vector3.new(0.6 + i*0.02, 0.4, 0.2)
    platform.Anchored = true
    platform.Parent = Workspace
end

-- Tower Victory Platform
local towerTop = Instance.new("Part")
towerTop.Name = "TowerTop"
towerTop.Position = Vector3.new(-50, 85, -100)
towerTop.Size = Vector3.new(15, 2, 15)
towerTop.Color = Vector3.new(1.0, 0.8, 0.0)
towerTop.Anchored = true
towerTop.Parent = Workspace

-- ============================================
-- SECTION 6: FLOATING ISLANDS (Eye Candy)
-- ============================================

local function createFloatingIsland(x, y, z, size, color)
    local i = Instance.new("Part")
    i.Name = "FloatingIsland"
    i.Position = Vector3.new(x, y, z)
    i.Size = Vector3.new(size, size * 0.3, size)
    i.Color = color or Vector3.new(0.3, 0.6, 0.3)
    i.Anchored = true
    i.Parent = Workspace
end

createFloatingIsland(60, 40, 60, 20, Vector3.new(0.3, 0.7, 0.3))
createFloatingIsland(-70, 50, 70, 15, Vector3.new(0.4, 0.65, 0.4))
createFloatingIsland(80, 35, -60, 18, Vector3.new(0.35, 0.6, 0.35))
createFloatingIsland(-50, 60, -70, 12, Vector3.new(0.45, 0.7, 0.4))

print("[Hub] Map loaded with 4 MINIGAMES!")
print("  1. Parkour Challenge (East)")
print("  2. KillBrick Survival (West)")
print("  3. Bounce Race Track (North)")
print("  4. Tower Climb (Southwest)")

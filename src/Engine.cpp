#include "Engine.hpp"
#include "CharacterHelper.hpp"
#include <iostream>
#include <cmath>

namespace CatCube {

Engine::Engine() {
}

Engine::~Engine() {
    shutdown();
}

bool Engine::init(const std::string& title, int width, int height, bool headless) {
    m_width = width;
    m_height = height;
    m_headless = headless;

    if (m_headless) {
        std::cout << "Engine: Running in HEADLESS mode (Server)." << std::endl;
    }

    // Initialize SDL
    uint32_t sdlFlags = SDL_INIT_TIMER | SDL_INIT_EVENTS;
    if (!m_headless) sdlFlags |= SDL_INIT_VIDEO;

    if (SDL_Init(sdlFlags) != 0) {
        std::cerr << "SDL_Init failed: " << SDL_GetError() << std::endl;
        return false;
    }

    if (!m_headless) {
        SDL_GL_SetAttribute(SDL_GL_CONTEXT_MAJOR_VERSION, 2);
        SDL_GL_SetAttribute(SDL_GL_CONTEXT_MINOR_VERSION, 1);
        SDL_GL_SetAttribute(SDL_GL_DOUBLEBUFFER, 1);
        SDL_GL_SetAttribute(SDL_GL_DEPTH_SIZE, 24);

        m_window = SDL_CreateWindow(
            title.c_str(),
            SDL_WINDOWPOS_CENTERED,
            SDL_WINDOWPOS_CENTERED,
            m_width,
            m_height,
            SDL_WINDOW_SHOWN | SDL_WINDOW_RESIZABLE | SDL_WINDOW_OPENGL
        );

        if (!m_window) {
            std::cerr << "SDL_CreateWindow failed: " << SDL_GetError() << std::endl;
            return false;
        }

        m_glContext = SDL_GL_CreateContext(m_window);
        if (!m_glContext) {
            std::cerr << "SDL_GL_CreateContext failed: " << SDL_GetError() << std::endl;
            return false;
        }

        SDL_GL_SetSwapInterval(1);
        m_renderer.init(m_width, m_height);
    }
    
    m_physics.init();
    
    m_camera.target = {0, 0, 0};
    m_camera.distance = 30.0f;
    m_camera.yaw = 45.0f;
    m_camera.pitch = -25.0f;
    m_camera.updateDirection();
    if (!m_headless) m_renderer.setCamera(m_camera);

    m_scriptService.init();
    m_networkService.init();

    // --- NETWORKING CALLBACKS ---
    m_networkService.onPlayerJoined = [this](uint32_t id) {
        std::cout << "Engine: Player " << id << " connected." << std::endl;
    };

    m_networkService.onPlayerLeft = [this](uint32_t id) {
        std::cout << "Engine: Player " << id << " left." << std::endl;
        if (m_remotePlayers.count(id)) {
            m_remotePlayers[id]->setParent(nullptr);
            m_remotePlayers.erase(id);
            m_remoteTargets.erase(id);
            m_remoteYaws.erase(id);
        }
    };

    m_networkService.onMapReceived = [this](const std::string& name) {
        std::cout << "Engine: Incoming Map Request [" << name << "]" << std::endl;
        std::string scriptPath = "../maps/" + name + ".lua";
        if (!m_scriptService.runFile(scriptPath)) {
             m_scriptService.runFile(name);
        }
        this->setWorld(m_world);

        if (!m_character && !m_localPlayerName.empty()) {
            this->spawnCharacter(m_localPlayerName, {0, 10, 0});
        }
    };

    m_networkService.onPositionReceived = [this](uint32_t id, Vector3 pos, float yaw) {
        if (m_remotePlayers.find(id) == m_remotePlayers.end()) {
            auto remoteName = (id == 0) ? "Host_Server" : "Guest_" + std::to_string(id);
            auto remoteChar = CharacterHelper::createCharacter(remoteName, pos);
            if (m_world) remoteChar->setParent(m_world);
            m_remotePlayers[id] = remoteChar;
            CharacterHelper::syncRemoteVisuals(remoteChar, pos, 0.016f);
        }
        m_remoteTargets[id] = pos;
        m_remoteYaws[id] = yaw;
    };
    
    if (!m_headless) {
        SDL_SetRelativeMouseMode(SDL_FALSE);
        m_mouseCaptured = false;
    }

    m_running = true;
    m_lastTime = SDL_GetPerformanceCounter();
    return true;
}

void Engine::spawnCharacter(const std::string& name, Vector3 pos) {
    if (m_character) return;
    std::cout << "Engine: Spawn local character " << name << std::endl;
    m_character = CharacterHelper::createCharacter(name, pos);
    if (m_world) m_character->setParent(m_world);
    registerPhysicsRecursively(m_character);
}

void Engine::setWorld(InstancePtr world) {
    m_world = world;
    registerPhysicsRecursively(m_world);
}

void Engine::registerPhysicsRecursively(InstancePtr instance) {
    auto basePart = std::dynamic_pointer_cast<BasePart>(instance);
    if (basePart) m_physics.addPart(basePart);
    for (const auto& child : instance->getChildren()) registerPhysicsRecursively(child);
}

void Engine::run() {
    while (m_running) {
        uint64_t currentTime = SDL_GetPerformanceCounter();
        m_deltaTime = (float)(currentTime - m_lastTime) / SDL_GetPerformanceFrequency();
        m_lastTime = currentTime;
        if (m_deltaTime > 0.1f) m_deltaTime = 0.1f;
        m_fps = 1.0f / m_deltaTime;

        processInput();
        update(m_deltaTime);
        render();
    }
}

void Engine::processInput() {
    if (m_headless) return;
    m_mouseDeltaX = 0; m_mouseDeltaY = 0;
    SDL_Event event;
    while (SDL_PollEvent(&event)) {
        if (event.type == SDL_QUIT) m_running = false;
        else if (event.type == SDL_KEYDOWN) {
            if (event.key.keysym.scancode < 512) m_keys[event.key.keysym.scancode] = true;
            if (event.key.keysym.sym == SDLK_ESCAPE) m_running = false;
            if (event.key.keysym.sym == SDLK_TAB) {
                m_mouseCaptured = !m_mouseCaptured;
                SDL_SetRelativeMouseMode(m_mouseCaptured ? SDL_TRUE : SDL_FALSE);
            }
        }
        else if (event.type == SDL_KEYUP) {
            if (event.key.keysym.scancode < 512) m_keys[event.key.keysym.scancode] = false;
        }
        else if (event.type == SDL_MOUSEMOTION) {
            m_mouseDeltaX = event.motion.xrel; m_mouseDeltaY = event.motion.yrel;
        }
    }
}

void Engine::update(float deltaTime) {
    m_physics.update(deltaTime);
    m_networkService.update();

    Vector3 moveDir{0, 0, 0};
    float yawRad = m_camera.yaw * 3.14159f / 180.0f;
    Vector3 forward = {-std::sin(yawRad), 0, -std::cos(yawRad)};
    Vector3 right = {std::cos(yawRad), 0, -std::sin(yawRad)};
    
    if (m_keys[SDL_SCANCODE_W]) moveDir = moveDir + forward;
    if (m_keys[SDL_SCANCODE_S]) moveDir = moveDir - forward;
    if (m_keys[SDL_SCANCODE_A]) moveDir = moveDir - right;
    if (m_keys[SDL_SCANCODE_D]) moveDir = moveDir + right;
    if (moveDir.length() > 0.1f) moveDir = moveDir.normalized();
    
    bool jump = m_keys[SDL_SCANCODE_SPACE];

    if (m_character) {
        CharacterHelper::updateCharacterPhysics(m_character, moveDir, jump, m_physics, deltaTime);
        
        m_networkTimer += deltaTime;
        if (m_networkTimer >= 0.033f) {
            m_networkTimer = 0;
            auto root = std::dynamic_pointer_cast<BasePart>(m_character->getPrimaryPart());
            auto hum = std::dynamic_pointer_cast<Humanoid>(m_character->findFirstChild("Humanoid"));
            if (root) m_networkService.sendPosition(root->getPosition(), hum ? hum->currentYaw : 0);
        }

        auto head = m_character->findFirstChild("Head");
        if (head && !m_headless) {
            m_camera.target = std::dynamic_pointer_cast<BasePart>(head)->getPosition();
            float pitchRad = m_camera.pitch * 3.14159f / 180.0f;
            float yawRadCam = m_camera.yaw * 3.14159f / 180.0f;
            float hDist = m_camera.distance * std::cos(pitchRad);
            float vDist = m_camera.distance * std::sin(pitchRad);
            m_camera.position.x = m_camera.target.x + std::sin(yawRadCam) * hDist;
            m_camera.position.z = m_camera.target.z + std::cos(yawRadCam) * hDist;
            m_camera.position.y = m_camera.target.y - vDist;
        }
    }

    for (auto& pair : m_remotePlayers) {
        if (m_remoteTargets.count(pair.first)) {
            Vector3 target = m_remoteTargets[pair.first];
            float targetYaw = m_remoteYaws.count(pair.first) ? m_remoteYaws[pair.first] : 0;
            auto root = std::dynamic_pointer_cast<BasePart>(pair.second->getPrimaryPart());
            auto hum = std::dynamic_pointer_cast<Humanoid>(pair.second->findFirstChild("Humanoid"));
            if (root && hum) {
                Vector3 current = root->getPosition();
                Vector3 nextPos = current + (target - current) * 0.2f;
                float diff = targetYaw - hum->currentYaw;
                while (diff > 180) diff -= 360; while (diff < -180) diff += 360;
                hum->currentYaw += diff * 0.2f;
                CharacterHelper::syncRemoteVisuals(pair.second, nextPos, deltaTime);
            }
        }
    }
}

void Engine::render() {
    if (m_headless) return;
    m_renderer.beginFrame();
    if (m_world) m_renderer.renderHierarchy(m_world);
    m_renderer.endFrame();
    SDL_GL_SwapWindow(m_window);
}

void Engine::shutdown() {
    m_running = false;
    if (m_glContext) SDL_GL_DeleteContext(m_glContext);
    if (m_window) SDL_DestroyWindow(m_window);
    m_networkService.shutdown();
}

} // namespace CatCube

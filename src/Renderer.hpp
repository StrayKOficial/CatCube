#pragma once

#include "Part.hpp"
#include "Shader.hpp"
#include <GL/glew.h>
#include <vector>
#include <cmath>

namespace CatCube {

/**
 * Camera - 3D camera for viewing the scene
 */
class Camera {
public:
    Vector3 position{0, 10, 20};
    Vector3 target{0, 0, 0};
    Vector3 up{0, 1, 0};
    
    float fov = 70.0f;
    float nearPlane = 0.1f;
    float farPlane = 1000.0f;
    
    // Orbit camera controls
    float yaw = 0.0f;
    float pitch = -20.0f;
    float distance = 25.0f;
    
    void updateDirection() {
        float yawRad = yaw * 3.14159f / 180.0f;
        float pitchRad = pitch * 3.14159f / 180.0f;
        
        // Calculate direction vector
        Vector3 direction;
        direction.x = cos(pitchRad) * sin(yawRad);
        direction.y = sin(pitchRad);
        direction.z = -cos(pitchRad) * cos(yawRad); // Negative Z for forward
        
        // Target is position + direction
        target = position + direction;
    }
};

/**
 * Renderer - Renders Parts using OpenGL
 */
class Renderer {
public:
    Renderer();
    ~Renderer();
    
    void init(int width, int height);
    void resize(int width, int height);
    
    void beginFrame();
    void endFrame();
    
    // Set camera
    void setCamera(const Camera& camera);
    
    // Render all parts in an instance hierarchy
    void renderHierarchy(InstancePtr root);

private:
    void setupProjection();
    void renderShadowPass(InstancePtr root);
    void renderMainPass(InstancePtr root);
    
    void renderPart(const BasePart& part, Shader& shader);
    void renderCube(const Vector3& pos, const Vector3& size, const Vector3& rot, const Color3& color, Shader& shader);
    void renderStuds(const Vector3& pos, const Vector3& size, const Vector3& rot, Shader& shader);
    
    void lookAt(const Vector3& eye, const Vector3& center, const Vector3& up);

    int m_width = 1280;
    int m_height = 720;
    Camera m_camera;

    // === Shadow Mapping ===
    GLuint m_shadowFBO = 0;
    GLuint m_shadowMap = 0;
    const int SHADOW_WIDTH = 2048;
    const int SHADOW_HEIGHT = 2048;

    Shader m_shaderShadow;
    Shader m_shaderMain;

    float m_lightSpaceMatrix[16];
    
    // Cube vertices (will use immediate mode for simplicity - Roblox 2009 style!)
    void drawCubeFace(float x1, float y1, float z1,
                      float x2, float y2, float z2,
                      float x3, float y3, float z3,
                      float x4, float y4, float z4,
                      float nx, float ny, float nz);
                      
    // Display Lists
    GLuint m_cubeList = 0;
    GLuint m_studList = 0;
    void createDisplayLists();
};

} // namespace CatCube

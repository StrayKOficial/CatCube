#pragma once

#include "Instance.hpp"
#include <array>
#include <cmath>

namespace CatCube {

/**
 * Vector3 - 3D vector (like Roblox Vector3)
 */
struct Vector3 {
    float x = 0, y = 0, z = 0;
    
    Vector3() = default;
    Vector3(float x, float y, float z) : x(x), y(y), z(z) {}
    
    Vector3 operator+(const Vector3& other) const { return {x + other.x, y + other.y, z + other.z}; }
    Vector3 operator-(const Vector3& other) const { return {x - other.x, y - other.y, z - other.z}; }
    Vector3 operator*(float s) const { return {x * s, y * s, z * s}; }
    Vector3 operator/(float s) const { return {x / s, y / s, z / s}; }
    
    float dot(const Vector3& other) const { return x * other.x + y * other.y + z * other.z; }
    Vector3 cross(const Vector3& other) const {
        return {
            y * other.z - z * other.y,
            z * other.x - x * other.z,
            x * other.y - y * other.x
        };
    }
    
    float length() const { return std::sqrt(x*x + y*y + z*z); }
    Vector3 normalized() const { float l = length(); return l > 0 ? *this / l : Vector3(); }
};

/**
 * Color3 - RGB color (like Roblox Color3)
 */
struct Color3 {
    float r = 1, g = 1, b = 1;
    
    Color3() = default;
    Color3(float r, float g, float b) : r(r), g(g), b(b) {}
    
    // Common Roblox 2009 colors
    static Color3 Gray() { return {0.64f, 0.64f, 0.64f}; }
    static Color3 DarkGray() { return {0.43f, 0.43f, 0.43f}; }
    static Color3 White() { return {1.0f, 1.0f, 1.0f}; }
    static Color3 Black() { return {0.1f, 0.1f, 0.1f}; }
    static Color3 Red() { return {0.77f, 0.16f, 0.16f}; }
    static Color3 Green() { return {0.16f, 0.77f, 0.16f}; }
    static Color3 Blue() { return {0.16f, 0.16f, 0.77f}; }
    static Color3 Yellow() { return {0.96f, 0.8f, 0.19f}; }
    static Color3 Brown() { return {0.49f, 0.36f, 0.27f}; }
    static Color3 BrightGreen() { return {0.29f, 0.59f, 0.29f}; }
};

/**
 * BasePart - Base class for all 3D parts (like Roblox BasePart)
 */
class BasePart : public Instance {
public:
    BasePart(const std::string& className = "BasePart");
    
    bool isA(const std::string& className) const override;
    
    // Position
    const Vector3& getPosition() const { return m_position; }
    void setPosition(const Vector3& pos) { m_position = pos; }
    
    // Size
    const Vector3& getSize() const { return m_size; }
    void setSize(const Vector3& size) { m_size = size; }
    
    // Rotation (Euler angles in degrees)
    const Vector3& getRotation() const { return m_rotation; }
    void setRotation(const Vector3& rot) { m_rotation = rot; }
    
    // Color
    const Color3& getColor() const { return m_color; }
    void setColor(const Color3& color) { m_color = color; }
    
    // Physics properties
    bool isAnchored() const { return m_anchored; }
    void setAnchored(bool anchored) { m_anchored = anchored; }
    
    bool canCollide() const { return m_canCollide; }
    void setCanCollide(bool canCollide) { m_canCollide = canCollide; }
    
    // Transparency (0 = opaque, 1 = invisible)
    float getTransparency() const { return m_transparency; }
    void setTransparency(float t) { m_transparency = t; }

    InstancePtr clone() const override;

protected:
    Vector3 m_position{0, 0, 0};
    Vector3 m_size{4, 1, 2};  // Default Roblox part size
    Vector3 m_rotation{0, 0, 0};
    Color3 m_color = Color3::Gray();
    bool m_anchored = false;
    bool m_canCollide = true;
    float m_transparency = 0.0f;
};

/**
 * Part - A basic brick/block (like Roblox Part)
 */
class Part : public BasePart {
public:
    static constexpr const char* ClassName = "Part";
    
    Part();
    
    bool isA(const std::string& className) const override;
    InstancePtr clone() const override;
};

/**
 * SpawnLocation - Where players spawn
 */
class SpawnLocation : public BasePart {
public:
    static constexpr const char* ClassName = "SpawnLocation";
    
    SpawnLocation();
    
    bool isA(const std::string& className) const override;
};

} // namespace CatCube

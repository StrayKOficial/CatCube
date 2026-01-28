#pragma once

#include "Instance.hpp"
#include "Part.hpp"

namespace CatCube {

class Humanoid : public Instance {
public:
    static constexpr const char* ClassName = "Humanoid";
    
    Humanoid();
    
    bool isA(const std::string& className) const override;
    
    // Properties
    float getHealth() const { return m_health; }
    void setHealth(float health) { m_health = health; }
    
    float getMaxHealth() const { return m_maxHealth; }
    void setMaxHealth(float max) { m_maxHealth = max; }
    
    float getWalkSpeed() const { return m_walkSpeed; }
    void setWalkSpeed(float speed) { m_walkSpeed = speed; }
    
    float getJumpPower() const { return m_jumpPower; }
    void setJumpPower(float power) { m_jumpPower = power; }
    
    // Movement Control
    void move(const Vector3& direction, bool jump);
    
    // Helper to find the RootPart
    std::shared_ptr<BasePart> getRootPart() const;

    // Animation State (Public for easy access from CharacterHelper)
    float walkCycle = 0.0f;
    float breatheCycle = 0.0f;
    float currentLegAngle = 0.0f;
    float currentArmAngle = 0.0f;
    float currentTorsoTilt = 0.0f;
    float currentTorsoRoll = 0.0f;
    float currentYaw = 0.0f;
    float currentBobY = 0.0f;
    
private:
    float m_health = 100.0f;
    float m_maxHealth = 100.0f;
    float m_walkSpeed = 16.0f;
    float m_jumpPower = 50.0f;
    
    // Movement input state to be applied in physics step
    Vector3 m_moveDirection{0, 0, 0};
    bool m_jump = false;
};

} // namespace CatCube

#pragma once

#include "Engine.hpp"
#include "Humanoid.hpp"
#include "Model.hpp"
#include "Part.hpp"
#include <iostream>
#include <cmath>

namespace CatCube {

class CharacterHelper {
public:
    static std::shared_ptr<Model> createCharacter(const std::string& name, const Vector3& position) {
        auto character = std::make_shared<Model>();
        character->setName(name);
        
        auto humanoid = std::make_shared<Humanoid>();
        humanoid->setParent(character);
        
        // === HumanoidRootPart (Invisible Collider) ===
        auto root = std::make_shared<Part>();
        root->setName("HumanoidRootPart");
        root->setSize({4, 10, 4}); 
        root->setPosition({position.x, position.y + 5.0f, position.z});
        root->setColor(Color3::Red()); // Still red but now hidden by renderer
        root->setTransparency(1.0f);   // Invisible
        root->setAnchored(false);
        root->setCanCollide(true);
        root->setParent(character);
        
        // === Visual Parts (Non-colliding) ===
        // Offsets will be managed in update loop relative to Root
        
        auto torso = std::make_shared<Part>();
        torso->setName("Torso");
        torso->setSize({4, 4, 2});
        torso->setPosition(root->getPosition());
        torso->setColor(Color3::Yellow());
        torso->setAnchored(false);
        torso->setCanCollide(false);
        torso->setParent(character);
        
        auto head = std::make_shared<Part>();
        head->setName("Head");
        head->setSize({2, 2, 2}); // Bigger block head (2x2x2 to match arm depth)
        head->setPosition(root->getPosition());
        head->setColor(Color3::Yellow());
        head->setAnchored(false);
        head->setCanCollide(false);
        head->setParent(character);
        
        auto leftLeg = std::make_shared<Part>();
        leftLeg->setName("LeftLeg");
        leftLeg->setSize({2, 4, 2}); // R6 Standard
        leftLeg->setPosition(root->getPosition());
        leftLeg->setColor(Color3::Green()); // Both legs green
        leftLeg->setAnchored(false);
        leftLeg->setCanCollide(false);
        leftLeg->setParent(character);

        auto rightLeg = std::make_shared<Part>();
        rightLeg->setName("RightLeg");
        rightLeg->setSize({2, 4, 2}); 
        rightLeg->setPosition(root->getPosition());
        rightLeg->setColor(Color3::Green());
        rightLeg->setAnchored(false);
        rightLeg->setCanCollide(false);
        rightLeg->setParent(character);
        
        // Arms (Yellow)
        auto leftArm = std::make_shared<Part>();
        leftArm->setName("LeftArm");
        leftArm->setSize({2, 4, 2});
        leftArm->setPosition(root->getPosition());
        leftArm->setColor(Color3::Yellow());
        leftArm->setAnchored(false);
        leftArm->setCanCollide(false);
        leftArm->setParent(character);
        
        auto rightArm = std::make_shared<Part>();
        rightArm->setName("RightArm");
        rightArm->setSize({2, 4, 2}); 
        rightArm->setPosition(root->getPosition());
        rightArm->setColor(Color3::Yellow());
        rightArm->setAnchored(false);
        rightArm->setCanCollide(false);
        rightArm->setParent(character);


        
        character->setPrimaryPart(root);
        
        return character;
    }
    
    static void updateCharacterPhysics(std::shared_ptr<Model> character, const Vector3& moveDir, bool jump, PhysicsService& physics, float m_deltaTime) {
        if (!character) return;
        
        auto rootPart = std::dynamic_pointer_cast<BasePart>(character->getPrimaryPart());
        auto humanoid = std::dynamic_pointer_cast<Humanoid>(character->findFirstChild("Humanoid"));
        
        if (humanoid && rootPart) {
            // == PHYSICS ==
            physics.setPartAngularFactor(rootPart, {0, 0, 0}); 
            physics.setPartFriction(rootPart, 0.0f);
            
            Vector3 currentVel = physics.getPartVelocity(rootPart);
            float speed = humanoid->getWalkSpeed();
            Vector3 targetVel = moveDir * speed;
            Vector3 finalVel = {targetVel.x, currentVel.y, targetVel.z};
            
            if (moveDir.length() < 0.01f) {
                finalVel.x *= 0.9f;
                finalVel.z *= 0.9f;
                if (std::abs(finalVel.x) < 0.1f) finalVel.x = 0;
                if (std::abs(finalVel.z) < 0.1f) finalVel.z = 0;
            }
            
            if (jump && std::abs(currentVel.y) < 0.5f) { // Increased threshold slightly
                finalVel.y = humanoid->getJumpPower();
            }
            
            physics.setPartVelocity(rootPart, finalVel);
            
            // == VISUALS SYNC & OVERHAULED ANIMATION ==
            Vector3 rp = rootPart->getPosition();
            
            // Calculate visual rotation (Face movements)
            if (moveDir.length() > 0.1f) {
                float targetYaw = atan2(moveDir.x, moveDir.z) * 180.0f / 3.14159f;
                float diff = targetYaw - humanoid->currentYaw;
                while (diff > 180) diff -= 360;
                while (diff < -180) diff += 360;
                humanoid->currentYaw += diff * 0.12f; // Slower turn for "mass"
            }
            
            // Logic
            float horizSpeed = Vector3(currentVel.x, 0, currentVel.z).length();
            bool isMoving = horizSpeed > 1.0f;
            bool isJumping = currentVel.y > 1.0f;
            bool isFalling = currentVel.y < -1.0f;
            
            // --- UPDATE CYCLES (Dramatically slower) ---
            float walkAnimSpeed = 0.55f; // Rhythmic walk
            humanoid->walkCycle += horizSpeed * m_deltaTime * walkAnimSpeed; 
            humanoid->breatheCycle += m_deltaTime * 0.8f; 
            
            // --- CALC TARGETS ---
            float targetLegAngle = 0;
            float targetArmAngle = 0;
            float targetTorsoTilt = 0; // Forward tilt
            float targetTorsoRoll = 0; // Side sway
            float targetBobY = 0;
            
            if (isJumping || isFalling) {
                targetLegAngle = -10.0f; 
                targetArmAngle = 170.0f; 
                targetTorsoTilt = -5.0f; 
            }
            else if (isMoving) {
                // WALK CYCLE
                float walkRange = 32.0f;
                targetLegAngle = sin(humanoid->walkCycle) * walkRange;
                targetArmAngle = sin(humanoid->walkCycle) * walkRange;
                
                // Torso Sway (Sideways roll)
                targetTorsoRoll = sin(humanoid->walkCycle) * 4.0f;
                
                // Forward Lean (Velocity dependent)
                targetTorsoTilt = (horizSpeed / humanoid->getWalkSpeed()) * 8.0f;
                
                // Vertical Bob (Classic quique)
                targetBobY = fabs(sin(humanoid->walkCycle)) * 0.25f;
            }
            else {
                // IDLE
                targetArmAngle = sin(humanoid->breatheCycle) * 3.5f; 
                targetBobY = sin(humanoid->breatheCycle) * 0.05f;
                targetTorsoRoll = sin(humanoid->breatheCycle * 0.5f) * 1.5f; // Side idle sway
            }
            
            // --- LERP STATES (Higher weight = smoother) ---
            auto lerp = [](float a, float b, float t) { return a + (b - a) * t; };
            humanoid->currentLegAngle = lerp(humanoid->currentLegAngle, targetLegAngle, 0.15f);
            humanoid->currentArmAngle = lerp(humanoid->currentArmAngle, targetArmAngle, 0.15f);
            humanoid->currentTorsoTilt = lerp(humanoid->currentTorsoTilt, targetTorsoTilt, 0.1f);
            humanoid->currentTorsoRoll = lerp(humanoid->currentTorsoRoll, targetTorsoRoll, 0.1f);
            humanoid->currentBobY = lerp(humanoid->currentBobY, targetBobY, 0.2f);
            
            // Rotations
            Vector3 visRotNoTilt = {0, humanoid->currentYaw, 0};
            // Note: R6 Torso Tilt (X) and Roll (Z)
            Vector3 visRotTorso = {humanoid->currentTorsoTilt, humanoid->currentYaw, humanoid->currentTorsoRoll};
            
            // Helper to rotate vector by yaw
            auto rotateVec = [&](const Vector3& v) -> Vector3 {
                float r = humanoid->currentYaw * 3.14159f / 180.0f;
                return {
                    v.x * cos(r) + v.z * sin(r),
                    v.y,
                    -v.x * sin(r) + v.z * cos(r)
                };
            };
            
            Vector3 bobOffset = {0, humanoid->currentBobY, 0};
            
            // --- Update Parts ---
            
            // Torso center at rp.y + 1 (Height 4, covers -1 to 3)
            auto torso = std::dynamic_pointer_cast<BasePart>(character->findFirstChild("Torso"));
            if (torso) {
                torso->setPosition(rp + Vector3(0, 1.0f, 0) + bobOffset);
                torso->setRotation(visRotTorso);
            }
            
            // Head center at rp.y + 4 (Height 2, covers 3 to 5)
            auto head = std::dynamic_pointer_cast<BasePart>(character->findFirstChild("Head"));
            if (head) {
                head->setPosition(rp + Vector3(0, 4.0f, 0) + bobOffset);
                head->setRotation(visRotNoTilt); // Head stays level (mostly)
            }
            
            // Limbs Pivot Helper
            auto setLimb = [&](const std::string& name, Vector3 hipOffset, float angle, bool isArm) {
                (void)isArm; // Currently used implicitly for bobbing (now both do)
                auto part = std::dynamic_pointer_cast<BasePart>(character->findFirstChild(name));
                if (part) {
                    Vector3 limbCenterOffset = {0, -2.0f, 0}; // Arm/Leg is 4 long, center is -2 from pivot
                    float rad = angle * 3.14159f / 180.0f;
                    
                    // Pitch Pivot Math
                    Vector3 rotatedCenter = {
                        0,
                        limbCenterOffset.y * cos(rad) - limbCenterOffset.z * sin(rad),
                        limbCenterOffset.y * sin(rad) + limbCenterOffset.z * cos(rad)
                    };
                    
                    Vector3 posOffset = hipOffset + rotatedCenter + bobOffset;
                    part->setPosition(rp + rotateVec(posOffset));
                    part->setRotation({angle, humanoid->currentYaw, 0});
                }
            };
            
            // Right Arm
            setLimb("RightArm", {3.0f, 2.5f, 0}, isJumping ? 160.0f : humanoid->currentArmAngle, true);
        }
    }

    static void syncRemoteVisuals(std::shared_ptr<Model> character, const Vector3& rp, float m_deltaTime) {
        if (!character) return;
        auto humanoid = std::dynamic_pointer_cast<Humanoid>(character->findFirstChild("Humanoid"));
        if (!humanoid) return;

        // Force-anchor all parts of remote character to prevent physics desync
        for (auto& child : character->getChildren()) {
            if (auto bp = std::dynamic_pointer_cast<BasePart>(child)) {
                bp->setAnchored(true);
                bp->setCanCollide(false); // Remote players shouldn't push the client locally
            }
        }

        // Just update animations cycles
        float dummySpeed = 16.0f;
        humanoid->walkCycle += dummySpeed * m_deltaTime * 0.55f;
        humanoid->breatheCycle += m_deltaTime * 0.8f;

        // Simplify for remote: Just stay in a neutral/moving lerp
        auto lerp = [](float a, float b, float t) { return a + (b - a) * t; };
        humanoid->currentLegAngle = lerp(humanoid->currentLegAngle, sin(humanoid->walkCycle) * 32.0f, 0.2f);
        humanoid->currentArmAngle = lerp(humanoid->currentArmAngle, sin(humanoid->walkCycle) * 32.0f, 0.2f);
        
        Vector3 visRotNoTilt = {0, humanoid->currentYaw, 0};
        Vector3 bobOffset = {0, sin(humanoid->walkCycle) * 0.2f, 0};

        auto movePart = [&](const std::string& name, Vector3 offset, Vector3 rot = {0,0,0}) {
            auto part = std::dynamic_pointer_cast<BasePart>(character->findFirstChild(name));
            if (part) {
                part->setPosition(rp + offset + bobOffset);
                part->setRotation(rot);
            }
        };

        movePart("Torso", {0, 1.0f, 0}, {0, humanoid->currentYaw, 0});
        movePart("Head", {0, 4.0f, 0}, visRotNoTilt);

        auto setLimb = [&](const std::string& name, Vector3 hipOffset, float angle) {
            auto part = std::dynamic_pointer_cast<BasePart>(character->findFirstChild(name));
            if (part) {
                float rad = angle * 3.14159f / 180.0f;
                Vector3 limbCenterOffset = {0, -2.0f, 0};
                Vector3 rotatedCenter = { 0, limbCenterOffset.y * cos(rad), limbCenterOffset.y * sin(rad) };
                part->setPosition(rp + hipOffset + rotatedCenter + bobOffset);
                part->setRotation({angle, humanoid->currentYaw, 0});
            }
        };

        setLimb("LeftLeg", {-1.0f, -1.2f, 0}, humanoid->currentLegAngle);
        setLimb("RightLeg", {1.0f, -1.2f, 0}, -humanoid->currentLegAngle);
        setLimb("LeftArm", {-3.0f, 2.5f, 0}, -humanoid->currentArmAngle);
        setLimb("RightArm", {3.0f, 2.5f, 0}, humanoid->currentArmAngle);
        
        // Also move the root part to the actual net position
        auto root = std::dynamic_pointer_cast<BasePart>(character->getPrimaryPart());
        if (root) root->setPosition(rp);
    }
};

} // namespace CatCube

#pragma once

#include "Instance.hpp"
#include "Part.hpp"
#include <btBulletDynamicsCommon.h>
#include <vector>
#include <memory>
#include <unordered_map>

namespace CatCube {

class PhysicsService {
public:
    PhysicsService();
    ~PhysicsService();
    
    void init();
    void update(float deltaTime);
    void shutdown();
    
    // Manage physics objects
    void addPart(std::shared_ptr<BasePart> part);
    void removePart(std::shared_ptr<BasePart> part);
    
    // Character Physics Control
    void setPartVelocity(std::shared_ptr<BasePart> part, const Vector3& velocity);
    Vector3 getPartVelocity(std::shared_ptr<BasePart> part);
    void setPartAngularFactor(std::shared_ptr<BasePart> part, const Vector3& factor);
    void setPartFriction(std::shared_ptr<BasePart> part, float friction);
    
    btDiscreteDynamicsWorld* getWorld() { return m_dynamicsWorld; }

private:
    btDefaultCollisionConfiguration* m_collisionConfiguration = nullptr;
    btCollisionDispatcher* m_dispatcher = nullptr;
    btBroadphaseInterface* m_broadphase = nullptr;
    btSequentialImpulseConstraintSolver* m_solver = nullptr;
    btDiscreteDynamicsWorld* m_dynamicsWorld = nullptr;
    
    struct RigidBodyData {
        btRigidBody* body;
        btCollisionShape* shape;
        btMotionState* motionState;
    };
    
    std::unordered_map<InstancePtr, RigidBodyData> m_bodies;
};

} // namespace CatCube

#include "PhysicsService.hpp"
#include <iostream>

namespace CatCube {

// Custom MotionState to sync Bullet with CatCube Parts
class PartMotionState : public btMotionState {
public:
    PartMotionState(std::shared_ptr<BasePart> part) : m_part(part) {
        // Init transform from part
        const auto& pos = part->getPosition();
        // Ignoring rotation for initial setup for simplicity, but should be included
        m_transform.setIdentity();
        m_transform.setOrigin(btVector3(pos.x, pos.y, pos.z));
    }

    void getWorldTransform(btTransform& worldTrans) const override {
        worldTrans = m_transform;
    }

    void setWorldTransform(const btTransform& worldTrans) override {
        if (auto part = m_part.lock()) {
            btVector3 pos = worldTrans.getOrigin();
            part->setPosition({(float)pos.x(), (float)pos.y(), (float)pos.z()});
            
            // Sync rotation
            btQuaternion rot = worldTrans.getRotation();
            btScalar yaw, pitch, roll;
            btMatrix3x3(rot).getEulerYPR(yaw, pitch, roll);
            
            // Convert back to degrees for CatCube (this is approximate, euler angles are tricky)
            Vector3 euler;
            euler.y = -yaw * 180.0f / 3.14159f; // Bullet uses different convention
            euler.x = -pitch * 180.0f / 3.14159f;
            euler.z = -roll * 180.0f / 3.14159f;
            part->setRotation(euler); 
        }
        m_transform = worldTrans;
    }

    btTransform m_transform;
    std::weak_ptr<BasePart> m_part;
};

PhysicsService::PhysicsService() {}

PhysicsService::~PhysicsService() {
    shutdown();
}

void PhysicsService::init() {
    m_collisionConfiguration = new btDefaultCollisionConfiguration();
    m_dispatcher = new btCollisionDispatcher(m_collisionConfiguration);
    m_broadphase = new btDbvtBroadphase();
    m_solver = new btSequentialImpulseConstraintSolver();
    m_dynamicsWorld = new btDiscreteDynamicsWorld(m_dispatcher, m_broadphase, m_solver, m_collisionConfiguration);
    
    // Roblox gravity (196.2 studs/s^2)
    m_dynamicsWorld->setGravity(btVector3(0, -196.2, 0));
    
    std::cout << "PhysicsService initialized (Bullet 3)" << std::endl;
}

void PhysicsService::update(float deltaTime) {
    if (m_dynamicsWorld) {
        // Step simulation at 60Hz
        m_dynamicsWorld->stepSimulation(deltaTime, 10);
    }
}

void PhysicsService::addPart(std::shared_ptr<BasePart> part) {
    if (!m_dynamicsWorld || !part) return;
    if (m_bodies.count(part)) return; // Already exists
    
    // Respect CanCollide - if false, don't add to physics world (purely visual)
    if (!part->canCollide()) return;
    
    // Create shape (Box)
    const auto& size = part->getSize();
    btCollisionShape* shape = new btBoxShape(btVector3(size.x/2.0f, size.y/2.0f, size.z/2.0f));
    
    // Initial transform
    btTransform startTransform;
    startTransform.setIdentity();
    const auto& pos = part->getPosition();
    startTransform.setOrigin(btVector3(pos.x, pos.y, pos.z));
    
    const auto& rot = part->getRotation();
    btQuaternion q;
    q.setEuler(rot.y * 3.14159f / 180.0f, rot.x * 3.14159f / 180.0f, rot.z * 3.14159f / 180.0f);
    startTransform.setRotation(q);
    
    // Calculate mass (0 if anchored, otherwise based on size)
    btScalar mass = 0.0f;
    if (!part->isAnchored()) {
        mass = size.x * size.y * size.z * 0.7f; // Density approximation
    }
    
    btVector3 localInertia(0, 0, 0);
    if (mass != 0.0f) {
        shape->calculateLocalInertia(mass, localInertia);
    }
    
    // Create body
    PartMotionState* motionState = new PartMotionState(part);
    btRigidBody::btRigidBodyConstructionInfo rbInfo(mass, motionState, shape, localInertia);
    rbInfo.m_restitution = 0.3f; // Bounciness
    rbInfo.m_friction = 0.5f;
    
    btRigidBody* body = new btRigidBody(rbInfo);
    
    m_dynamicsWorld->addRigidBody(body);
    
    // Store data
    RigidBodyData data;
    data.body = body;
    data.shape = shape;
    data.motionState = motionState;
    m_bodies[part] = data;
}

void PhysicsService::removePart(std::shared_ptr<BasePart> part) {
    auto it = m_bodies.find(part);
    if (it != m_bodies.end()) {
        RigidBodyData& data = it->second;
        m_dynamicsWorld->removeRigidBody(data.body);
        delete data.body->getMotionState();
        delete data.body;
        delete data.shape;
        m_bodies.erase(it);
    }
}

void PhysicsService::setPartVelocity(std::shared_ptr<BasePart> part, const Vector3& velocity) {
    auto it = m_bodies.find(part);
    if (it != m_bodies.end()) {
        btRigidBody* body = it->second.body;
        body->activate(true); // Wake up if sleeping
        body->setLinearVelocity(btVector3(velocity.x, velocity.y, velocity.z));
    }
}

Vector3 PhysicsService::getPartVelocity(std::shared_ptr<BasePart> part) {
    auto it = m_bodies.find(part);
    if (it != m_bodies.end()) {
        const btVector3& vel = it->second.body->getLinearVelocity();
        return { (float)vel.x(), (float)vel.y(), (float)vel.z() };
    }
    return {0, 0, 0};
}

void PhysicsService::setPartAngularFactor(std::shared_ptr<BasePart> part, const Vector3& factor) {
    auto it = m_bodies.find(part);
    if (it != m_bodies.end()) {
        it->second.body->setAngularFactor(btVector3(factor.x, factor.y, factor.z));
    }
}

void PhysicsService::setPartFriction(std::shared_ptr<BasePart> part, float friction) {
    auto it = m_bodies.find(part);
    if (it != m_bodies.end()) {
        it->second.body->setFriction(friction);
    }
}

void PhysicsService::shutdown() {
    // Clean up bodies
    for (auto& pair : m_bodies) {
        RigidBodyData& data = pair.second;
        m_dynamicsWorld->removeRigidBody(data.body);
        delete data.body->getMotionState();
        delete data.body;
        delete data.shape;
    }
    m_bodies.clear();

    if (m_dynamicsWorld) { delete m_dynamicsWorld; m_dynamicsWorld = nullptr; }
    if (m_solver) { delete m_solver; m_solver = nullptr; }
    if (m_broadphase) { delete m_broadphase; m_broadphase = nullptr; }
    if (m_dispatcher) { delete m_dispatcher; m_dispatcher = nullptr; }
    if (m_collisionConfiguration) { delete m_collisionConfiguration; m_collisionConfiguration = nullptr; }
}

} // namespace CatCube

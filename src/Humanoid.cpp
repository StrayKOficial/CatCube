#include "Humanoid.hpp"
#include "Model.hpp"

namespace CatCube {

Humanoid::Humanoid() : Instance("Humanoid") {
    m_name = "Humanoid";
}

bool Humanoid::isA(const std::string& className) const {
    return className == "Humanoid" || Instance::isA(className);
}

void Humanoid::move(const Vector3& direction, bool jump) {
    m_moveDirection = direction;
    m_jump = jump;
    
    // Note: Actual physics application happens in PhysicsService or via logic that reads these properties
    // For now, we'll store them here, and PhysicsService will look for Humanoids to update
}

std::shared_ptr<BasePart> Humanoid::getRootPart() const {
    auto parent = getParent();
    if (!parent) return nullptr;
    
    // Try to find "HumanoidRootPart"
    auto root = parent->findFirstChild("HumanoidRootPart");
    if (root && root->isA("BasePart")) {
        return std::dynamic_pointer_cast<BasePart>(root);
    }
    
    // Fallback to Torso
    root = parent->findFirstChild("Torso");
    if (root && root->isA("BasePart")) {
        return std::dynamic_pointer_cast<BasePart>(root);
    }
    
    // Fallback to Model's PrimaryPart
    auto model = std::dynamic_pointer_cast<Model>(parent);
    if (model) {
        auto primary = model->getPrimaryPart();
        if (primary && primary->isA("BasePart")) {
            return std::dynamic_pointer_cast<BasePart>(primary);
        }
    }
    
    return nullptr;
}

} // namespace CatCube

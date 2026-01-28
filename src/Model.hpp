#pragma once

#include "Instance.hpp"

namespace CatCube {

class Model : public Instance {
public:
    static constexpr const char* ClassName = "Model";
    
    Model() : Instance("Model") {
        m_name = "Model";
    }
    
    bool isA(const std::string& className) const override {
        return className == "Model" || Instance::isA(className);
    }
    
    InstancePtr getPrimaryPart() const { return m_primaryPart.lock(); }
    void setPrimaryPart(InstancePtr part) { m_primaryPart = part; }
    
private:
    std::weak_ptr<Instance> m_primaryPart;
};

} // namespace CatCube

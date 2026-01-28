#include "Instance.hpp"
#include <algorithm>
#include <sstream>

namespace CatCube {

// ============== Instance ==============

Instance::Instance(const std::string& className)
    : m_className(className)
    , m_name(className)
{
}

Instance::~Instance() {
    clearChildren();
}

bool Instance::isA(const std::string& className) const {
    return m_className == className || className == "Instance";
}

InstancePtr Instance::getParent() const {
    return m_parent.lock();
}

void Instance::setParent(InstancePtr parent) {
    if (m_destroyed) return;
    
    auto oldParent = m_parent.lock();
    
    // Remove from old parent
    if (oldParent) {
        oldParent->removeChild(shared_from_this());
    }
    
    // Set new parent
    m_parent = parent;
    
    // Add to new parent's children
    if (parent) {
        parent->m_children.push_back(shared_from_this());
        parent->onChildAdded(shared_from_this());
    }
    
    onParentChanged(oldParent, parent);
}

InstancePtr Instance::findFirstChild(const std::string& name) const {
    for (const auto& child : m_children) {
        if (child->getName() == name) {
            return child;
        }
    }
    return nullptr;
}

InstancePtr Instance::findFirstChildOfClass(const std::string& className) const {
    for (const auto& child : m_children) {
        if (child->isA(className)) {
            return child;
        }
    }
    return nullptr;
}

std::vector<InstancePtr> Instance::getDescendants() const {
    std::vector<InstancePtr> descendants;
    
    for (const auto& child : m_children) {
        descendants.push_back(child);
        auto childDescendants = child->getDescendants();
        descendants.insert(descendants.end(), childDescendants.begin(), childDescendants.end());
    }
    
    return descendants;
}

void Instance::addChild(InstancePtr child) {
    if (!child || m_destroyed) return;
    child->setParent(shared_from_this());
}

void Instance::removeChild(InstancePtr child) {
    if (!child) return;
    
    auto it = std::find(m_children.begin(), m_children.end(), child);
    if (it != m_children.end()) {
        m_children.erase(it);
        onChildRemoved(child);
    }
}

void Instance::clearChildren() {
    // Make a copy since children will modify the vector
    auto childrenCopy = m_children;
    for (auto& child : childrenCopy) {
        child->destroy();
    }
    m_children.clear();
}

void Instance::destroy() {
    if (m_destroyed) return;
    m_destroyed = true;
    
    // Remove from parent
    auto parent = m_parent.lock();
    if (parent) {
        parent->removeChild(shared_from_this());
    }
    
    // Destroy all children
    clearChildren();
}

std::string Instance::getFullName() const {
    std::vector<std::string> path;
    
    auto current = shared_from_this();
    while (current) {
        path.push_back(current->getName());
        current = current->getParent();
    }
    
    std::stringstream ss;
    for (auto it = path.rbegin(); it != path.rend(); ++it) {
        if (it != path.rbegin()) ss << ".";
        ss << *it;
    }
    
    return ss.str();
}

InstancePtr Instance::clone() const {
    auto cloned = std::make_shared<Instance>(m_className);
    cloned->m_name = m_name;
    
    // Clone properties
    for (const auto& [name, prop] : m_properties) {
        cloned->m_properties[name] = prop; // Shallow copy for now
    }
    
    // Clone children
    for (const auto& child : m_children) {
        auto childClone = child->clone();
        childClone->setParent(cloned);
    }
    
    return cloned;
}

void Instance::onParentChanged(InstancePtr oldParent, InstancePtr newParent) {
    (void)oldParent;
    (void)newParent;
    // Override in subclasses
}

void Instance::onChildAdded(InstancePtr child) {
    (void)child;
    // Override in subclasses
}

void Instance::onChildRemoved(InstancePtr child) {
    (void)child;
    // Override in subclasses
}

// ============== InstanceFactory ==============

InstanceFactory& InstanceFactory::instance() {
    static InstanceFactory factory;
    return factory;
}

void InstanceFactory::registerClass(const std::string& className, Creator creator) {
    m_creators[className] = creator;
}

InstancePtr InstanceFactory::create(const std::string& className) {
    auto it = m_creators.find(className);
    if (it != m_creators.end()) {
        return it->second();
    }
    // Default: create base Instance
    return std::make_shared<Instance>(className);
}

bool InstanceFactory::isRegistered(const std::string& className) const {
    return m_creators.find(className) != m_creators.end();
}

} // namespace CatCube

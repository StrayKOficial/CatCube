#pragma once

#include <string>
#include <vector>
#include <memory>
#include <unordered_map>
#include <functional>
#include <any>
#include <typeindex>

namespace CatCube {

// Forward declarations
class Instance;
using InstancePtr = std::shared_ptr<Instance>;
using InstanceWeakPtr = std::weak_ptr<Instance>;

/**
 * Property - Holds a value with type info (like Roblox properties)
 */
class Property {
public:
    template<typename T>
    Property(const std::string& name, T value) 
        : m_name(name), m_value(value), m_type(typeid(T)) {}
    
    const std::string& getName() const { return m_name; }
    std::type_index getType() const { return m_type; }
    
    template<typename T>
    T get() const {
        return std::any_cast<T>(m_value);
    }
    
    template<typename T>
    void set(T value) {
        m_value = value;
    }

private:
    std::string m_name;
    std::any m_value;
    std::type_index m_type;
};

/**
 * Instance - Base class for all objects (like Roblox Instance)
 * 
 * Every object in CatCube inherits from Instance.
 * Supports parent/child hierarchy and properties.
 */
class Instance : public std::enable_shared_from_this<Instance> {
public:
    Instance(const std::string& className = "Instance");
    virtual ~Instance();

    // Class info
    const std::string& getClassName() const { return m_className; }
    virtual bool isA(const std::string& className) const;
    
    // Name
    const std::string& getName() const { return m_name; }
    void setName(const std::string& name) { m_name = name; }
    
    // Hierarchy
    InstancePtr getParent() const;
    void setParent(InstancePtr parent);
    const std::vector<InstancePtr>& getChildren() const { return m_children; }
    
    // Find children
    InstancePtr findFirstChild(const std::string& name) const;
    InstancePtr findFirstChildOfClass(const std::string& className) const;
    std::vector<InstancePtr> getDescendants() const;
    
    // Child management
    void addChild(InstancePtr child);
    void removeChild(InstancePtr child);
    void clearChildren();
    
    // Properties (Roblox-style)
    template<typename T>
    void setProperty(const std::string& name, T value) {
        m_properties[name] = std::make_shared<Property>(name, value);
    }
    
    template<typename T>
    T getProperty(const std::string& name) const {
        auto it = m_properties.find(name);
        if (it != m_properties.end()) {
            return it->second->get<T>();
        }
        return T{};
    }
    
    bool hasProperty(const std::string& name) const {
        return m_properties.find(name) != m_properties.end();
    }
    
    // Destruction
    void destroy();
    bool isDestroyed() const { return m_destroyed; }

    // Get full path (like Roblox)
    std::string getFullName() const;

    // Clone
    virtual InstancePtr clone() const;

protected:
    std::string m_className;
    std::string m_name;
    InstanceWeakPtr m_parent;
    std::vector<InstancePtr> m_children;
    std::unordered_map<std::string, std::shared_ptr<Property>> m_properties;
    bool m_destroyed = false;

    // Called when parent changes
    virtual void onParentChanged(InstancePtr oldParent, InstancePtr newParent);
    // Called when a child is added
    virtual void onChildAdded(InstancePtr child);
    // Called when a child is removed
    virtual void onChildRemoved(InstancePtr child);
};

/**
 * Instance Factory - Creates instances by class name
 */
class InstanceFactory {
public:
    using Creator = std::function<InstancePtr()>;
    
    static InstanceFactory& instance();
    
    void registerClass(const std::string& className, Creator creator);
    InstancePtr create(const std::string& className);
    bool isRegistered(const std::string& className) const;

private:
    InstanceFactory() = default;
    std::unordered_map<std::string, Creator> m_creators;
};

// Macro to register classes
#define REGISTER_INSTANCE(className) \
    static bool _registered_##className = []() { \
        InstanceFactory::instance().registerClass(#className, []() { \
            return std::make_shared<className>(); \
        }); \
        return true; \
    }()

} // namespace CatCube

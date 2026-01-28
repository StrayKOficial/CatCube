#pragma once

#include "Instance.hpp"
#include <iostream>

namespace CatCube {

/**
 * DataModel - Root of the game hierarchy (like game in Roblox)
 * 
 * Contains all services like Workspace, Players, etc.
 */
class DataModel : public Instance {
public:
    DataModel() : Instance("DataModel") {
        m_name = "Game";
    }
    
    bool isA(const std::string& className) const override {
        return className == "DataModel" || Instance::isA(className);
    }

    // Get or create a service
    template<typename T>
    std::shared_ptr<T> getService() {
        std::string serviceName = T::ClassName;
        auto service = findFirstChildOfClass(serviceName);
        if (!service) {
            service = std::make_shared<T>();
            service->setParent(shared_from_this());
        }
        return std::dynamic_pointer_cast<T>(service);
    }
};

/**
 * Workspace - Contains all 3D objects (like Workspace in Roblox)
 */
class Workspace : public Instance {
public:
    static constexpr const char* ClassName = "Workspace";
    
    Workspace() : Instance("Workspace") {
        m_name = "Workspace";
    }
    
    bool isA(const std::string& className) const override {
        return className == "Workspace" || Instance::isA(className);
    }
};

/**
 * Lighting - Contains lighting settings
 */
class Lighting : public Instance {
public:
    static constexpr const char* ClassName = "Lighting";
    
    Lighting() : Instance("Lighting") {
        m_name = "Lighting";
        // Default Roblox 2009 lighting
        setProperty("Ambient", 0.5f);
        setProperty("Brightness", 1.0f);
        setProperty("TimeOfDay", 14.0f); // 2 PM
    }
    
    bool isA(const std::string& className) const override {
        return className == "Lighting" || Instance::isA(className);
    }
};

/**
 * Players - Contains player instances
 */
class Players : public Instance {
public:
    static constexpr const char* ClassName = "Players";
    
    Players() : Instance("Players") {
        m_name = "Players";
    }
    
    bool isA(const std::string& className) const override {
        return className == "Players" || Instance::isA(className);
    }
};

/**
 * ReplicatedStorage - Shared storage for scripts
 */
class ReplicatedStorage : public Instance {
public:
    static constexpr const char* ClassName = "ReplicatedStorage";
    
    ReplicatedStorage() : Instance("ReplicatedStorage") {
        m_name = "ReplicatedStorage";
    }
    
    bool isA(const std::string& className) const override {
        return className == "ReplicatedStorage" || Instance::isA(className);
    }
};

/**
 * StarterPack - Default items for players
 */
class StarterPack : public Instance {
public:
    static constexpr const char* ClassName = "StarterPack";
    
    StarterPack() : Instance("StarterPack") {
        m_name = "StarterPack";
    }
    
    bool isA(const std::string& className) const override {
        return className == "StarterPack" || Instance::isA(className);
    }
};

} // namespace CatCube

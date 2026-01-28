#include "Part.hpp"

namespace CatCube {

// ============== BasePart ==============

BasePart::BasePart(const std::string& className) 
    : Instance(className) 
{
}

bool BasePart::isA(const std::string& className) const {
    return className == "BasePart" || Instance::isA(className);
}

InstancePtr BasePart::clone() const {
    auto cloned = std::make_shared<BasePart>(m_className);
    cloned->m_name = m_name;
    cloned->m_position = m_position;
    cloned->m_size = m_size;
    cloned->m_rotation = m_rotation;
    cloned->m_color = m_color;
    cloned->m_anchored = m_anchored;
    cloned->m_canCollide = m_canCollide;
    cloned->m_transparency = m_transparency;
    return cloned;
}

// ============== Part ==============

Part::Part() : BasePart("Part") {
    m_name = "Part";
    m_size = {4, 1.2f, 2};  // Default Roblox part size
    m_color = Color3::Gray();
}

bool Part::isA(const std::string& className) const {
    return className == "Part" || BasePart::isA(className);
}

InstancePtr Part::clone() const {
    auto cloned = std::make_shared<Part>();
    cloned->m_name = m_name;
    cloned->m_position = m_position;
    cloned->m_size = m_size;
    cloned->m_rotation = m_rotation;
    cloned->m_color = m_color;
    cloned->m_anchored = m_anchored;
    cloned->m_canCollide = m_canCollide;
    cloned->m_transparency = m_transparency;
    return cloned;
}

// ============== SpawnLocation ==============

SpawnLocation::SpawnLocation() : BasePart("SpawnLocation") {
    m_name = "SpawnLocation";
    m_size = {6, 1, 6};
    m_color = Color3::DarkGray();
    m_anchored = true;
}

bool SpawnLocation::isA(const std::string& className) const {
    return className == "SpawnLocation" || BasePart::isA(className);
}

} // namespace CatCube

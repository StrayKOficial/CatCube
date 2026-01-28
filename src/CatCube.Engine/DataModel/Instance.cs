using System.Numerics;
using MoonSharp.Interpreter;

namespace CatCube.Engine;

/// <summary>
/// The base class for all objects in the DataModel hierarchy.
/// </summary>
[MoonSharpUserData]
public class Instance
{
    public string Name { get; set; } = "Instance";
    public string ClassName => GetType().Name;
    
    private Instance? _parent;
    private readonly List<Instance> _children = new List<Instance>();
    
    public Instance? Parent
    {
        get => _parent;
        set
        {
            if (_parent == value) return;
            
            // Remove from old parent
            if (_parent != null)
            {
                _parent._children.Remove(this);
                OnParentChanged();
            }
            
            _parent = value;
            
            // Add to new parent
            if (_parent != null)
            {
                _parent._children.Add(this);
                OnParentChanged();
            }
        }
    }
    
    protected virtual void OnParentChanged() { }
    
    public bool IsDescendantOf(Instance ancestor)
    {
        var current = Parent;
        while (current != null)
        {
            if (current == ancestor) return true;
            current = current.Parent;
        }
        return false;
    }
    
    public Instance()
    {
    }

    public Instance[] GetChildren()
    {
        return _children.ToArray();
    }
    
    public Instance? FindFirstChild(string name)
    {
        foreach(var child in _children)
        {
            if (child.Name == name)
                return child;
        }
        return null;
    }
    
    public void Destroy()
    {
        Parent = null; // Detach from hierarchy
        foreach(var child in _children.ToArray()) // Copy to avoid modification while iterating
        {
            child.Destroy();
        }
    }
    
    // Factory method for Lua: Instance.new("Part")
    public static Instance? New(string className)
    {
        // Simple reflection-less factory for now (expand later)
        switch (className)
        {
            case "Instance": return new Instance();
            case "Part": return new Part();
            case "SpawnLocation": return new SpawnLocation();
            case "Model": return new Model();
            case "Folder": return new Folder();
            case "Frame": return new Frame();
            default: return null;
        }
    }
    
    public override string ToString()
    {
        return Name;
    }
}

// Basic specialized instances

[MoonSharpUserData]
public class Folder : Instance { }

[MoonSharpUserData]
public class Model : Instance { }



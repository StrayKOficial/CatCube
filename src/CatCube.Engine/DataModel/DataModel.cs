using MoonSharp.Interpreter;

namespace CatCube.Engine;

[MoonSharpUserData]
public class DataModel : Instance
{
    // Singleton-like access for convenience in engine code,
    // though in a real engine we might have multiple DataModels (Client/Server)
    public static DataModel Current { get; private set; } = new DataModel();

    public Workspace Workspace { get; private set; }
    public Lighting Lighting { get; private set; }
    public Instance CoreGui { get; private set; } // Container for 2D items

    public DataModel()
    {
        Name = "Game";
        
        // Initialize default services
        Workspace = new Workspace();
        Workspace.Parent = this;
        
        Lighting = new Lighting();
        Lighting.Parent = this;

        CoreGui = new Instance() { Name = "CoreGui" };
        CoreGui.Parent = this;
    }
    
    // Heartbeat/Update event exposed to Lua via simple callback list
    // Events in MoonSharp can be tricky, using method pattern is safer
    private List<Closure> _updateCallbacks = new List<Closure>();
    
    public void BindToUpdate(Closure callback)
    {
        _updateCallbacks.Add(callback);
    }
    
    public void FireUpdate(double dt)
    {
        // Invoke all Lua callbacks
        foreach (var callback in _updateCallbacks)
        {
            try
            {
                callback.Call(dt);
            }
            catch (Exception e)
            {
                Console.WriteLine($"[Lua Update Error] {e.Message}");
            }
        }
    }
    
    // GetService("Workspace") etc
    public Instance? GetService(string className)
    {
        return FindFirstChild(className); // Simplified
    }
}

[MoonSharpUserData]
public class Workspace : Instance
{
    public Physics.PhysicsSpace Physics { get; private set; }

    public Workspace()
    {
        Name = "Workspace";
        Physics = new Physics.PhysicsSpace();
    }
    
    public void StepPhysics(float dt)
    {
        // Clamp dt
        float physicsDt = Math.Min(dt, 0.033f);
        Physics.Update(physicsDt);
        
        // Process Contact Events
        while (Physics.ContactEvents.TryDequeue(out var contact))
        {
            object? objA = null;
            object? objB = null;
            
            // Resolve A
            if (contact.bodyA.HasValue) Physics.BodyToPart.TryGetValue(contact.bodyA.Value, out objA);
            else if (contact.staticA.HasValue) Physics.StaticToPart.TryGetValue(contact.staticA.Value, out objA);
            
            // Resolve B
            if (contact.bodyB.HasValue) Physics.BodyToPart.TryGetValue(contact.bodyB.Value, out objB);
            else if (contact.staticB.HasValue) Physics.StaticToPart.TryGetValue(contact.staticB.Value, out objB);
            
            // Trigger Events if both are Parts
            if (objA is Part pA && objB is Part pB)
            {
                pA.OnTouched(pB);
                pB.OnTouched(pA);
            }
        }
    }
}

[MoonSharpUserData]
public class Lighting : Instance
{
    public Lighting()
    {
        Name = "Lighting";
    }
}

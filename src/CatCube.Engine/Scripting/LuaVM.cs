using MoonSharp.Interpreter;

namespace CatCube.Engine.Scripting;

public class LuaVM
{
    private Script _script;
    
    public LuaVM()
    {
        // Register types
        UserData.RegisterType<Instance>();
        UserData.RegisterType<Part>();
        UserData.RegisterType<SpawnLocation>();
        UserData.RegisterType<Folder>();
        UserData.RegisterType<Model>();
        UserData.RegisterType<Workspace>();
        UserData.RegisterType<DataModel>();
        UserData.RegisterType<Lighting>();
        UserData.RegisterType<DataModel>();
        UserData.RegisterType<Lighting>();
        UserData.RegisterType<Frame>();
        UserData.RegisterType<System.Numerics.Vector3>(); 
        UserData.RegisterType<System.Numerics.Vector2>(); 
        
        // Ensure delegates work
        Script.GlobalOptions.CustomConverters.SetScriptToClrCustomConversion(DataType.Function, typeof(Action<Part>), v =>
        {
            var function = v.Function;
            return (Action<Part>)(p => function.Call(p));
        });
        
        Script.GlobalOptions.CustomConverters.SetScriptToClrCustomConversion(DataType.Function, typeof(Action<double>), v =>
        {
            var function = v.Function;
            return (Action<double>)(d => function.Call(d));
        });
        
        _script = new Script();
        
        // Expose 'Instance' static class for Instance.new
        _script.Globals["Instance"] = typeof(Instance);
        
        // Expose 'game' and 'workspace'
        _script.Globals["game"] = DataModel.Current;
        _script.Globals["workspace"] = DataModel.Current.Workspace;
        
        // Expose Vector3 constructor wrapper ("Vector3.new(x,y,z)")
        UserData.RegisterType<Vector3Proxy>();
        _script.Globals["Vector3"] = typeof(Vector3Proxy);
        
        // Vector2
        UserData.RegisterType<Vector2Proxy>();
        _script.Globals["Vector2"] = typeof(Vector2Proxy);
        
        // Standard libraries
        // _script.Options.DebugPrint = s => Console.WriteLine($"[LUA] {s}");
    }
    
    [MoonSharpUserData]
    public class Vector3Proxy
    {
        public static System.Numerics.Vector3 @new(float x, float y, float z) => new System.Numerics.Vector3(x, y, z);
        public static System.Numerics.Vector3 zero => System.Numerics.Vector3.Zero;
        public static System.Numerics.Vector3 one => System.Numerics.Vector3.One;
    }

    [MoonSharpUserData]
    public class Vector2Proxy
    {
        public static System.Numerics.Vector2 @new(float x, float y) => new System.Numerics.Vector2(x, y);
        public static System.Numerics.Vector2 zero => System.Numerics.Vector2.Zero;
        public static System.Numerics.Vector2 one => System.Numerics.Vector2.One;
    }
    
    public void Execute(string code)
    {
        try
        {
            _script.DoString(code);
        }
        catch (SyntaxErrorException ex)
        {
            Console.WriteLine($"[Lua Syntax Error] {ex.DecoratedMessage}");
        }
        catch (ScriptRuntimeException ex)
        {
            Console.WriteLine($"[Lua Runtime Error] {ex.DecoratedMessage}");
        }
    }

    public void ExecuteFile(string path)
    {
        try
        {
            if (!System.IO.File.Exists(path))
            {
                Console.WriteLine($"[Lua Error] File not found: {path}");
                return;
            }
            
            string code = System.IO.File.ReadAllText(path);
            _script.DoString(code, null, path);
        }
        catch (SyntaxErrorException ex)
        {
            Console.WriteLine($"[Lua Syntax Error in {path}] {ex.DecoratedMessage}");
        }
        catch (ScriptRuntimeException ex)
        {
            Console.WriteLine($"[Lua Runtime Error in {path}] {ex.DecoratedMessage}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Lua Error] {ex.Message}");
        }
    }
}

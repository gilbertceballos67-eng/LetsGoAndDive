using System;
using System.Runtime.Loader;

public class CustomAssemblyLoadContext : AssemblyLoadContext
{
    public CustomAssemblyLoadContext() : base(true) { }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        return IntPtr.Zero;
    }
}

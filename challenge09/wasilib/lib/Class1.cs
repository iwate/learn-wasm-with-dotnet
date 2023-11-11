using System.Runtime.InteropServices;

namespace lib;

public static class Class1
{
    [UnmanagedCallersOnly(EntryPoint = "MyAdd")]
    public static int MyAdd(int a, int b) 
    {
        return a + b;
    }

    [UnmanagedCallersOnly(EntryPoint = "HelloWorld")]
    public static IntPtr HelloWorld() 
    {
        var ptr = Marshal.AllocHGlobal(13);
        Marshal.WriteByte(ptr, 0x48);
        Marshal.WriteByte(ptr+1, 0x65);
        Marshal.WriteByte(ptr+2, 0x6c);
        Marshal.WriteByte(ptr+3, 0x6c);
        Marshal.WriteByte(ptr+4, 0x6f);
        Marshal.WriteByte(ptr+5, 0x2c);
        Marshal.WriteByte(ptr+6, 0x57);
        Marshal.WriteByte(ptr+7, 0x6f);
        Marshal.WriteByte(ptr+8, 0x72);
        Marshal.WriteByte(ptr+9, 0x6c);
        Marshal.WriteByte(ptr+10, 0x64);
        Marshal.WriteByte(ptr+11, 0x21);
        Marshal.WriteByte(ptr+12, 0x00);
        return ptr;
    }
}
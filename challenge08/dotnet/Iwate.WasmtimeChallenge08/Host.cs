using System;
using System.Threading.Tasks;

public class Host
{
    public async Task<int> HostMethod1(int intParam, float floatParam, string stringParam, ObjParam objParam)
    {
        await Task.Delay(1);
        Console.WriteLine("Invoke HostMethod1");
        Console.WriteLine($"intParam = {intParam}");
        Console.WriteLine($"floatParam = {floatParam}");
        Console.WriteLine($"stringParam = {stringParam}");
        Console.WriteLine($"objParam = {objParam?.Property}");
        return -1;
    }
}

public class ObjParam
{
    public string Property { get; set; }
}
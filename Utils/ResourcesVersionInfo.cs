using System.IO;
using Newtonsoft.Json;

namespace MaaBATapAssistant.Utils;

public class ResourcesVersionInfo
{
    public string ResourcesVersion { get; set; } = string.Empty;

    public static string GetResourcesVersion()
    {
        try
        {
            string json = File.ReadAllText(MyConstant.ResourcesVersionJsonPath);
            ResourcesVersionInfo? resourcesVersionInfo = JsonConvert.DeserializeObject<ResourcesVersionInfo>(json);
            if (resourcesVersionInfo != null)
            {
                return resourcesVersionInfo.ResourcesVersion;
            }
        }
        catch
        {
            Utility.MyDebugWriteLine("无法读取ResourcesVersionInfo");
        }
        return "0.0.0.0";
    }
}

namespace GattClientNative;

using Java.Util;
public class Util
{
    public static UUID? FromGuid(Guid guid)
    {
        return UUID.FromString(guid.ToString());
    }
}
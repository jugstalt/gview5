namespace gView.CopyFeatureclass.Lib;
class ReplicationInformation
{
    public static string Replace(string? str)
    {
        if (str == null)
        {
            return string.Empty;
        }

        return str.Replace("[MACHINENAME]", MachineName).Replace("[USER]", UserName);
    }
    public static string MachineName
    {
        get { return System.Environment.MachineName; }
    }
    public static string UserName
    {
        get { return System.Environment.UserName; }
    }
}

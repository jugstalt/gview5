using System;

namespace gView.Blazor.Core;

public class KnownExplorerObjectIDs
{
    private static Guid _Any = new global::System.Guid("00000000-0000-0000-0000-000000000000");
    private static Guid _Directory = new global::System.Guid("458E62A0-4A93-45cf-B14D-2F958D67E522");
    private static Guid _Drive = new global::System.Guid("CB2915F4-DB1A-461a-A14E-73F3A259F0BA");

    public static Guid Any { get { return _Any; } }
    public static Guid Directory { get { return _Directory; } }
    public static Guid Drive { get { return _Drive; } }
}

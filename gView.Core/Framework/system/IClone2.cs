﻿/// <summary>
/// The <c>gView.Framework</c> provides all interfaces to develope
/// with and for gView
/// </summary>
namespace gView.Framework.system
{
    public interface IClone2
    {
        object Clone(CloneOptions options);
        void Release();
    }
}

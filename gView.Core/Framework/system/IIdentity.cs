﻿using System.Collections.Generic;

/// <summary>
/// The <c>gView.Framework</c> provides all interfaces to develope
/// with and for gView
/// </summary>
namespace gView.Framework.system
{
    public interface IIdentity
    {
        string UserName { get; }
        List<string> UserRoles { get; }
        //string HashedPassword { get; }
        bool IsAdministrator { get; }

        bool IsAnonymous { get; }
    }
}

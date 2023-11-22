using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.system;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace gView.DataExplorer.Core.Extensions;

static public class ExplorerObjectExtensions
{
    static public IEnumerable<IExplorerObject> GetAncestors(this IExplorerObject exObject, bool includeSelf)
    {
        List<IExplorerObject> ancestors = new List<IExplorerObject>();

        if (includeSelf)
        {
            ancestors.Add(exObject);
        }

        while (exObject.ParentExplorerObject != null)
        {
            ancestors.Insert(0, exObject.ParentExplorerObject);
            exObject = exObject.ParentExplorerObject;
        }

        return ancestors;
    }

    async static public Task<bool> SecureRefresh(this IExplorerObject exObject)
    {
        if (exObject is IExplorerParentObject parentExObject)
        {
            using (var mutex = await FuzzyMutexAsync.LockAsync(exObject.GetHashCode().ToString()))
            {
                if (parentExObject.RequireRefresh() == false)
                {
                    return true;
                }

                if (mutex.WasBlocked == false)
                {
                    try
                    {
                        await parentExObject.Refresh();
                    }
                    catch (Exception ex)
                    {
                        if (!parentExObject.HandleRefreshException(ex))
                        {
                            throw;
                        }
                    }
                }
            }

            return true;
        }

        return false;
    }
}

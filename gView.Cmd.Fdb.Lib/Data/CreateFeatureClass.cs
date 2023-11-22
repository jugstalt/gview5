using gView.DataSources.Fdb.MSAccess;
using gView.Framework.Data;
using gView.Framework.Geometry;
using System;
using System.Threading.Tasks;

namespace gView.Cmd.Fdb.Lib.Data;
internal class CreateFeatureClass
{
    public CreateFeatureClass()
    {

    }

    async public Task<bool> Create(IFeatureDataset featureDataset,
                             string fcName,
                             IGeometryDef gDef,
                             IFieldCollection fields,
                             BinaryTreeDef binaryTreeDef)
    {
        var fdb = featureDataset.Database as AccessFDB;

        if (fdb == null)
        {
            throw new Exception("Database is not a gView Feature Database");
        }

        int fcId = await fdb.CreateFeatureClass(
            featureDataset.DatasetName,
            fcName,
            gDef,
            fields);

        if (fcId < 0)
        {
            throw new Exception(fdb.LastErrorMessage);
        }

        if(await fdb.SetSpatialIndexBounds(fcName, "BinaryTree2", binaryTreeDef.Bounds, 0.55, binaryTreeDef.MaxPerNode, binaryTreeDef.MaxLevel) == false)
        {
            await fdb.DeleteFeatureClass(fcName);

            throw new Exception(fdb.LastErrorMessage);
        }

        return true;
    }
}

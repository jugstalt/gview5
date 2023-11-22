using gView.Framework.Geometry;
using gView.Framework.Proj;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Threading.Tasks;

namespace gView.Blazor.Core.Services;

public class SpatialReferenceService
{
    private readonly ProjDB _projectsDb;
    private readonly ProjDB _datumsDb;

    public SpatialReferenceService()
    {
        _projectsDb = new ProjDB(ProjDBTables.projs);
        _datumsDb = new ProjDB(ProjDBTables.datums);
    }

    public Task<IDictionary<string, string>> GetProjections()
    {
        var result = new Dictionary<string, string>();
        var dataTable = _projectsDb.GetTable();

        foreach (DataRow row in dataTable.Rows)
        {
            result.Add(row[0].ToString(), row[1].ToString());
        }

        return Task.FromResult<IDictionary<string, string>>(result);
    }

    public Task<IDictionary<string,string>> GetDatums()
    {
        var result = new Dictionary<string, string>();
        var dataTable = _datumsDb.GetDatumTable();

        foreach (DataRow row in dataTable.Rows)
        {
            result.Add(row[0].ToString(), row[0].ToString());
        }

        return Task.FromResult<IDictionary<string, string>>(result);
    }

    public Task<SpatialReference> GetSpatialReference(string id)
    {
        return Task.FromResult(new SpatialReference(id));
    }

    public Task<GeodeticDatum> GetGeodeticDatum(string id)
    {
        return Task.FromResult(new GeodeticDatum(id));
    }
}

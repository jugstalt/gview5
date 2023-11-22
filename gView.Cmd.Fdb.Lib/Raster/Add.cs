using gView.Cmd.Fdb.Lib.SpatialIndex;
using gView.DataSources.Fdb.ImageDataset;
using gView.Framework.Data;
using gView.Framework.FDB;
using gView.Framework.system;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace gView.Cmd.Fdb.Lib.Raster;

internal class Add
{
    int ProgressCounter = 0;

    private ICancelTracker _cancelTracker;
    public delegate void ReportActionEvent(Add sender, string action);
    public event ReportActionEvent? ReportAction;

    public Add(ICancelTracker? cancelTracker)
    {
        _cancelTracker = cancelTracker ?? new CancelTracker();
    }

    async public Task<bool> RunAddFiles(IFeatureDataset ds,
                                        IEnumerable<string> fileNames,
                                        Dictionary<string, Guid>? providers)
    {
        FDBImageDataset import = new FDBImageDataset(ds.Database as IImageDB, ds.DatasetName);
        import.handleNonGeorefAsError = true;

        import.ReportProgress += (sender, progress) =>
        {
            ProgressCounter++;

            if (ProgressCounter % 100 == 0)
            {
                ReportAction?.Invoke(this, $"..{ProgressCounter}");
            }
        };

        foreach (string filename in fileNames)
        {
            if (!_cancelTracker.Continue)
            {
                ReportAction?.Invoke(this, $"canceled");
                return false;
            }

            //Console.WriteLine("Import: " + filename);
            if (!await import.Import(filename, providers))
            {
                throw new Exception(import.lastErrorMessage);
            }
        }

        ReportAction?.Invoke(this, $"..{ProgressCounter}");

        return true;
    }

    async public Task<bool> RunImportDirectory(IFeatureDataset ds,
                                     DirectoryInfo di,
                                     string[] filters,
                                     Dictionary<string, Guid>? providers)
    {
        ReportAction?.Invoke(this, $"Import Directory: {di.FullName}");

        foreach (string filter in filters)
        {
            WildcardEx wildcard = new WildcardEx(filter, RegexOptions.IgnoreCase);

            FileInfo[] fis = di.GetFiles(filter);

            List<string> filenames = new List<string>();
            foreach (FileInfo fi in fis)
            {
                if (wildcard.IsMatch(fi.Name))
                {
                    filenames.Add(fi.FullName);
                }
            }

            if (filenames.Count != 0)
            {
                if (!await RunAddFiles(ds, filenames.ToArray(), providers))
                {
                    return false;
                }
            }
        }

        foreach (DirectoryInfo sub in di.GetDirectories())
        {
            if (!_cancelTracker.Continue)
            {
                ReportAction?.Invoke(this, $"canceled");
                return false;
            }

            if (!await RunImportDirectory(ds, sub, filters, providers))
            {
                return false;
            }
        }

        return true;
    }
}

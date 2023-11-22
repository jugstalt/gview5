using System.Collections.Generic;

namespace gView.Framework.Db
{
    public class CommonDbConnectionsModelModel
    {
        public IEnumerable<ProviderModel> Providers { get; set; } 

        #region SubClasses

        public class ProviderModel
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Provider { get; set; }

            public IEnumerable<SchemeModel> Schemes { get; set; }
        }

        public class SchemeModel
        {
            public string Name { get; set; }   
            public string FileFilter { get; set; }
            public string ConnectionString { get; set; }
        }

        #endregion
    }
}

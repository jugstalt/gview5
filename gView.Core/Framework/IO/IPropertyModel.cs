using System;

namespace gView.Framework.IO
{
    public interface IPropertyModel
    {
        Type PropertyModelType { get; }
        object GetPropertyModel();
        void SetPropertyModel(object propertyModel);
    }
}

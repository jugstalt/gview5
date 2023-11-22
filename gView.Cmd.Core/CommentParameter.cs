using gView.Cmd.Core.Abstraction;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace gView.Cmd.Core;
public class CommentParameter : ICommandParameterDescription
{
    public CommentParameter(string comment)
    {
        this.Description = comment;
    }

    public string Name => "";

    public string Description { get; set; }

    public bool IsRequired => false;

    public Type ParameterType => typeof(object);
}

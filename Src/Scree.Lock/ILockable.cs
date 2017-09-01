using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Scree.Log;

namespace Scree.Lock
{
    public interface ILockable
    {
        string Id { get; }
    }
}

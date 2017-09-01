using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scree.Syn
{
    public interface ISynServerService
    {
        long CurrentIndex { get; }
        string CurrentAppId { get; }
        void Add(ISynData[] data);
        SynGetted Get(long lastIndex);
    }
}

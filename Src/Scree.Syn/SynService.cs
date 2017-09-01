using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Scree.Log;
using Scree.Cache;
using Scree.Persister;

namespace Scree.Syn
{
    public interface ISynable
    {
        string Id { get; }
        DateTime LastAlterDate { get; }
        string CurrentAlias { get; }
    }



}

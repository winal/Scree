using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scree.Syn
{
    [Serializable]
    public sealed class SynGetted
    {
        public ISynData[] SynData { get; set; }
        public long Index { get; set; }
    }

    public interface ISynable
    {
        string Id { get; }
        long Version { get; }
    }

    public interface ISynData : ISynable
    {
        string TypeName { get; }
    }

    [Serializable]
    public sealed class SynData : ISynData
    {
        public string TypeName { get;  private set; }
        public string Id { get; private set; }
        public long Version { get; private set; }

        public SynData()
        {
        }
        private SynData(string typeName, string id, long version)
        {
            this.TypeName = typeName;
            this.Id = id;
            this.Version = version;
        }

        public static ISynData Create(string typeName, string id, long version)
        {
            return new SynData(typeName, id, version);
        }

        public static ISynData Create(string typeName, string id)
        {
            return new SynData(typeName, id, 0);
        }

        #region 操作符重载

        public static bool operator ==(SynData x, SynData y)
        {
            if (Object.ReferenceEquals(x, y))
            {
                return true;
            }

            if (((object)x == null) || ((object)y == null))
            {
                return false;
            }

            return string.Equals(x.TypeName, y.TypeName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.Id, y.Id, StringComparison.OrdinalIgnoreCase);
        }
        public static bool operator !=(SynData x, SynData y)
        {
            return !(x == y);
        }
        public override bool Equals(Object obj)
        {
            SynData p = obj as SynData;
            if ((object)p == null)
            {
                return false;
            }

            return this == p;
        }
        public bool Equals(SynData p)
        {
            return this == p;
        }
        public override int GetHashCode()
        {
            string hCode = string.Format("{0}.{1}", TypeName, Id);
            return hCode.GetHashCode();
        }

        #endregion
    }
}

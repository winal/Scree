using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Scree.Attributes;
using Scree.Persister;

namespace MyApp.Models
{
    public enum SystemLogType
    {
        AddUser = 0,
        AlterUser = 1,
        DeleteUser = 2,
    }

    public class SystemLog : SRO
    {
        [StringDataType(Length = 200)]
        public string ObjectType { get; set; }

        [StringDataType(IsNullable = false)]
        public string ObjectId { get; set; }

        public SystemLogType SystemLogType { get; set; }

        [StringDataType(IsMaxLength = true)]
        public string Remark { get; set; }

    }
    public interface INeedFullLogging
    {

    }
    public class SROLog : SRO
    {
        [StringDataType(IsNullable = false)]
        public string ObjectId { get; set; }
        [StringDataType(IsMaxLength = true)]
        public string ObjectJson { get; set; }
        [StringDataType(Length = 200)]
        public string ObjectType { get; set; }
        [StringDataType(Length = 50)]
        public string HostName { get; set; }
    }
}

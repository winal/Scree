using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Scree.Attributes;
using Scree.Common;
using Scree.Persister;

namespace MyUI.Models
{
    public enum NewsType
    {
        Military = 0,
        World = 1,
        Society = 2,
        Culture = 3,
        Travel = 4,
    }
    public class News : SRO
    {
        [StringDataType(IsNullable = false, Length = 50)]
        public string Title { get; set; }

        [StringDataType(IsMaxLength = true)]
        public string Context { get; set; }

        public string Author { get; set; }

        public NewsType Type { get; set; }

        public int ReadingQuantity { get; set; }

        static News()
        {
            TimeStampService.RegisterIdFormat<News>("xw{0:yyMMdd}{1}");
        }
    }


    public enum SystemLogType
    {
        AddNews = 0,
        AlterNews = 1,
        DeleteNews = 2,
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
}
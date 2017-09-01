using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Scree.Common;
using Scree.Persister;
using Scree.Attributes;

namespace MyApp.Models
{
    public class User : SRO, INeedFullLogging
    {
        [StringDataType(IsNullable = false)]
        public string Name { get; set; }

        public Gender Gender { get; set; }

        [StringDataType(Length = 11)]
        public string Mobile { get; set; }

        public decimal AvailableBalance { get; set; }

        static User()
        {
            TimeStampService.RegisterIdFormat<User>("US{3}{0:yyyyMMdd}{4}{2}");
        }

        protected override void BeforeSave()
        {
            this.RegisterStorageBehavior(null);
            this.RegisterStorageBehavior("UserSubById", "UserById" + Id.Substring(Id.Length - 1));
            this.RegisterStorageBehavior("UserSubByHour", "UserByHour" + CreatedDate.Hour.ToString());
        }
    }
    public enum Gender
    {
        Male = 0,
        Female = 1
    }
}

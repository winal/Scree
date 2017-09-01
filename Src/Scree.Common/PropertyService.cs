using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Scree.Attributes;

namespace Scree.Common
{
    public class PropertyInfoProxy : IMyPropertyInfo
    {
        private static Type STRINGTYPE = typeof(string);
        private static Type DATATYPEATTRIBUTETYPE = typeof(DataTypeAttribute);

        private static Dictionary<string, IMyPropertyInfo[]> Properties = new Dictionary<string, IMyPropertyInfo[]>(89);

        private PropertyInfo Property { get; set; }

        public bool IsSave
        {
            get
            {
                return _isSave;
            }
            set
            {
                _isSave = value;
            }
        }
        private bool _isSave = true;

        public bool IsLoad
        {
            get
            {
                return _isLoad;
            }
            set
            {
                _isLoad = value;
            }
        }
        private bool _isLoad = true;

        public string Name
        {
            get
            {
                return Property.Name;
            }
        }

        public Type PropertyType
        {
            get
            {
                return Property.PropertyType;
            }
        }

        public StringDataTypeAttribute StringDataType { get; set; }

        private PropertyInfoProxy(PropertyInfo property)
        {
            this.Property = property;
        }

        public static IMyPropertyInfo GetProperty(Type type, string propertyName)
        {
            IMyPropertyInfo[] properties = GetProperties(type);

            var property = from p in properties
                           where p.Name == propertyName
                           select p;

            return property.First<IMyPropertyInfo>();
        }

        public static IMyPropertyInfo[] GetProperties(Type type)
        {
            string key = string.Format("{0};{1}", type.FullName, type.Assembly.FullName);

            if (Properties.ContainsKey(key))
            {
                return Properties[key];
            }

            lock (Properties)
            {
                if (Properties.ContainsKey(key))
                {
                    return Properties[key];
                }

                IMyPropertyInfo[] properties = PropertiesBuilder(type);
                Properties[key] = properties;

                return properties;
            }
        }

        private static IMyPropertyInfo[] PropertiesBuilder(Type type)
        {
            Queue<IMyPropertyInfo> queue = new Queue<IMyPropertyInfo>();
            PropertyInfo[] propertys = type.GetProperties();

            IMyPropertyInfo myPropertyInfo;
            foreach (PropertyInfo property in propertys)
            {
                myPropertyInfo = new PropertyInfoProxy(property);
                if (myPropertyInfo.PropertyType == STRINGTYPE)
                {
                    myPropertyInfo.StringDataType = new StringDataTypeAttribute();
                }
                queue.Enqueue(myPropertyInfo);

                object[] attributes = property.GetCustomAttributes(DATATYPEATTRIBUTETYPE, false);
                if (attributes == null || attributes.Length == 0)
                {
                    continue;
                }

                var dataTypeAttributes = from attr in attributes
                                         where attr is DataTypeAttribute
                                         select attr;

                foreach (DataTypeAttribute attr in dataTypeAttributes)
                {
                    if (attr.IsSave == false)
                    {
                        myPropertyInfo.IsSave = false;
                    }

                    if (attr.IsLoad == false)
                    {
                        myPropertyInfo.IsLoad = false;
                    }

                    if (attr is StringDataTypeAttribute)
                    {
                        myPropertyInfo.StringDataType = attr as StringDataTypeAttribute;
                    }
                }
            }

            return queue.ToArray();
        }

        public void SetValue(object obj, object value)
        {
            Property.SetValue(obj, value, null);
        }

        public object GetValue(object obj)
        {
            return Property.GetValue(obj, null);
        }
    }

    public interface IMyPropertyInfo
    {
        void SetValue(object obj, object value);

        object GetValue(object obj);

        bool IsSave { get; set; }

        bool IsLoad { get; set; }

        string Name { get; }

        Type PropertyType { get; }

        StringDataTypeAttribute StringDataType { get; set; }
    }

}

using System;
using System.Reflection;
using System.Collections;

namespace DynamicProxy
{
    public class MetaDataFactory
    {
        private static Hashtable typeMap = new Hashtable();

        private MetaDataFactory()
        {
        }

        public static void Add(Type interfaceType)
        {
            if (interfaceType != null)
            {
                lock (typeMap.SyncRoot)
                {
                    if (!typeMap.ContainsKey(interfaceType.FullName))
                    {
                        typeMap.Add(interfaceType.FullName, interfaceType);
                    }
                }
            }
        }

        public static MethodInfo GetMethod(string name, int i)
        {
            Type type = null;
            lock (typeMap.SyncRoot)
            {
                type = (Type)typeMap[name];
            }

            MethodInfo[] methods = type.GetMethods();
            if (i < methods.Length)
            {
                return methods[i];
            }

            return null;
        }
    }
}

using System;
using System.Runtime.Remoting.Proxies;

namespace DynamicProxy
{

    public class SecurityProxy : IProxyInvocationHandler
    {
        Object obj = null;

        private SecurityProxy(Object obj)
        {
            this.obj = obj;
        }

        public static Object NewInstance(Object obj)
        {
            return ProxyFactory.GetInstance().Create(new SecurityProxy(obj), obj.GetType());
        }

        public Object Invoke(Object proxy, System.Reflection.MethodInfo method, Object[] parameters)
        {
            Object retVal = null;
            string userRole = "role";
            if (SecurityManager.IsMethodInRole(userRole, method.Name))
            {
                retVal = method.Invoke(obj, parameters);
                Console.WriteLine("fx");
            }
            else
            {
                throw new Exception("Invalid permission to invoke " + method.Name);
            }
            return retVal;
        }
    }
}

using System;
using System.Reflection;

namespace DynamicProxy
{
    public interface IProxyInvocationHandler
    {
        Object Invoke(Object proxy, MethodInfo method, Object[] parameters);
    }
}

using System;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace DynamicProxy
{
	/// <summary>
	/// </summary>
	public class ProxyFactory
	{
        private static ProxyFactory instance;
        private static Object lockObj = new Object();

        private Hashtable typeMap = Hashtable.Synchronized( new Hashtable() );
        private static readonly Hashtable opCodeTypeMapper = new Hashtable();

        private const string PROXY_SUFFIX       = "Proxy";
        private const string ASSEMBLY_NAME      = "ProxyAssembly";
        private const string MODULE_NAME        = "ProxyModule";
        private const string HANDLER_NAME       = "handler";

        static ProxyFactory() {
            opCodeTypeMapper.Add( typeof( System.Boolean ), OpCodes.Ldind_I1 );
            opCodeTypeMapper.Add( typeof( System.Int16 ), OpCodes.Ldind_I2 );
            opCodeTypeMapper.Add( typeof( System.Int32 ), OpCodes.Ldind_I4 );
            opCodeTypeMapper.Add( typeof( System.Int64 ), OpCodes.Ldind_I8 );
            opCodeTypeMapper.Add( typeof( System.Double ), OpCodes.Ldind_R8 );
            opCodeTypeMapper.Add( typeof( System.Single ), OpCodes.Ldind_R4 );
            opCodeTypeMapper.Add( typeof( System.UInt16 ), OpCodes.Ldind_U2 );
            opCodeTypeMapper.Add( typeof( System.UInt32 ), OpCodes.Ldind_U4 );
        }

        private ProxyFactory() {
            
        }

        public static ProxyFactory GetInstance() {
            if ( instance == null ) {
                CreateInstance();
            }

            return instance;
        }

        private static void CreateInstance()
        {
            lock (lockObj)
            {
                if (instance == null)
                {
                    instance = new ProxyFactory();
                }
            }
        }

        public Object Create(IProxyInvocationHandler handler, Type objType, bool isObjInterface)
        {
            string typeName = objType.FullName + PROXY_SUFFIX;
            Type type = (Type)typeMap[typeName];

            if (type == null)
            {
                if (isObjInterface)
                {
                    type = CreateType(handler, new Type[] { objType }, typeName);
                }
                else
                {
                    type = CreateType(handler, objType.GetInterfaces(), typeName);
                }

                typeMap.Add(typeName, type);
            }

            return Activator.CreateInstance(type, new object[] { handler });
        }

        public Object Create(IProxyInvocationHandler handler, Type objType)
        {
            return Create(handler, objType, false);
        }

        private Type CreateType(IProxyInvocationHandler handler, Type[] interfaces, String dynamicTypeName)
        {
            Type retVal = null;

            if (handler != null && interfaces != null)
            {
                Type objType = typeof(System.Object);
                Type handlerType = typeof(IProxyInvocationHandler);

                AppDomain domain = Thread.GetDomain();
                AssemblyName assemblyName = new AssemblyName();
                assemblyName.Name = ASSEMBLY_NAME;
                assemblyName.Version = new Version(1, 0, 0, 0);

                AssemblyBuilder assemblyBuilder = domain.DefineDynamicAssembly(
                    assemblyName, AssemblyBuilderAccess.Run);

                ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(MODULE_NAME);

                TypeAttributes typeAttributes = TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed;

                TypeBuilder typeBuilder = moduleBuilder.DefineType(dynamicTypeName, typeAttributes, objType, interfaces);

                FieldBuilder handlerField = typeBuilder.DefineField(HANDLER_NAME, handlerType, FieldAttributes.Private);

                ConstructorInfo superConstructor = objType.GetConstructor(new Type[0]);
                ConstructorBuilder delegateConstructor = typeBuilder.DefineConstructor(
                    MethodAttributes.Public, CallingConventions.Standard, new Type[] { handlerType });

                #region("Constructor IL Code")
                ILGenerator constructorIL = delegateConstructor.GetILGenerator();

                constructorIL.Emit(OpCodes.Ldarg_0);
                constructorIL.Emit(OpCodes.Ldarg_1);
                constructorIL.Emit(OpCodes.Stfld, handlerField);
                constructorIL.Emit(OpCodes.Ldarg_0);
                constructorIL.Emit(OpCodes.Call, superConstructor);
                constructorIL.Emit(OpCodes.Ret);
                #endregion

                foreach (Type interfaceType in interfaces)
                {
                    GenerateMethod(interfaceType, handlerField, typeBuilder);
                }

                retVal = typeBuilder.CreateType();
            }

            return retVal;
        }

        private void GenerateMethod(Type interfaceType, FieldBuilder handlerField, TypeBuilder typeBuilder)
        {
            MetaDataFactory.Add(interfaceType);
            MethodInfo[] interfaceMethods = interfaceType.GetMethods();
            if (interfaceMethods != null)
            {

                for (int i = 0; i < interfaceMethods.Length; i++)
                {
                    MethodInfo methodInfo = interfaceMethods[i];
           
                    ParameterInfo[] methodParams = methodInfo.GetParameters();
                    int numOfParams = methodParams.Length;
                    Type[] methodParameters = new Type[numOfParams];

                    for (int j = 0; j < numOfParams; j++)
                    {
                        methodParameters[j] = methodParams[j].ParameterType;
                    }

                    MethodBuilder methodBuilder = typeBuilder.DefineMethod(
                        methodInfo.Name,
                        MethodAttributes.Public | MethodAttributes.Virtual,
                        CallingConventions.Standard,
                        methodInfo.ReturnType, methodParameters);

                    #region( "Handler Method IL Code" )
                    ILGenerator methodIL = methodBuilder.GetILGenerator();

                    if (!methodInfo.ReturnType.Equals(typeof(void)))
                    {
                        methodIL.DeclareLocal(methodInfo.ReturnType);
                        if (methodInfo.ReturnType.IsValueType && !methodInfo.ReturnType.IsPrimitive)
                        {
                            methodIL.DeclareLocal(methodInfo.ReturnType);
                        }
                    }

                    if (numOfParams > 0)
                    {
                        methodIL.DeclareLocal(typeof(System.Object[]));
                    }

                    Label handlerLabel = methodIL.DefineLabel();
                    Label returnLabel = methodIL.DefineLabel();

                    methodIL.Emit(OpCodes.Ldarg_0);
                    methodIL.Emit(OpCodes.Ldfld, handlerField);
                    methodIL.Emit(OpCodes.Brtrue_S, handlerLabel);
                    if (!methodInfo.ReturnType.Equals(typeof(void)))
                    {
                        if (methodInfo.ReturnType.IsValueType && !methodInfo.ReturnType.IsPrimitive && !methodInfo.ReturnType.IsEnum)
                        {
                            methodIL.Emit(OpCodes.Ldloc_1);
                        }
                        else
                        {
                            methodIL.Emit(OpCodes.Ldnull);
                        }
                        methodIL.Emit(OpCodes.Stloc_0);
                        methodIL.Emit(OpCodes.Br_S, returnLabel);
                    }

                    methodIL.MarkLabel(handlerLabel);

                    methodIL.Emit(OpCodes.Ldarg_0);
                    methodIL.Emit(OpCodes.Ldfld, handlerField);
                    methodIL.Emit(OpCodes.Ldarg_0);
                    methodIL.Emit(OpCodes.Ldstr, interfaceType.FullName);
                    methodIL.Emit(OpCodes.Ldc_I4, i);
                    methodIL.Emit(OpCodes.Call, typeof(DynamicProxy.MetaDataFactory).GetMethod("GetMethod", new Type[] { typeof(string), typeof(int) }));

                    methodIL.Emit(OpCodes.Ldc_I4, numOfParams);
                    methodIL.Emit(OpCodes.Newarr, typeof(System.Object));

                    if (numOfParams > 0)
                    {
                        methodIL.Emit(OpCodes.Stloc_1);
                        for (int j = 0; j < numOfParams; j++)
                        {
                            methodIL.Emit(OpCodes.Ldloc_1);
                            methodIL.Emit(OpCodes.Ldc_I4, j);
                            methodIL.Emit(OpCodes.Ldarg, j + 1);
                            if (methodParameters[j].IsValueType)
                            {
                                methodIL.Emit(OpCodes.Box, methodParameters[j]);
                            }
                            methodIL.Emit(OpCodes.Stelem_Ref);
                        }
                        methodIL.Emit(OpCodes.Ldloc_1);
                    }

                    methodIL.Emit(OpCodes.Callvirt,
                        typeof(DynamicProxy.IProxyInvocationHandler).GetMethod("Invoke"));

                    if (!methodInfo.ReturnType.Equals(typeof(void)))
                    {
                        if (methodInfo.ReturnType.IsValueType)
                        {
                            methodIL.Emit(OpCodes.Unbox, methodInfo.ReturnType);
                            if (methodInfo.ReturnType.IsEnum)
                            {
                                methodIL.Emit(OpCodes.Ldind_I4);
                            }
                            else if (!methodInfo.ReturnType.IsPrimitive)
                            {
                                methodIL.Emit(OpCodes.Ldobj, methodInfo.ReturnType);
                            }
                            else
                            {
                                methodIL.Emit((OpCode)opCodeTypeMapper[methodInfo.ReturnType]);
                            }
                        }

                        methodIL.Emit(OpCodes.Stloc_0);
                        methodIL.Emit(OpCodes.Br_S, returnLabel);
                        methodIL.MarkLabel(returnLabel);
                        methodIL.Emit(OpCodes.Ldloc_0);
                    }
                    else
                    {
                        methodIL.Emit(OpCodes.Pop);
                        methodIL.MarkLabel(returnLabel);
                    }

                    methodIL.Emit(OpCodes.Ret);
                    #endregion

                }
            }

            foreach (Type parentType in interfaceType.GetInterfaces())
            {
                GenerateMethod(parentType, handlerField, typeBuilder);
            }
        }
    }
}
﻿namespace ThMEPWSS.DynamicInvocation
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    /// <summary>
    /// Represents the implementation of an interface method.
    /// </summary>
    public class InterfaceMethodOverride
    {
        private static readonly MethodInfo BuildAdditionalInterfaceNonImplementedExceptionMethod =
            StaticReflection.GetMethodInfo(() => InterfaceMethodOverride.BuildAdditionalInterfaceNonImplementedException());

        private const MethodAttributes ImplicitImplementationAttributes =
            MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final
            | MethodAttributes.HideBySig | MethodAttributes.NewSlot;
        private const MethodAttributes ExplicitImplementationAttributes =
            MethodAttributes.Private | MethodAttributes.Virtual | MethodAttributes.Final
            | MethodAttributes.HideBySig | MethodAttributes.NewSlot;

        private readonly TypeBuilder typeBuilder;
        private readonly MethodInfo methodToOverride;
        private readonly ParameterInfo[] methodParameters;
        private readonly FieldBuilder proxyInterceptionPipelineField;
        private readonly bool explicitImplementation;
        private readonly FieldBuilder targetField;
        private readonly Type targetInterface;
        private readonly GenericParameterMapper targetInterfaceParameterMapper;
        private readonly int overrideCount;

        internal InterfaceMethodOverride(
            TypeBuilder typeBuilder,
            FieldBuilder proxyInterceptionPipelineField,
            FieldBuilder targetField,
            MethodInfo methodToOverride,
            Type targetInterface,
            GenericParameterMapper targetInterfaceParameterMapper,
            bool explicitImplementation,
            int overrideCount)
        {
            this.typeBuilder = typeBuilder;
            this.proxyInterceptionPipelineField = proxyInterceptionPipelineField;
            this.explicitImplementation = explicitImplementation;
            this.targetField = targetField;
            this.methodToOverride = methodToOverride;
            this.targetInterface = targetInterface;
            this.targetInterfaceParameterMapper = targetInterfaceParameterMapper;
            this.methodParameters = methodToOverride.GetParameters();
            this.overrideCount = overrideCount;
        }

        internal MethodBuilder AddMethod()
        {
            MethodBuilder delegateMethod = CreateDelegateImplementation();
            return CreateMethodOverride(delegateMethod);
        }

        private string CreateMethodName(string purpose)
        {
            return "<" + methodToOverride.Name + "_" + purpose + ">__" +
                overrideCount.ToString(CultureInfo.InvariantCulture);
        }

        private static readonly OpCode[] LoadArgsOpcodes =
        {
            OpCodes.Ldarg_1,
            OpCodes.Ldarg_2,
            OpCodes.Ldarg_3
        };

        private static void EmitLoadArgument(ILGenerator il, int argumentNumber)
        {
            if (argumentNumber < LoadArgsOpcodes.Length)
            {
                il.Emit(LoadArgsOpcodes[argumentNumber]);
            }
            else
            {
                il.Emit(OpCodes.Ldarg, argumentNumber + 1);
            }
        }

        private static readonly OpCode[] LoadConstOpCodes =
        {
            OpCodes.Ldc_I4_0,
            OpCodes.Ldc_I4_1,
            OpCodes.Ldc_I4_2,
            OpCodes.Ldc_I4_3,
            OpCodes.Ldc_I4_4,
            OpCodes.Ldc_I4_5,
            OpCodes.Ldc_I4_6,
            OpCodes.Ldc_I4_7,
            OpCodes.Ldc_I4_8,
        };

        private static void EmitLoadConstant(ILGenerator il, int i)
        {
            if (i < LoadConstOpCodes.Length)
            {
                il.Emit(LoadConstOpCodes[i]);
            }
            else
            {
                il.Emit(OpCodes.Ldc_I4, i);
            }
        }

        private static void EmitBox(ILGenerator il, Type typeOnStack)
        {
            if (typeOnStack.IsValueType || typeOnStack.IsGenericParameter)
            {
                il.Emit(OpCodes.Box, typeOnStack);
            }
        }

        private static void EmitUnboxOrCast(ILGenerator il, Type targetType)
        {
            if (targetType.IsValueType || targetType.IsGenericParameter)
            {
                il.Emit(OpCodes.Unbox_Any, targetType);
            }
            else
            {
                il.Emit(OpCodes.Castclass, targetType);
            }
        }

        private MethodBuilder CreateDelegateImplementation()
        {
            string methodName = CreateMethodName("DelegateImplementation");

            MethodBuilder methodBuilder = typeBuilder.DefineMethod(methodName,
                MethodAttributes.Private | MethodAttributes.HideBySig);
            List<LocalBuilder> outOrRefLocals = new List<LocalBuilder>();

            var paramMapper = new MethodOverrideParameterMapper(methodToOverride);
            paramMapper.SetupParameters(methodBuilder, this.targetInterfaceParameterMapper);

            methodBuilder.SetReturnType(typeof(IMethodReturn));
            // Adding parameters
            methodBuilder.SetParameters(typeof(IMethodInvocation), typeof(GetNextInterceptionBehaviorDelegate));
            // Parameter 
            methodBuilder.DefineParameter(1, ParameterAttributes.None, "inputs");
            // Parameter 
            methodBuilder.DefineParameter(2, ParameterAttributes.None, "getNext");

            methodBuilder.SetCustomAttribute(new CustomAttributeBuilder(CompilerGeneratedAttributeMethods.CompilerGeneratedAttribute, new object[0]));

            ILGenerator il = methodBuilder.GetILGenerator();

            if (this.targetField != null)
            {
                // forwarding implementation
                Label done = il.DefineLabel();
                LocalBuilder ex = il.DeclareLocal(typeof(Exception));

                LocalBuilder baseReturn = null;
                LocalBuilder parameters = null;

                if (MethodHasReturnValue)
                {
                    baseReturn = il.DeclareLocal(paramMapper.GetReturnType());
                }
                LocalBuilder retval = il.DeclareLocal(typeof(IMethodReturn));

                il.BeginExceptionBlock();
                // Call the target method
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, targetField);

                if (methodParameters.Length > 0)
                {
                    parameters = il.DeclareLocal(typeof(IParameterCollection));
                    il.Emit(OpCodes.Ldarg_1);
                    il.EmitCall(OpCodes.Callvirt, IMethodInvocationMethods.GetArguments, null);
                    il.Emit(OpCodes.Stloc, parameters);

                    for (int i = 0; i < methodParameters.Length; ++i)
                    {
                        il.Emit(OpCodes.Ldloc, parameters);
                        EmitLoadConstant(il, i);
                        il.EmitCall(OpCodes.Callvirt, IListMethods.GetItem, null);
                        Type parameterType = paramMapper.GetParameterType(methodParameters[i].ParameterType);

                        if (parameterType.IsByRef)
                        {
                            Type elementType = parameterType.GetElementType();
                            LocalBuilder refShadowLocal = il.DeclareLocal(elementType);
                            outOrRefLocals.Add(refShadowLocal);
                            EmitUnboxOrCast(il, elementType);
                            il.Emit(OpCodes.Stloc, refShadowLocal);
                            il.Emit(OpCodes.Ldloca, refShadowLocal);
                        }
                        else
                        {
                            EmitUnboxOrCast(il, parameterType);
                        }
                    }
                }

                MethodInfo callTarget = methodToOverride;
                if (callTarget.IsGenericMethod)
                {
                    callTarget = methodToOverride.MakeGenericMethod(paramMapper.GenericMethodParameters);
                }

                il.Emit(OpCodes.Callvirt, callTarget);

                if (MethodHasReturnValue)
                {
                    il.Emit(OpCodes.Stloc, baseReturn);
                }

                // Generate  the return value
                il.Emit(OpCodes.Ldarg_1);
                if (MethodHasReturnValue)
                {
                    il.Emit(OpCodes.Ldloc, baseReturn);
                    EmitBox(il, paramMapper.GetReturnType());
                }
                else
                {
                    il.Emit(OpCodes.Ldnull);
                }
                EmitLoadConstant(il, methodParameters.Length);
                il.Emit(OpCodes.Newarr, typeof(object));

                if (methodParameters.Length > 0)
                {
                    LocalBuilder outputArguments = il.DeclareLocal(typeof(object[]));
                    il.Emit(OpCodes.Stloc, outputArguments);

                    int outputArgNum = 0;
                    for (int i = 0; i < methodParameters.Length; ++i)
                    {
                        il.Emit(OpCodes.Ldloc, outputArguments);
                        EmitLoadConstant(il, i);

                        Type parameterType = paramMapper.GetParameterType(methodParameters[i].ParameterType);
                        if (parameterType.IsByRef)
                        {
                            parameterType = parameterType.GetElementType();
                            il.Emit(OpCodes.Ldloc, outOrRefLocals[outputArgNum++]);
                            EmitBox(il, parameterType);
                        }
                        else
                        {
                            il.Emit(OpCodes.Ldloc, parameters);
                            EmitLoadConstant(il, i);
                            il.Emit(OpCodes.Callvirt, IListMethods.GetItem);
                        }
                        il.Emit(OpCodes.Stelem_Ref);
                    }
                    il.Emit(OpCodes.Ldloc, outputArguments);
                }

                il.Emit(OpCodes.Callvirt, IMethodInvocationMethods.CreateReturn);
                il.Emit(OpCodes.Stloc, retval);
                il.BeginCatchBlock(typeof(Exception));
                il.Emit(OpCodes.Stloc, ex);
                // Create an exception return
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldloc, ex);
                il.EmitCall(OpCodes.Callvirt, IMethodInvocationMethods.CreateExceptionMethodReturn, null);
                il.Emit(OpCodes.Stloc, retval);
                il.EndExceptionBlock();
                il.MarkLabel(done);
                il.Emit(OpCodes.Ldloc, retval);
                il.Emit(OpCodes.Ret);
            }
            else
            {
                // exception-throwing implementation
                il.Emit(OpCodes.Ldarg_1);
                il.EmitCall(OpCodes.Call, BuildAdditionalInterfaceNonImplementedExceptionMethod, null);
                il.EmitCall(OpCodes.Callvirt, IMethodInvocationMethods.CreateExceptionMethodReturn, null);
                il.Emit(OpCodes.Ret);
            }
            return methodBuilder;
        }

        private MethodBuilder CreateMethodOverride(MethodBuilder delegateMethod)
        {
            string methodName =
                this.explicitImplementation
                        ? methodToOverride.DeclaringType.Name + "." + methodToOverride.Name
                        : methodToOverride.Name;

            MethodBuilder methodBuilder =
                typeBuilder.DefineMethod(
                    methodName,
                    this.explicitImplementation ? ExplicitImplementationAttributes : ImplicitImplementationAttributes);

            var paramMapper = new MethodOverrideParameterMapper(methodToOverride);
            paramMapper.SetupParameters(methodBuilder, this.targetInterfaceParameterMapper);

            methodBuilder.SetReturnType(paramMapper.GetReturnType());
            methodBuilder.SetParameters(methodParameters.Select(pi => paramMapper.GetParameterType(pi.ParameterType)).ToArray());
            if (this.explicitImplementation)
            {
                this.typeBuilder.DefineMethodOverride(methodBuilder, this.methodToOverride);
            }

            int paramNum = 1;
            foreach (ParameterInfo pi in methodParameters)
            {
                methodBuilder.DefineParameter(paramNum++, pi.Attributes, pi.Name);
            }

            ILGenerator il = methodBuilder.GetILGenerator();

            LocalBuilder methodReturn = il.DeclareLocal(typeof(IMethodReturn));
            LocalBuilder ex = il.DeclareLocal(typeof(Exception));
            LocalBuilder parameterArray = il.DeclareLocal(typeof(object[]));
            LocalBuilder inputs = il.DeclareLocal(typeof(VirtualMethodInvocation));

            // Create instance of VirtualMethodInvocation
            il.Emit(OpCodes.Ldarg_0); // target object

            // If we have a targetField, that means we're building a proxy and
            // should use it as the target object. If we don't, we're building
            // a type interceptor and should leave the this pointer as the
            // target.
            if (targetField != null)
            {
                il.Emit(OpCodes.Ldfld, targetField);
            }

            // If we have a generic method, we want to make sure we're using the open constructed generic method
            // so when a closed generic version of the method is invoked the actual type parameters are used
            il.Emit(
                OpCodes.Ldtoken,
                methodToOverride.IsGenericMethodDefinition
                    ? methodToOverride.MakeGenericMethod(paramMapper.GenericMethodParameters)
                    : methodToOverride);
            if (methodToOverride.DeclaringType.IsGenericType)
            {
                // if the declaring type is generic, we need to get the method from the target type
                il.Emit(OpCodes.Ldtoken, targetInterface);
                il.Emit(OpCodes.Call, MethodBaseMethods.GetMethodForGenericFromHandle);
            }
            else
            {
                il.Emit(OpCodes.Call, MethodBaseMethods.GetMethodFromHandle); // target method
            }

            EmitLoadConstant(il, methodParameters.Length);
            il.Emit(OpCodes.Newarr, typeof(object)); // object[] parameters
            if (methodParameters.Length > 0)
            {
                il.Emit(OpCodes.Stloc, parameterArray);

                for (int i = 0; i < methodParameters.Length; ++i)
                {
                    il.Emit(OpCodes.Ldloc, parameterArray);
                    EmitLoadConstant(il, i);
                    EmitLoadArgument(il, i);
                    Type elementType = paramMapper.GetParameterType(methodParameters[i].ParameterType);
                    if (elementType.IsByRef)
                    {
                        elementType = paramMapper.GetElementType(methodParameters[i].ParameterType);
                        il.Emit(OpCodes.Ldobj, elementType);
                    }
                    EmitBox(il, elementType);
                    il.Emit(OpCodes.Stelem_Ref);
                }

                il.Emit(OpCodes.Ldloc, parameterArray);
            }
            il.Emit(OpCodes.Newobj, VirtualMethodInvocationMethods.VirtualMethodInvocation);
            il.Emit(OpCodes.Stloc, inputs);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, proxyInterceptionPipelineField);
            il.Emit(OpCodes.Ldloc, inputs);

            // Put delegate reference onto the stack
            il.Emit(OpCodes.Ldarg_0);

            MethodInfo callTarget = delegateMethod;
            if (callTarget.IsGenericMethod)
            {
                callTarget = delegateMethod.MakeGenericMethod(paramMapper.GenericMethodParameters);
            }

            il.Emit(OpCodes.Ldftn, callTarget);

            il.Emit(OpCodes.Newobj, InvokeInterceptionBehaviorDelegateMethods.InvokeInterceptionBehaviorDelegate);

            // And call the pipeline
            il.Emit(OpCodes.Call, InterceptionBehaviorPipelineMethods.Invoke);

            il.Emit(OpCodes.Stloc, methodReturn);

            // Was there an exception?
            Label noException = il.DefineLabel();
            il.Emit(OpCodes.Ldloc, methodReturn);
            il.EmitCall(OpCodes.Callvirt, IMethodReturnMethods.GetException, null);
            il.Emit(OpCodes.Stloc, ex);
            il.Emit(OpCodes.Ldloc, ex);
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ceq);
            il.Emit(OpCodes.Brtrue_S, noException);
            il.Emit(OpCodes.Ldloc, ex);
            il.Emit(OpCodes.Throw);

            il.MarkLabel(noException);

            // Unpack any ref/out parameters
            if (methodParameters.Length > 0)
            {
                int outputArgNum = 0;
                for (paramNum = 0; paramNum < methodParameters.Length; ++paramNum)
                {
                    ParameterInfo pi = methodParameters[paramNum];
                    if (pi.ParameterType.IsByRef)
                    {
                        // Get the original parameter value - address of the ref or out
                        EmitLoadArgument(il, paramNum);

                        // Get the value of this output parameter out of the Outputs collection
                        il.Emit(OpCodes.Ldloc, methodReturn);
                        il.Emit(OpCodes.Callvirt, IMethodReturnMethods.GetOutputs);
                        EmitLoadConstant(il, outputArgNum++);
                        il.Emit(OpCodes.Callvirt, IListMethods.GetItem);
                        EmitUnboxOrCast(il, paramMapper.GetElementType(pi.ParameterType));

                        // And store in the caller
                        il.Emit(OpCodes.Stobj, paramMapper.GetElementType(pi.ParameterType));
                    }
                }
            }

            if (MethodHasReturnValue)
            {
                il.Emit(OpCodes.Ldloc, methodReturn);
                il.EmitCall(OpCodes.Callvirt, IMethodReturnMethods.GetReturnValue, null);
                EmitUnboxOrCast(il, paramMapper.GetReturnType());
            }
            il.Emit(OpCodes.Ret);

            return methodBuilder;
        }

        private bool MethodHasReturnValue
        {
            get { return methodToOverride.ReturnType != typeof(void); }
        }

        /// <summary>
        /// Used to throw an <see cref="NotImplementedException"/> for non-implemented methods on the
        /// additional interfaces.
        /// </summary>
        public static Exception BuildAdditionalInterfaceNonImplementedException()
        {
            return new NotImplementedException();
        }
    }

    internal class IMethodReturnMethods
    {
        public static MethodInfo GetReturnValue { get; internal set; }
        public static byte GetOutputs { get; internal set; }
        public static MethodInfo GetException { get; internal set; }
    }

    internal class InterceptionBehaviorPipelineMethods
    {
        public static byte Invoke { get; internal set; }
    }

    internal class VirtualMethodInvocationMethods
    {
        public static byte VirtualMethodInvocation { get; internal set; }
    }

    internal class InvokeInterceptionBehaviorDelegateMethods
    {
        public static byte InvokeInterceptionBehaviorDelegate { get; internal set; }
    }

    internal class MethodBaseMethods
    {
        public static byte GetMethodForGenericFromHandle { get; internal set; }
        public static byte GetMethodFromHandle { get; internal set; }
    }

    internal class VirtualMethodInvocation
    {
    }

    internal class IListMethods
    {
        public static MethodInfo GetItem { get; internal set; }
    }

    internal class IMethodInvocationMethods
    {
        public static MethodInfo GetArguments { get; internal set; }
        public static byte CreateReturn { get; internal set; }
        public static MethodInfo CreateExceptionMethodReturn { get; internal set; }
    }

    internal interface IParameterCollection
    {
    }

    internal class StaticReflection
    {
        internal static MethodInfo GetMethodInfo(Func<Exception> p)
        {
            throw new NotImplementedException();
        }
    }

    internal class CompilerGeneratedAttributeMethods
    {
        public static ConstructorInfo CompilerGeneratedAttribute { get; internal set; }
    }

    internal class GetNextInterceptionBehaviorDelegate
    {
    }

    internal class MethodOverrideParameterMapper
    {
        private MethodInfo methodToOverride;

        public MethodOverrideParameterMapper(MethodInfo methodToOverride)
        {
            this.methodToOverride = methodToOverride;
        }

        public Type[] GenericMethodParameters { get; internal set; }

        internal Type GetElementType(Type parameterType)
        {
            throw new NotImplementedException();
        }

        internal Type GetParameterType(Type parameterType)
        {
            throw new NotImplementedException();
        }

        internal Type GetReturnType()
        {
            throw new NotImplementedException();
        }

        internal void SetupParameters(MethodBuilder methodBuilder, GenericParameterMapper targetInterfaceParameterMapper)
        {
            throw new NotImplementedException();
        }
    }

    internal interface IMethodInvocation
    {
    }

    internal interface IMethodReturn
    {
    }
}
namespace ThMEPWSS.DynamicInvocation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    internal class InterfaceImplementation
    {
        private readonly TypeBuilder typeBuilder;
        private readonly Type @interface;
        private readonly Type targetInterface;
        private readonly GenericParameterMapper genericParameterMapper;
        private readonly FieldBuilder proxyInterceptionPipelineField;
        private readonly bool explicitImplementation;
        private readonly FieldBuilder targetField;

        public InterfaceImplementation(
            TypeBuilder typeBuilder,
            Type @interface,
            FieldBuilder proxyInterceptionPipelineField,
            bool explicitImplementation)
            : this(typeBuilder, @interface, proxyInterceptionPipelineField, explicitImplementation, null)
        { }

        public InterfaceImplementation(
            TypeBuilder typeBuilder,
            Type @interface,
            FieldBuilder proxyInterceptionPipelineField,
            bool explicitImplementation,
            FieldBuilder targetField)
            : this(typeBuilder, @interface, GenericParameterMapper.DefaultMapper, proxyInterceptionPipelineField, explicitImplementation, targetField)
        { }

        public InterfaceImplementation(
            TypeBuilder typeBuilder,
            Type @interface,
            GenericParameterMapper genericParameterMapper,
            FieldBuilder proxyInterceptionPipelineField,
            bool explicitImplementation,
            FieldBuilder targetField)
        {
            this.typeBuilder = typeBuilder;
            this.@interface = @interface;
            this.genericParameterMapper = genericParameterMapper;
            this.proxyInterceptionPipelineField = proxyInterceptionPipelineField;
            this.explicitImplementation = explicitImplementation;
            this.targetField = targetField;

            if (@interface.IsGenericType)
            {
                // when the @interface is generic we need to get references to its methods though it
                // in this case, the targetInterface is a constructed version using the generic type parameters
                // from the generated type generate type
                var definition = @interface.GetGenericTypeDefinition();
                var mappedParameters = definition.GetGenericArguments().Select(t => genericParameterMapper.Map(t)).ToArray();
                this.targetInterface = definition.MakeGenericType(mappedParameters);
            }
            else
            {
                this.targetInterface = @interface;
            }
        }

        public int Implement(HashSet<Type> implementedInterfaces, int memberCount)
        {
            if (implementedInterfaces.Contains(this.@interface))
            {
                return memberCount;
            }

            implementedInterfaces.Add(this.@interface);

            typeBuilder.AddInterfaceImplementation(this.@interface);

            foreach (MethodInfo method in MethodsToIntercept())
            {
                OverrideMethod(method, memberCount++);
            }

            foreach (PropertyInfo property in PropertiesToIntercept())
            {
                OverrideProperty(property, memberCount++);
            }

            foreach (EventInfo @event in EventsToIntercept())
            {
                OverrideEvent(@event, memberCount++);
            }

            foreach (var @extendedInterface in this.@interface.GetInterfaces())
            {
                memberCount =
                    new InterfaceImplementation(
                        this.typeBuilder,
                        @extendedInterface,
                        new GenericParameterMapper(@extendedInterface, this.genericParameterMapper),
                        this.proxyInterceptionPipelineField,
                        this.explicitImplementation,
                        this.targetField)
                        .Implement(implementedInterfaces, memberCount);
            }

            return memberCount;
        }

        private IEnumerable<MethodInfo> MethodsToIntercept()
        {
            foreach (MethodInfo method in
                this.@interface.GetMethods(
                    BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (!method.IsSpecialName)
                {
                    yield return method;
                }
            }
        }

        private void OverrideMethod(MethodInfo method, int methodNum)
        {
            new InterfaceMethodOverride(
                this.typeBuilder,
                this.proxyInterceptionPipelineField,
                this.targetField,
                method,
                this.targetInterface,
                this.genericParameterMapper,
                this.explicitImplementation,
                methodNum)
                .AddMethod();
        }

        private IEnumerable<PropertyInfo> PropertiesToIntercept()
        {
            return this.@interface.GetProperties(
                BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        private void OverrideProperty(PropertyInfo property, int count)
        {
            MethodBuilder getMethod = OverridePropertyMethod(property.GetGetMethod(), count);
            MethodBuilder setMethod = OverridePropertyMethod(property.GetSetMethod(), count);
            AddPropertyDefinition(property, getMethod, setMethod);
        }

        private void AddPropertyDefinition(PropertyInfo property, MethodBuilder getMethod, MethodBuilder setMethod)
        {
            PropertyBuilder newProperty =
                this.typeBuilder.DefineProperty(
                    property.Name,
                    property.Attributes,
                    property.PropertyType,
                    property.GetIndexParameters().Select(param => param.ParameterType).ToArray());

            if (getMethod != null)
            {
                newProperty.SetGetMethod(getMethod);
            }

            if (setMethod != null)
            {
                newProperty.SetSetMethod(setMethod);
            }
        }

        private MethodBuilder OverridePropertyMethod(MethodInfo method, int count)
        {
            return method == null
                ? null
                : new InterfaceMethodOverride(
                    this.typeBuilder,
                    this.proxyInterceptionPipelineField,
                    this.targetField,
                    method,
                    this.targetInterface,
                    this.genericParameterMapper,
                    this.explicitImplementation,
                    count)
                    .AddMethod();
        }

        private IEnumerable<EventInfo> EventsToIntercept()
        {
            return this.@interface.GetEvents(
                BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        private void OverrideEvent(EventInfo @event, int count)
        {
            MethodBuilder addMethod = OverrideEventMethod(@event.GetAddMethod(), count);
            MethodBuilder removeMethod = OverrideEventMethod(@event.GetRemoveMethod(), count);
            AddEventDefinition(@event, addMethod, removeMethod);
        }

        private void AddEventDefinition(EventInfo @event, MethodBuilder addMethod, MethodBuilder removeMethod)
        {
            EventBuilder newEvent = this.typeBuilder.DefineEvent(@event.Name, @event.Attributes, @event.EventHandlerType);

            if (addMethod != null)
            {
                newEvent.SetAddOnMethod(addMethod);
            }

            if (removeMethod != null)
            {
                newEvent.SetRemoveOnMethod(removeMethod);
            }
        }

        private MethodBuilder OverrideEventMethod(MethodInfo method, int count)
        {
            return method == null
                ? null
                : new InterfaceMethodOverride(
                    this.typeBuilder,
                    this.proxyInterceptionPipelineField,
                    this.targetField,
                    method,
                    this.targetInterface,
                    this.genericParameterMapper,
                    this.explicitImplementation,
                    count)
                    .AddMethod();
        }
    }

    internal class GenericParameterMapper
    {
        private Type extendedInterface;
        private GenericParameterMapper genericParameterMapper;

        public GenericParameterMapper(Type extendedInterface, GenericParameterMapper genericParameterMapper)
        {
            this.extendedInterface = extendedInterface;
            this.genericParameterMapper = genericParameterMapper;
        }

        public static GenericParameterMapper DefaultMapper { get; internal set; }

        internal Type Map(Type t)
        {
            throw new NotImplementedException();
        }
    }
}
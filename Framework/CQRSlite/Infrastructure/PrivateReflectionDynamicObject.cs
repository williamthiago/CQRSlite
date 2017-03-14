﻿using System;
using System.Collections.Concurrent;
using System.Dynamic;
using System.Reflection;
using System.Linq;


namespace CQRSlite.Infrastructure
{
    internal class PrivateReflectionDynamicObject : DynamicObject
    {
        private const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly ConcurrentDictionary<int, MethodInfo> cachedMembers = new ConcurrentDictionary<int, MethodInfo>();

        public object RealObject { get; set; }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            var type = RealObject.GetType();
            var hash = 719 + type.GetHashCode();
            var argtypes = new Type[args.Length];
            for (var i = 0; i < args.Length; i++)
            {
                var argtype = args[i].GetType();
                argtypes[i] = argtype;
                hash = hash * 31 + argtype.GetHashCode();
            }
            var method = cachedMembers.GetOrAdd(hash, x => GetMember(type, binder.Name, argtypes));
            result = method?.Invoke(RealObject, args);
            return true;
        }

        private static MethodInfo GetMember(Type type, string name, Type[] argtypes)
        {
            while (true)
            {
                var member = type.GetMethods(bindingFlags)
                    .FirstOrDefault(m => m.Name == name && m.GetParameters()
                                             .Select(p => p.ParameterType).SequenceEqual(argtypes));

                if (member != null)
                {
                    return member;
                }
                var t = type.GetTypeInfo().BaseType;
                if (t == null)
                {
                    return null;
                }
                type = t;
            }
        }
    }
}

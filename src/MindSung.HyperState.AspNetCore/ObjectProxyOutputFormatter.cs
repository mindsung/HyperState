﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MindSung.HyperState.AspNetCore
{
    internal class ObjectProxyOutputFormatter<TSerialized, TFactory> : IOutputFormatter
        where TFactory : IWebObjectProxyFactory<TSerialized>
    {
        public ObjectProxyOutputFormatter(TFactory factory, MvcOptions options)
        {
            this.factory = factory;
            this.options = options;
        }

        private TFactory factory;
        private MvcOptions options;
        private ConcurrentDictionary<Type, SerializedInvoker> invokers = new ConcurrentDictionary<Type, SerializedInvoker>();

        class SerializedInvoker
        {
            public SerializedInvoker(Type genericType)
            {
                proxyType = typeof(IObjectProxy<,>).MakeGenericType(genericType, typeof(TSerialized));
                getSerializedMethod = proxyType.GetTypeInfo().GetDeclaredProperty("Serialized").GetMethod;
            }

            Type proxyType;
            MethodInfo getSerializedMethod;

            public TSerialized GetSerialized(object proxy)
            {
                return (TSerialized)getSerializedMethod.Invoke(proxy, null);
            }
        }

        public bool CanWriteResult(OutputFormatterCanWriteContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            var type = context.ObjectType;
            if (type == null)
            {
                return false;
            }
            // We'll format the object if it is an object proxy or if no other registered formatters will handle it.
            if (type.GetTypeInfo().GetInterfaces().Any(i =>
            {
                return i.GenericTypeArguments.Length == 2 && i.GenericTypeArguments[1] == typeof(TSerialized)
                    && i == typeof(IObjectProxy<,>).MakeGenericType(i.GenericTypeArguments[0], typeof(TSerialized));
            }))
            {
                invokers.TryAdd(type, new SerializedInvoker(type.GetTypeInfo().GenericTypeArguments[0]));
                return true;
            }
            if (!options.OutputFormatters.Where(f => f.GetType() != GetType()).Any(f => f.CanWriteResult(context)))
            {
                return true;
            }
            return false;
        }

        public async Task WriteAsync(OutputFormatterWriteContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            if (context.ObjectType == null)
            {
                throw new ArgumentNullException(nameof(context.ObjectType));
            }
            TSerialized serialized;
            SerializedInvoker invoker;
            if (invokers.TryGetValue(context.ObjectType, out invoker))
            {
                serialized = invoker.GetSerialized(context.Object);
            }
            else
            {
                serialized = factory.Serializer.Serialize(context.Object, context.ObjectType);
            }
            await factory.WriteSerialized(context.HttpContext.Response, serialized);
        }
    }
}

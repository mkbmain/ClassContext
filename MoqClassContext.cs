using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Moq;

namespace Tests
{
    public abstract class ClassContext<T> where T : class
    {
        private T _sut = null;
        protected T Sut => _sut ??= Resolve();

        private class BuiltMocks
        {
            public object Mock { get; set; }
            public object MockValue { get; set; }
        }

        private Dictionary<Type, BuiltMocks> _mocks = new Dictionary<Type, BuiltMocks>();


        protected Mock<TE> MockOf<TE>() where TE : class
        {
            if (_mocks.ContainsKey(typeof(TE)))
            {
                return (Mock<TE>) _mocks[typeof(TE)].Mock;
            }

            Mock<TE> mock = null;
            if (typeof(TE).IsClass)
            {
                var parameters = GetParamInfoForConstructorOfType<TE>( false);

                if (parameters.Length > 0)
                {
                    var items = parameters.Select(t => GetDefault(t.ParameterType)).ToArray();
                    mock = new Mock<TE>(items);
                }
            }
            else
            {
                mock = new Mock<TE>();
            }


            _mocks.Add(typeof(TE), new BuiltMocks
            {
                Mock = mock,
                MockValue = mock.Object
            });
            return MockOf<TE>();
        }

        private static object GetDefault(Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        private T Resolve()
        {
            var allTypes = GetParamInfoForConstructorOfType<T>()
                .Select(t => t.ParameterType)
                .Select(t => _mocks.ContainsKey(t) ? _mocks[t].MockValue : null)
                .ToArray();

            return allTypes.Any()
                ? (T) Activator.CreateInstance(typeof(T), allTypes)
                : (T) Activator.CreateInstance(typeof(T));
        }

        private static ParameterInfo[] GetParamInfoForConstructorOfType<TE>(bool largest = true)
        {
            return GetParamInfoForConstructorOfType(typeof(TE), largest);
        }

        private static ParameterInfo[] GetParamInfoForConstructorOfType(Type type, bool largest = true)
        {
            var entryPoints = type.GetConstructors().Where(t => t.IsPublic);
            var part = largest
                ? entryPoints.OrderByDescending(t => t.GetParameters().Length)
                : entryPoints.OrderBy(t => t.GetParameters().Length);

            return part.FirstOrDefault()?.GetParameters() ?? Array.Empty<ParameterInfo>();
        }
    }
}

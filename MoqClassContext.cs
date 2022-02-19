using System;
using System.Collections.Generic;
using System.Linq;
using Moq;

namespace Tests
{
    public abstract class ClassContext<T> where T : class
    {
        private T _sut = null;
        public T Sut => _sut ??= Resolve();

        private Dictionary<Type, object> _mocks = new Dictionary<Type, object>();
        private Dictionary<Type, object> _mocks2 = new Dictionary<Type, object>();

        public Mock<TE> MockOf<TE>() where TE : class
        {
            if (_mocks.ContainsKey(typeof(TE)))
            {
                return (Mock<TE>) _mocks[typeof(TE)];
            }

            Mock<TE> mock = new Mock<TE>();
            if (typeof(TE).IsClass)
            {
                var smallest = typeof(TE).GetConstructors().Where(t => t.IsPublic)
                    .OrderBy(t => t.GetParameters().Length)
                    .FirstOrDefault();

                if (smallest.GetParameters().Length > 0)
                {
                    var par = smallest.GetParameters().Select(t => t.ParameterType);
                    var items = par.Select(GetDefault).ToArray();
                    mock = new Mock<TE>(items);
                }
            }


            _mocks.Add(typeof(TE), mock);
            _mocks2.Add(typeof(TE), mock.Object);
            return MockOf<TE>();
        }

        private static object GetDefault(Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }

            return null;
        }

        private T Resolve()
        {
            var biggestEntryPoint = typeof(T).GetConstructors().Where(t => t.IsPublic)
                .OrderByDescending(t => t.GetParameters().Length)
                .FirstOrDefault();

            var par = biggestEntryPoint.GetParameters();
            var allTypes = par.Select(t => t.ParameterType).Where(t => t != null)
                .ToArray();

            var types = allTypes.Select(t => _mocks2.ContainsKey(t) ? _mocks2[t] : null).ToArray();
            return (T) Activator.CreateInstance(typeof(T), types);
        }
    }
}

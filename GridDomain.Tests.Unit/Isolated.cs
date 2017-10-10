﻿using System;

namespace GridDomain.Tests.Unit
{
    public sealed class Isolated<T> : IDisposable where T:MarshalByRefObject
    {
        private AppDomain _domain;
        public T Value { get; private set; }

        public Isolated()
        {
            _domain = AppDomain.CreateDomain("Isolated:" + Guid.NewGuid(),
                                             null,
                                             AppDomain.CurrentDomain.SetupInformation);

            Type type = typeof(T);
            Value = (T) _domain.CreateInstanceAndUnwrap(type.Assembly.FullName, type.FullName);
        }

        public void Dispose()
        {
            if (Value is IDisposable disposable)
            {
                disposable.Dispose();
                Value = null;
            }

            if (_domain == null) return;
            AppDomain.Unload(_domain);
            _domain = null;
        }
    }
}
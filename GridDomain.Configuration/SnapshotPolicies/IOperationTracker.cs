﻿namespace GridDomain.Configuration {
    public interface IOperationTracker<T>
    {
        int  InProgress { get; }
        void Start(T criteria);
        void Complete(T instance);
        void Fail(T instance);
                               
    }
}
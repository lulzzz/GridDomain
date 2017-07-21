﻿using GridDomain.Common;

namespace GridDomain.Routing
{
    public interface IPublisher
    {
        void Publish(object msg);
        void Publish(object msg, IMessageMetadata metadata);
    }
}
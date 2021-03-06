using System;

namespace GridDomain.EventSourcing.VersionedTypeSerialization
{
    internal class EmptyTypeNameException : Exception
    {
        public EmptyTypeNameException()
        {
            
        }
        public EmptyTypeNameException(string[] versionedTypeParts)
        {
            VersionedTypeParts = versionedTypeParts;
        }

        public string[] VersionedTypeParts { get; }
    }
}
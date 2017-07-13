using Microsoft.Practices.Unity;

namespace GridDomain.Common
{
    public static class ContainerExtensions
    {
        public static void Register(this IUnityContainer container, IContainerConfiguration configuration)
        {
            configuration.Register(container);
        }
    }
}
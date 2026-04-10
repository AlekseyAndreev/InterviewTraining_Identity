using System.Threading;

namespace Order.BusinessLogic.Managers.Tests
{
    public abstract class BaseUnitTests
    {
        protected CancellationToken Token;

        protected BaseUnitTests()
        {
            Token = new CancellationTokenSource().Token;
        }
    }
}

using System.Threading;

namespace SevSharks.Identity.Tests
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

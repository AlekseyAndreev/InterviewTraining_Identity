using Duende.IdentityServer.EntityFramework.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace SevSharks.Identity.DataAccess
{
    public class AppPersistedGrantDbContext : PersistedGrantDbContext
    {
        public AppPersistedGrantDbContext(DbContextOptions<PersistedGrantDbContext> options) : base(options)
        {
        }
    }
}

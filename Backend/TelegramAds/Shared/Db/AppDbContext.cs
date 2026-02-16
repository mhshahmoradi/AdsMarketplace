using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TelegramAds.Shared.Outbox;

namespace TelegramAds.Shared.Db;

public sealed class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<Channel> Channels => Set<Channel>();
    public DbSet<ChannelAdmin> ChannelAdmins => Set<ChannelAdmin>();
    public DbSet<ChannelStats> ChannelStats => Set<ChannelStats>();
    public DbSet<Listing> Listings => Set<Listing>();
    public DbSet<ListingAdFormat> ListingAdFormats => Set<ListingAdFormat>();
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<CampaignApplication> CampaignApplications => Set<CampaignApplication>();
    public DbSet<ListingApplication> ListingApplications => Set<ListingApplication>();
    public DbSet<Deal> Deals => Set<Deal>();
    public DbSet<DealProposal> DealProposals => Set<DealProposal>();
    public DbSet<DealEvent> DealEvents => Set<DealEvent>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<EscrowBalance> EscrowBalances => Set<EscrowBalance>();
    public DbSet<Withdrawal> Withdrawals => Set<Withdrawal>();
    public DbSet<PaymentWebhook> PaymentWebhooks => Set<PaymentWebhook>();
    public DbSet<DealEscrowTransaction> DealEscrowTransactions => Set<DealEscrowTransaction>();
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        builder.Entity<ApplicationUser>(b =>
        {
            b.HasIndex(u => u.TgUserId).IsUnique();
        });
    }
}

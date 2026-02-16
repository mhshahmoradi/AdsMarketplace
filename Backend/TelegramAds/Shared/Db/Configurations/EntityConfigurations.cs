using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TelegramAds.Shared.Outbox;

namespace TelegramAds.Shared.Db.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.MessageType).HasMaxLength(256).IsRequired();
        builder.Property(x => x.PayloadJson).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.LastError).HasMaxLength(2000);
        builder.HasIndex(x => new { x.Status, x.NextRetryAt });
    }
}

public sealed class ChannelConfiguration : IEntityTypeConfiguration<Channel>
{
    public void Configure(EntityTypeBuilder<Channel> builder)
    {
        builder.ToTable("channels");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Username).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.PhotoUrl).HasMaxLength(512);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.HasIndex(x => x.TgChannelId).IsUnique();
        builder.HasIndex(x => x.Username).IsUnique();
        builder.HasOne(x => x.OwnerUser).WithMany().HasForeignKey(x => x.OwnerUserId);
    }
}

public sealed class ChannelAdminConfiguration : IEntityTypeConfiguration<ChannelAdmin>
{
    public void Configure(EntityTypeBuilder<ChannelAdmin> builder)
    {
        builder.ToTable("channel_admins");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.ChannelId, x.TgUserId }).IsUnique();
        builder.HasOne(x => x.Channel).WithMany(c => c.Admins).HasForeignKey(x => x.ChannelId);
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
    }
}

public sealed class ChannelStatsConfiguration : IEntityTypeConfiguration<ChannelStats>
{
    public void Configure(EntityTypeBuilder<ChannelStats> builder)
    {
        builder.ToTable("channel_stats");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PrimaryLanguage).HasMaxLength(16);
        builder.HasIndex(x => x.ChannelId).IsUnique();
        builder.HasOne(x => x.Channel).WithOne(c => c.Stats).HasForeignKey<ChannelStats>(x => x.ChannelId);
    }
}

public sealed class ListingConfiguration : IEntityTypeConfiguration<Listing>
{
    public void Configure(EntityTypeBuilder<Listing> builder)
    {
        builder.ToTable("listings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.HasOne(x => x.Channel).WithMany(c => c.Listings).HasForeignKey(x => x.ChannelId);
    }
}

public sealed class ListingAdFormatConfiguration : IEntityTypeConfiguration<ListingAdFormat>
{
    public void Configure(EntityTypeBuilder<ListingAdFormat> builder)
    {
        builder.ToTable("listing_ad_formats");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FormatType).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.PriceInTon).HasPrecision(18, 9);
        builder.Property(x => x.Terms).HasMaxLength(1000);
        builder.HasOne(x => x.Listing).WithMany(l => l.AdFormats).HasForeignKey(x => x.ListingId);
    }
}

public sealed class CampaignConfiguration : IEntityTypeConfiguration<Campaign>
{
    public void Configure(EntityTypeBuilder<Campaign> builder)
    {
        builder.ToTable("campaigns");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Brief).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.BudgetInTon).HasPrecision(18, 9);
        builder.Property(x => x.TargetLanguages).HasMaxLength(256);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.HasOne(x => x.AdvertiserUser).WithMany().HasForeignKey(x => x.AdvertiserUserId);
    }
}

public sealed class CampaignApplicationConfiguration : IEntityTypeConfiguration<CampaignApplication>
{
    public void Configure(EntityTypeBuilder<CampaignApplication> builder)
    {
        builder.ToTable("campaign_applications");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProposedPriceInTon).HasPrecision(18, 9);
        builder.Property(x => x.Message).HasMaxLength(2000);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.HasOne(x => x.Campaign).WithMany(c => c.Applications).HasForeignKey(x => x.CampaignId);
        builder.HasOne(x => x.Channel).WithMany().HasForeignKey(x => x.ChannelId);
        builder.HasOne(x => x.ApplicantUser).WithMany().HasForeignKey(x => x.ApplicantUserId);
        builder.HasIndex(x => new { x.CampaignId, x.ChannelId }).IsUnique();
    }
}

public sealed class ListingApplicationConfiguration : IEntityTypeConfiguration<ListingApplication>
{
    public void Configure(EntityTypeBuilder<ListingApplication> builder)
    {
        builder.ToTable("listing_applications");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProposedPriceInTon).HasPrecision(18, 9);
        builder.Property(x => x.Message).HasMaxLength(2000);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.HasOne(x => x.Listing).WithMany().HasForeignKey(x => x.ListingId);
        builder.HasOne(x => x.Campaign).WithMany().HasForeignKey(x => x.CampaignId);
        builder.HasOne(x => x.ApplicantUser).WithMany().HasForeignKey(x => x.ApplicantUserId);
        builder.HasIndex(x => new { x.ListingId, x.CampaignId }).IsUnique();
    }
}

public sealed class DealConfiguration : IEntityTypeConfiguration<Deal>
{
    public void Configure(EntityTypeBuilder<Deal> builder)
    {
        builder.ToTable("deals");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.AgreedPriceInTon).HasPrecision(18, 9);
        builder.Property(x => x.AdFormat).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.EscrowAddress).HasMaxLength(128);
        builder.Property(x => x.CreativeText).HasMaxLength(4096);
        builder.Property(x => x.CreativeRejectionReason).HasMaxLength(1000);
        builder.HasOne(x => x.Listing).WithMany().HasForeignKey(x => x.ListingId);
        builder.HasOne(x => x.Channel).WithMany().HasForeignKey(x => x.ChannelId);
        builder.HasOne(x => x.AdvertiserUser).WithMany().HasForeignKey(x => x.AdvertiserUserId);
        builder.HasOne(x => x.ChannelOwnerUser).WithMany().HasForeignKey(x => x.ChannelOwnerUserId);
        builder.HasOne(x => x.Campaign).WithMany().HasForeignKey(x => x.CampaignId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.CampaignApplication).WithMany().HasForeignKey(x => x.CampaignApplicationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ListingApplication).WithMany().HasForeignKey(x => x.ListingApplicationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Payment).WithOne(p => p.Deal).HasForeignKey<Deal>(x => x.PaymentId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DealProposalConfiguration : IEntityTypeConfiguration<DealProposal>
{
    public void Configure(EntityTypeBuilder<DealProposal> builder)
    {
        builder.ToTable("deal_proposals");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProposedPriceInTon).HasPrecision(18, 9);
        builder.Property(x => x.Terms).HasMaxLength(2000);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.HasOne(x => x.Deal).WithMany(d => d.Proposals).HasForeignKey(x => x.DealId);
        builder.HasOne(x => x.ProposerUser).WithMany().HasForeignKey(x => x.ProposerUserId);
    }
}

public sealed class DealEventConfiguration : IEntityTypeConfiguration<DealEvent>
{
    public void Configure(EntityTypeBuilder<DealEvent> builder)
    {
        builder.ToTable("deal_events");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FromStatus).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.ToStatus).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.EventType).HasConversion<string>().HasMaxLength(64);
        builder.HasOne(x => x.Deal).WithMany(d => d.Events).HasForeignKey(x => x.DealId);
        builder.HasOne(x => x.ActorUser).WithMany().HasForeignKey(x => x.ActorUserId);
        builder.HasIndex(x => x.DealId);
    }
}

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.InvoiceReference).HasMaxLength(50).IsRequired();
        builder.Property(x => x.InvoiceText).HasMaxLength(500).IsRequired();
        builder.Property(x => x.PaymentUrl).HasMaxLength(1024).IsRequired();
        builder.Property(x => x.AmountInTon).HasPrecision(18, 9);
        builder.Property(x => x.ActualAmountInTon).HasPrecision(18, 9);
        builder.Property(x => x.Currency).HasMaxLength(10);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.TransactionHash).HasMaxLength(255);
        builder.Property(x => x.PaidByAddress).HasMaxLength(255);
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.HasIndex(x => x.InvoiceReference).IsUnique();
        builder.HasIndex(x => x.TransactionHash);
        builder.HasIndex(x => x.DealId);
        builder.HasIndex(x => x.Status);
        builder.HasOne(x => x.Deal).WithOne(d => d.Payment).HasForeignKey<Payment>(x => x.DealId);
    }
}

public sealed class EscrowBalanceConfiguration : IEntityTypeConfiguration<EscrowBalance>
{
    public void Configure(EntityTypeBuilder<EscrowBalance> builder)
    {
        builder.ToTable("escrow_balances");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TotalEarnedInTon).HasPrecision(18, 9);
        builder.Property(x => x.AvailableBalanceInTon).HasPrecision(18, 9);
        builder.Property(x => x.LockedInDealsInTon).HasPrecision(18, 9);
        builder.Property(x => x.WithdrawnInTon).HasPrecision(18, 9);
        builder.Property(x => x.Currency).HasMaxLength(10);
        builder.HasIndex(x => new { x.UserId, x.Currency }).IsUnique();
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
    }
}

public sealed class WithdrawalConfiguration : IEntityTypeConfiguration<Withdrawal>
{
    public void Configure(EntityTypeBuilder<Withdrawal> builder)
    {
        builder.ToTable("withdrawals");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AmountInTon).HasPrecision(18, 9);
        builder.Property(x => x.FeeInTon).HasPrecision(18, 9);
        builder.Property(x => x.NetAmountInTon).HasPrecision(18, 9);
        builder.Property(x => x.Currency).HasMaxLength(10);
        builder.Property(x => x.DestinationAddress).HasMaxLength(255).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.TransactionHash).HasMaxLength(255);
        builder.Property(x => x.RejectionReason).HasMaxLength(1000);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.CreatedAt);
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        builder.HasOne(x => x.ReviewedByUser).WithMany().HasForeignKey(x => x.ReviewedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PaymentWebhookConfiguration : IEntityTypeConfiguration<PaymentWebhook>
{
    public void Configure(EntityTypeBuilder<PaymentWebhook> builder)
    {
        builder.ToTable("payment_webhooks");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.InvoiceId).HasMaxLength(255).IsRequired();
        builder.Property(x => x.Payload).IsRequired();
        builder.Property(x => x.ErrorMessage).HasMaxLength(2000);
        builder.HasIndex(x => x.InvoiceId);
        builder.HasIndex(x => x.Processed);
    }
}

public sealed class DealEscrowTransactionConfiguration : IEntityTypeConfiguration<DealEscrowTransaction>
{
    public void Configure(EntityTypeBuilder<DealEscrowTransaction> builder)
    {
        builder.ToTable("deal_escrow_transactions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TransactionType).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.AmountInTon).HasPrecision(18, 9);
        builder.HasIndex(x => x.DealId);
        builder.HasIndex(x => x.TransactionType);
        builder.HasOne(x => x.Deal).WithMany(d => d.EscrowTransactions).HasForeignKey(x => x.DealId);
        builder.HasOne(x => x.FromUser).WithMany().HasForeignKey(x => x.FromUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ToUser).WithMany().HasForeignKey(x => x.ToUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Payment).WithMany(p => p.Transactions).HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Withdrawal).WithMany(w => w.Transactions).HasForeignKey(x => x.WithdrawalId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("chat_messages");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.DealId);
        builder.HasIndex(x => x.SenderUserId);
        builder.HasOne(x => x.Deal).WithMany().HasForeignKey(x => x.DealId);
        builder.HasOne(x => x.SenderUser).WithMany().HasForeignKey(x => x.SenderUserId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.ReceiverUser).WithMany().HasForeignKey(x => x.ReceiverUserId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class WalletConfiguration : IEntityTypeConfiguration<Wallet>
{
    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        builder.ToTable("wallets");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Balance).HasPrecision(18, 9).HasDefaultValue(0);
        builder.HasIndex(x => x.UserId).IsUnique();
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
    }
}

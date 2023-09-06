using CsvHelper.Configuration.Attributes;

namespace CosmosDBWatcher.Models
{
    internal class CostData
    {
        public string InvoiceSectionName { get; set; }
        public string AccountName { get; set; }
        public string AccountOwnerId { get; set; }
        public string SubscriptionId { get; set; }
        public string SubscriptionName { get; set; }
        public string ResourceGroup { get; set; }
        public string ResourceLocation { get; set; }
        public string Date { get; set; }
        public string ProductName { get; set; }
        public string MeterCategory { get; set; }
        public string MeterSubCategory { get; set; }
        public string MeterId { get; set; }
        public string MeterName { get; set; }
        public string MeterRegion { get; set; }
        public string UnitOfMeasure { get; set; }
        public string Quantity { get; set; }
        public string EffectivePrice { get; set; }
        public string CostInBillingCurrency { get; set; }
        public string CostCenter { get; set; }
        public string ConsumedService { get; set; }
        public string ResourceId { get; set; }
        public string Tags { get; set; }
        public string OfferId { get; set; }
        public string AdditionalInfo { get; set; }
        public string ServiceInfo1 { get; set; }
        public string ServiceInfo2 { get; set; }
        public string ResourceName { get; set; }
        public string ReservationId { get; set; }
        public string ReservationName { get; set; }
        public string UnitPrice { get; set; }
        public string ProductOrderId { get; set; }
        public string ProductOrderName { get; set; }
        public string Term { get; set; }
        public string PublisherType { get; set; }
        public string PublisherName { get; set; }
        public string ChargeType { get; set; }
        public string Frequency { get; set; }
        public string PricingModel { get; set; }
        public string AvailabilityZone { get; set; }
        public string BillingAccountId { get; set; }
        public string BillingAccountName { get; set; }
        public string BillingCurrencyCode { get; set; }
        public string BillingPeriodStartDate { get; set; }
        public string BillingPeriodEndDate { get; set; }
        public string BillingProfileId { get; set; }
        public string BillingProfileName { get; set; }
        public string InvoiceSectionId { get; set; }
        public string IsAzureCreditEligible { get; set; }
        public string PartNumber { get; set; }
        public string PayGPrice { get; set; }
        public string PlanName { get; set; }
        public string ServiceFamily { get; set; }
        public string CostAllocationRuleName { get; set; }

        public string benefitId { get; set; }
        public string benefitName { get; set; }

        [Ignore]
        public CostDataAdditionalInfo AdditionalInfoObject { get; set; }
    }

    public class CostDataAdditionalInfo
    {
        public string GlobalDatabaseAccountName { get; set; }
        public string CollectionName { get; set; }
        public string CollectionRid { get; set; }
        public string DatabaseName { get; set; }
        public string UsageType { get; set; }
        public string Region { get; set; }
        public string AutoscaleVersion { get; set; }
    }
}

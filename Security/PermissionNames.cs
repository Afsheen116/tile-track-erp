using System.Collections.Generic;

namespace CeramicERP.Security
{
    public static class PermissionNames
    {
        public const string ViewDashboard = "view_dashboard";
        public const string ViewDashboardFinancial = "view_dashboard_financial";
        public const string ViewSales = "view_sales";
        public const string CreateSale = "create_sale";
        public const string EditSale = "edit_sale";
        public const string DeleteSale = "delete_sale";
        public const string ViewPurchases = "view_purchases";
        public const string CreatePurchase = "create_purchase";
        public const string EditPurchase = "edit_purchase";
        public const string DeletePurchase = "delete_purchase";
        public const string ViewInventory = "view_inventory";
        public const string ManageInventory = "manage_inventory";
        public const string ViewReports = "view_reports";
        public const string ViewProfit = "view_profit";
        public const string ViewLedger = "view_ledger";
        public const string ExportLedger = "export_ledger";
        public const string ManagePayments = "manage_payments";
        public const string ViewCustomersSuppliers = "view_customers_suppliers";
        public const string ManageUsers = "manage_users";
        public const string SystemSettings = "system_settings";

        public static IReadOnlyList<PermissionSeedItem> AllDefinitions { get; } = new List<PermissionSeedItem>
        {
            new(ViewDashboard, "Access business dashboard."),
            new(ViewDashboardFinancial, "Access financial dashboard cards."),
            new(ViewSales, "View sales records."),
            new(CreateSale, "Create sales invoices."),
            new(EditSale, "Edit sales invoices."),
            new(DeleteSale, "Delete sales invoices."),
            new(ViewPurchases, "View purchase records."),
            new(CreatePurchase, "Create purchase entries."),
            new(EditPurchase, "Edit purchase entries."),
            new(DeletePurchase, "Delete purchase entries."),
            new(ViewInventory, "View product and stock data."),
            new(ManageInventory, "Create or delete product/category data."),
            new(ViewReports, "View business reports."),
            new(ViewProfit, "View profit and margin metrics."),
            new(ViewLedger, "Open customer/supplier ledger screens."),
            new(ExportLedger, "Export ledger statements."),
            new(ManagePayments, "Record and manage payment flows."),
            new(ViewCustomersSuppliers, "View customer and supplier directories."),
            new(ManageUsers, "Create/update user roles."),
            new(SystemSettings, "Access system settings.")
        };
    }

    public sealed record PermissionSeedItem(string Name, string Description);
}

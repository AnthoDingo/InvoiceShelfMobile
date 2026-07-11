namespace InvoiceShelf.Constants
{
    public static class ApiUri
    {
        #region Misc
        public static string Version  => "/api/v1/app/version";
        #endregion

        #region Auth
        public static string Login      => "/api/v1/auth/login";
        public static string Logout     => "/api/v1/auth/logout";
        public static string CheckToken => "/api/v1/auth/check";
        public static string Me         => "/api/v1/me";
        #endregion

        #region Invoices
        public static string AllInvoices              => "/api/v1/invoices";
        public static string Invoice(int id)          => $"/api/v1/invoices/{id}";
        #endregion

        #region Estimates
        public static string AllEstimates             => "/api/v1/estimates";
        public static string Estimate(int id)         => $"/api/v1/estimates/{id}";
        #endregion

        #region Customers
        public static string AllCustomers             => "/api/v1/customers";
        public static string Customer(int id)         => $"/api/v1/customers/{id}";
        #endregion

        #region Payments
        public static string AllPayments              => "/api/v1/payments";
        public static string Payment(int id)          => $"/api/v1/payments/{id}";
        public static string AllPaymentMethods         => "/api/v1/payment-methods";
        #endregion

        #region Misc (suite)
        /// <summary>Numéro suivant auto-généré pour un modèle donné (ex. "payment").</summary>
        public static string NextNumber(string key)   => $"/api/v1/next-number?key={key}";
        #endregion

        #region Items (catalogue d'articles)
        public static string AllItems => "/api/v1/items";
        #endregion

        #region Templates
        public static string InvoiceTemplates => "/api/v1/invoices/templates";
        #endregion

        #region Champs personnalisés
        /// <summary>Liste les définitions de champs personnalisés pour un type de modèle donné (ex. "Invoice").</summary>
        public static string CustomFields(string modelType) => $"/api/v1/custom-fields?type={modelType}&limit=all";
        #endregion

        #region Expenses
        public static string AllExpenses              => "/api/v1/expenses";
        public static string Expense(int id)          => $"/api/v1/expenses/{id}";
        #endregion
    }
}

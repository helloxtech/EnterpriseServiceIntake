namespace ServiceIntake.Plugins
{
    internal static class Constants
    {
        internal const string ServiceRequest = "hx_servicerequest";
        internal const string RoutingRule = "hx_routingrule";
        internal const string SlaPolicy = "hx_slapolicy";
        internal const string ServiceDocument = "hx_servicedocument";

        internal const int SeverityCritical = 752630003;
        internal const int LifecycleSubmitted = 752630001;
        internal const int LifecycleResolved = 752630007;
        internal const int LifecycleClosed = 752630008;
        internal const int ApprovalNotRequired = 752630000;
        internal const int ApprovalPending = 752630001;
        internal const int SyncNotStarted = 752630000;
        internal const int DocumentTypeResolution = 752630001;
    }
}

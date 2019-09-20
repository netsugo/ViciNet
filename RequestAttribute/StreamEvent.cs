namespace ViciNet.RequestAttribute
{
    public enum StreamEvent
    {
        [RequestName("log")] Log,
        [RequestName("control-log")] ControlLog,

        [RequestName("list-sa")] ListSa,
        [RequestName("list-policy")] ListPolicy,
        [RequestName("list-conn")] ListConn,
        [RequestName("list-cert")] ListCert,
        [RequestName("list-authority")] ListAuthority,

        [RequestName("ike-updown")] IkeUpDown,
        [RequestName("child-updown")] ChildUpDown,

        [RequestName("ike-rekey")] IkeReKey,
        [RequestName("child-rekey")] ChildReKey
    }
}
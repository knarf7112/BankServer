
namespace AsyncConnection
{
    /// <summary>
    /// Socket connect status
    /// </summary>
    public enum ConnectStatus
    {
        /// <summary>
        /// Not initial
        /// </summary>
        None = 0,
        /// <summary>
        /// initial Socket
        /// </summary>
        Inited = 1,
        /// <summary>
        /// Connect has Error
        /// </summary>
        ConnectError = 2,
        /// <summary>
        /// Current Socket Status
        /// </summary>
        Connected = 3,
        /// <summary>
        /// Current Socket Status
        /// </summary>
        NotConnect = 4
    }
}

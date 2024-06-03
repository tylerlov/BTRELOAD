// Microsoft
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

// Unity
using UnityEngine;

namespace GUPS.EasyPerformanceMonitor.Provider
{
    /// <summary>
    /// Fetches the upload speed in bytes per second.
    /// </summary>
    /// <remarks>
    /// The <see cref="UploadSpeedProvider"/> class is responsible for fetching and providing information about the upload speed
    /// in bytes per second. This component is supported on all platforms, and it uses network interface statistics to calculate
    /// the upload speed.
    /// </remarks>
    public class UploadSpeedProvider : APerformanceProvider
    {
        /// <summary>
        /// The performance component name.
        /// </summary>
        public const String CName = "Upload";

        /// <summary>
        /// Returns the performance component name.
        /// </summary>
        public override string Name
        {
            get
            {
                return CName;
            }
        }

        /// <summary>
        /// Returns true if the performance component is supported on the current platform.
        /// The upload speed component is supported on all platforms.
        /// </summary>
        public override bool IsSupported
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// The default unit is bytes.
        /// </summary>
        public override String Unit
        {
            get
            {
                return "B";
            }
        }

        /// <summary>
        /// The last total bytes sent value.
        /// </summary>
        private Int64 lastTotalBytesSent = 0;

        /// <summary>
        /// Stores the last bytes sent.
        /// </summary>
        private float lastBytesSent = 0;

        /// <summary>
        /// Initialize the performance provider and get the network interfaces statistics.
        /// </summary>
        protected override void Awake()
        {
            // Call base method.
            base.Awake();

            // Get the last bytes sent value.
            this.lastTotalBytesSent = this.SumTotalBytesSent();

            // Update the received bytes twice a second.
            this.InvokeRepeating("UpdateSentBytes", 0, 0.5f);
        }

        /// <summary>
        /// Find all network interfaces of the device.
        /// </summary>
        /// <returns>Finds all network interfaces of the device.</returns>
        private List<NetworkInterface> GetNetworkInterfaces()
        {
            List<NetworkInterface> var_NetworkInterfaces = new List<NetworkInterface>();

            try
            {
                // Get all network interfaces.
                NetworkInterface[] var_NetworkInterfacesArray = NetworkInterface.GetAllNetworkInterfaces();

                if (var_NetworkInterfacesArray != null)
                {
                    var_NetworkInterfaces.AddRange(var_NetworkInterfacesArray);
                }
            }
            catch
            {
                // Just catch all exceptions and ignore them.
            }

            return var_NetworkInterfaces;
        }

        /// <summary>
        /// Sum up the total bytes sent.
        /// </summary>
        private Int64 SumTotalBytesSent()
        {
            // Get all network interfaces.
            List<NetworkInterface> var_NetworkInterfaces = this.GetNetworkInterfaces();

            // The total bytes sent.
            Int64 var_BytesSent = 0;

            // Get the statistics for each network interface that is up and not a loopback.
            foreach (var var_NetworkInterface in var_NetworkInterfaces)
            {
                if(var_NetworkInterface == null)
                {
                    continue;
                }

                try
                {
                    if (var_NetworkInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                        var_NetworkInterface.OperationalStatus == OperationalStatus.Up)
                    {
                        try
                        {
                            // First try newer statistics IPv6 + IPv4...
                            IPInterfaceStatistics var_Statistic = var_NetworkInterface.GetIPStatistics();

                            if (var_Statistic != null)
                            {
                                var_BytesSent += var_Statistic.BytesSent;
                            }
                        }
                        catch
                        {
                            // ... on fail try older statistics IPv4 only.
                            IPv4InterfaceStatistics var_OldStatistic = var_NetworkInterface.GetIPv4Statistics();

                            if (var_OldStatistic != null)
                            {
                                var_BytesSent += var_OldStatistic.BytesSent;
                            }
                        }
                    }
                }
                catch
                {
                    // Catch all exceptions and ignore them.
                }
            }

            return var_BytesSent;
        }

        /// <summary>
        /// Calculate on a fixed interval, because this is 'performance intensive'.
        /// </summary>
        private void UpdateSentBytes()
        {
            // Start as async task, because this is 'performance intensive' and we don't want to block the main thread. Also sync is not needed here.
            Task.Run(() =>
            {
                // Get the next total sent bytes.
                Int64 var_TotalBytesSent = this.SumTotalBytesSent();

                // Calculate the difference between the current and last sent bytes.
                this.lastBytesSent = Mathf.Max(0, var_TotalBytesSent - this.lastTotalBytesSent);

                // Store the current total sent bytes.
                this.lastTotalBytesSent = var_TotalBytesSent;
            });
        }

        /// <summary>
        /// Calculates the current upload rate and adds it to the graph.
        /// </summary>
        /// <returns></returns>
        protected override float GetNextValue()
        {
            // Return the current upload rate.
            return this.lastBytesSent;
        }
    }
}

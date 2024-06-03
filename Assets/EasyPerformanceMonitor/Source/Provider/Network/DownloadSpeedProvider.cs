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
    /// Fetches the download speed in bytes per second.
    /// </summary>
    /// <remarks>
    /// The <see cref="DownloadSpeedProvider"/> class is responsible for fetching and providing information about the download speed
    /// in bytes per second. This component is supported on all platforms, and it uses network interface statistics to calculate
    /// the download speed.
    /// </remarks>
    public class DownloadSpeedProvider : APerformanceProvider
    {
        /// <summary>
        /// The performance component name.
        /// </summary>
        public const String CName = "Download";

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
        /// The Download Speed component is supported on all platforms.
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
        /// The last total bytes received value.
        /// </summary>
        private Int64 lastTotalBytesReceived = 0;

        /// <summary>
        /// Stores the last bytes received.
        /// </summary>
        private float lastBytesReceived = 0;

        /// <summary>
        /// Initialize the performance provider and get the network interfaces statistics.
        /// </summary>
        protected override void Awake()
        {
            // Call base method.
            base.Awake();

            // Get the last bytes received value.
            this.lastTotalBytesReceived = this.SumTotalBytesReceived();

            // Update the received bytes twice a second.
            this.InvokeRepeating("UpdateReceivedBytes", 0, 0.5f);
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
        /// Sum up the total bytes received.
        /// </summary>
        private Int64 SumTotalBytesReceived()
        {
            // Get all network interfaces.
            List<NetworkInterface> var_NetworkInterfaces = this.GetNetworkInterfaces();

            // The total bytes received.
            Int64 var_BytesReceived = 0;

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
                                var_BytesReceived += var_Statistic.BytesReceived;
                            }
                        }
                        catch
                        {
                            // ... on fail try older statistics IPv4 only.
                            IPv4InterfaceStatistics var_OldStatistic = var_NetworkInterface.GetIPv4Statistics();

                            if (var_OldStatistic != null)
                            {
                                var_BytesReceived += var_OldStatistic.BytesReceived;
                            }
                        }
                    }
                }
                catch
                {
                    // Catch all exceptions and ignore them.
                }
            }

            return var_BytesReceived;
        }

        /// <summary>
        /// Calculate on a fixed interval, because this is 'performance intensive'.
        /// </summary>
        private void UpdateReceivedBytes()
        {
            // Start as async task, because this is 'performance intensive' and we don't want to block the main thread. Also sync is not needed here.
            Task.Run(() =>
            {
                // Get the next total received bytes.
                Int64 var_TotalBytesReceived = this.SumTotalBytesReceived();

                // Calculate the difference between the current and last received bytes.
                this.lastBytesReceived = Mathf.Max(0, var_TotalBytesReceived - this.lastTotalBytesReceived);

                // Store the current total received bytes.
                this.lastTotalBytesReceived = var_TotalBytesReceived;
            });
        }

        /// <summary>
        /// Calculates the current download rate and adds it to the graph.
        /// </summary>
        /// <returns></returns>
        protected override float GetNextValue()
        { 
            // Return the current download rate.
            return this.lastBytesReceived;
        }
    }
}

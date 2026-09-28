using System;
using Reown.AppKit.Unity;
using UnityEngine;

namespace DungeonCrawl.Chain
{
    /// <summary>
    /// Initializes Reown AppKit (WalletConnect) and exposes the connected player address.
    /// </summary>
    public class WalletConnectBootstrap : MonoBehaviour
    {
        [Tooltip("Create a free project at https://dashboard.reown.com to get this.")]
        [SerializeField] private string projectId = "YOUR_REOWN_PROJECT_ID";
        [SerializeField] private string appName = "Dungeon Crawl";
        [SerializeField] private string appDescription =
            "Onchain dungeon crawler on Robinhood Chain Testnet";
        [SerializeField] private string appUrl = "https://example.com";
        [SerializeField] private string appIconUrl = "https://example.com/icon.png";

        [Tooltip("Optional: starts a backend run-session automatically once a wallet connects.")]
        [SerializeField] private RunSessionBridge runSessionBridge;

        public string ConnectedAddress { get; private set; }

        public event Action<string> AccountConnected;

        public async void Start()
        {
            if (string.IsNullOrWhiteSpace(projectId) || projectId == "YOUR_REOWN_PROJECT_ID")
            {
                Debug.LogWarning("WalletConnectBootstrap: no Reown Project ID set (get one free at https://dashboard.reown.com) " +
                    "- skipping AppKit initialization so it doesn't spam 403 errors against Reown's API.");
                return;
            }

            var config = new AppKitConfig(
                projectId: projectId,
                new Metadata(
                    name: appName,
                    description: appDescription,
                    url: appUrl,
                    iconUrl: appIconUrl
                )
            );

            await AppKit.InitializeAsync(config);

            var resumed =
                await AppKit.ConnectorController.TryResumeSessionAsync();

            if (resumed)
            {
                HandleAccountConnected();
            }
            else
            {
                AppKit.AccountConnected += (_, _) =>
                    HandleAccountConnected();
            }
        }

        /// <summary>
        /// Opens the Reown wallet connection modal.
        /// </summary>
        public void OpenConnectModal()
        {
            AppKit.OpenModal();
        }

        private void HandleAccountConnected()
        {
            var account = AppKit.Account;

            ConnectedAddress = account.Address ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(ConnectedAddress))
            {
                Debug.Log($"Wallet connected: {ConnectedAddress}");
                AccountConnected?.Invoke(ConnectedAddress);
                runSessionBridge?.StartSessionForPlayer(ConnectedAddress);
            }
        }
    }
}
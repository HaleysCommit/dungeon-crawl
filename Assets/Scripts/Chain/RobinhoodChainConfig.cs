namespace DungeonCrawl.Chain
{
    /// <summary>
    /// Robinhood Chain Testnet network constants, verified against docs.robinhood.com/chain (2026-09-19).
    /// Mirrors frontend/src/chain/robinhoodChain.ts.
    /// </summary>
    public static class RobinhoodChainConfig
    {
        public const int ChainId = 46630;
        public const string ChainName = "Robinhood Chain Testnet";
        public const string RpcUrl = "https://rpc.testnet.chain.robinhood.com";
        public const string WebSocketUrl = "wss://feed.testnet.chain.robinhood.com";
        public const string ExplorerUrl = "https://explorer.testnet.chain.robinhood.com";
        public const string NativeCurrencySymbol = "ETH";

        public static string TxUrl(string txHash) => $"{ExplorerUrl}/tx/{txHash}";
    }
}

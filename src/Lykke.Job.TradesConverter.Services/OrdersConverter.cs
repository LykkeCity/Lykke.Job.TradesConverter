using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Job.TradesConverter.Contract;
using Lykke.Job.TradesConverter.Core.Services;
using Lykke.MatchingEngine.Connector.Models.Events;
using Lykke.MatchingEngine.Connector.Models.Events.Common;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.ClientAccount.Client.AutorestClient.Models;

namespace Lykke.Job.TradesConverter.Services
{
    [UsedImplicitly]
    public class OrdersConverter : IOrdersConverter
    {
        private const int _maxRetryCount = 5;
        private const int _serviceCallTimeout = 5 * 60 * 1000; // 5 min

        private readonly IClientAccountClient _clientAccountClient;
        private readonly ILog _log;
        private readonly ConcurrentDictionary<string, (string, string, string, string)> _walletInfoCache
            = new ConcurrentDictionary<string, (string, string, string, string)>();
        private readonly TimeSpan _clientAccountCallThreshold = TimeSpan.FromSeconds(10);

        public OrdersConverter(IClientAccountClient clientAccountClient, ILog log)
        {
            _clientAccountClient = clientAccountClient;
            _log = log;
        }

        public async Task<List<TradeLogItem>> ConvertAsync(ExecutionEvent model)
        {
            var result = new List<TradeLogItem>();

            foreach (var order in model.Orders)
            {
                if (order.Trades == null
                    || order.Trades.Count == 0
                    || order.OrderType != OrderType.Limit && order.OrderType != OrderType.Market)
                    continue;

                if (!_walletInfoCache.ContainsKey(order.WalletId))
                {
                    var (userId, hashedUserId, walletId, walletType) = await GetWalletInfoAsync(order.WalletId);
                    _walletInfoCache.TryAdd(order.WalletId, (userId, hashedUserId, walletId, walletType));
                }

                var userInfo = _walletInfoCache[order.WalletId];

                foreach (var trade in order.Trades)
                {
                    var trades = FromModel(
                        trade,
                        order,
                        userInfo.Item1,
                        userInfo.Item2,
                        userInfo.Item3,
                        userInfo.Item4,
                        model.Header.Timestamp);
                    result.AddRange(trades);
                }
            }

            return result;
        }

        private static string GetTradeId(string id1, string id2)
        {
            return id1.CompareTo(id2) <= 0 ? $"{id1}_{id2}" : $"{id2}_{id1}";
        }

        private List<TradeLogItem> FromModel(
            Trade model,
            Order order,
            string userId,
            string hashedUserId,
            string walletId,
            string walletType,
            DateTime timestamp)
        {
            var result = new List<TradeLogItem>(2);
            string orderId = order.ExternalId;
            string oppositeOrderId = model.OppositeExternalOrderId ?? model.OppositeOrderId;
            string tradeId = GetTradeId(orderId, oppositeOrderId);
            double baseVolume = double.Parse(model.BaseVolume);
            var baseDirection = baseVolume >= 0 ? Direction.Buy : Direction.Sell;
            var orderType = order.OrderType == OrderType.Limit ? "Limit" : "Market";
            result.Add(
                new TradeLogItem
                {
                    TradeLegId = model.TradeId,
                    TradeId = tradeId,
                    UserId = userId,
                    HashedUserId = hashedUserId,
                    WalletId = walletId,
                    WalletType = walletType,
                    OrderId = orderId,
                    OrderType = orderType,
                    Direction = baseDirection,
                    Asset = model.BaseAssetId,
                    Volume = (decimal)Math.Abs(baseVolume),
                    Price = (decimal)double.Parse(model.Price),
                    DateTime = model.Timestamp,
                    OppositeOrderId = oppositeOrderId,
                    OppositeAsset = model.QuotingAssetId,
                    OppositeVolume = (decimal)Math.Abs(double.Parse(model.QuotingVolume)),
                    Fee = ConvertFee(
                        order.Fees,
                        model.Fees,
                        model.BaseAssetId,
                        timestamp),
                });
            result.Add(
                new TradeLogItem
                {
                    TradeLegId = model.TradeId,
                    TradeId = tradeId,
                    UserId = userId,
                    HashedUserId = hashedUserId,
                    WalletId = walletId,
                    WalletType = walletType,
                    OrderId = orderId,
                    OrderType = orderType,
                    Direction = baseDirection == Direction.Sell ? Direction.Buy : Direction.Sell,
                    Asset = model.QuotingAssetId,
                    Volume = (decimal)Math.Abs(double.Parse(model.QuotingVolume)),
                    Price = (decimal)double.Parse(model.Price),
                    DateTime = model.Timestamp,
                    OppositeOrderId = oppositeOrderId,
                    OppositeAsset = model.BaseAssetId,
                    OppositeVolume = (decimal)Math.Abs(double.Parse(model.BaseVolume)),
                    Fee = ConvertFee(
                        order.Fees,
                        model.Fees,
                        model.QuotingAssetId,
                        timestamp),
                });

            return result;
        }

        private static async Task<T> TimeoutAfter<T>(Task<T> task, int millisecondsTimeout)
        {
            if (task != await Task.WhenAny(task, Task.Delay(millisecondsTimeout)))
                throw new TimeoutException();

            return await task;
        }

        private async Task<(string, string, string, string)> GetWalletInfoAsync(string clientId)
        {
            int retryCount = 0;
            string clientIdHash = ClientIdHashHelper.GetClientIdHash(clientId);
            do
            {
                try
                {
                    var start = DateTime.UtcNow;
                    var wallet = await TimeoutAfter(_clientAccountClient.GetWalletAsync(clientId), _serviceCallTimeout);
                    if (DateTime.UtcNow - start > _clientAccountCallThreshold)
                        _log.WriteWarning(nameof(OrdersConverter), nameof(GetWalletInfoAsync), $"Long processing of GetWalletAsync with id = {clientId}");
                    if (wallet != null)
                        return (wallet.ClientId, ClientIdHashHelper.GetClientIdHash(wallet.ClientId), clientId, wallet.Type);

                    start = DateTime.UtcNow;
                    var wallets = await TimeoutAfter(_clientAccountClient.GetClientWalletsByTypeAsync(clientId, WalletType.Trading), _serviceCallTimeout);
                    if (DateTime.UtcNow - start > _clientAccountCallThreshold)
                        _log.WriteWarning(nameof(OrdersConverter), nameof(GetWalletInfoAsync), $"Long processing of GetClientWalletsByTypeAsync with id = {clientId}");
                    if (wallets == null || !wallets.Any())
                        return (clientId, clientIdHash, clientId, "N/A");

                    var tradingWallet = wallets.First();
                    return (clientId, clientIdHash, tradingWallet.Id, tradingWallet.Type);
                }
                catch (Exception ex)
                {
                    _log.WriteWarning(nameof(OrdersConverter), nameof(GetWalletInfoAsync), ex.ToString());
                }
                ++retryCount;
            } while (retryCount <= _maxRetryCount);

            _log.WriteWarning(nameof(OrdersConverter), nameof(GetWalletInfoAsync), $"Couldn't get wallet from ClientAccount service for {clientId}");

            return (clientId, clientIdHash, clientId, "N/A");
        }

        private static TradeLogItemFee ConvertFee(
            List<FeeInstruction> feeInstructions,
            List<FeeTransfer> feeTransfers,
            string assetId,
            DateTime timestamp)
        {
            var transfer = feeTransfers?.FirstOrDefault(f => f.AssetId == assetId);
            if (transfer == null)
                return null;

            var feeInstruction = feeInstructions.First(f => f.Index == transfer.Index);

            var result = new TradeLogItemFee
                {
                    FromClientId = transfer.SourceWalletId,
                    ToClientId = transfer.TargetWalletId,
                    DateTime = timestamp,
                    Volume = double.Parse(transfer.Volume),
                    Asset = assetId,
                    Type = feeInstruction.Type.ToString(),
                    SizeType = feeInstruction.SizeType == FeeInstructionSizeType.Absolute ? "ABSOLUTE" : "PERCENTAGE",
                    MakerSizeType = feeInstruction.MakerSizeType.ToString(),
                };
            if (!string.IsNullOrWhiteSpace(feeInstruction.MakerFeeModificator))
                result.MakerFeeModificator = double.Parse(feeInstruction.MakerFeeModificator);
            if (!string.IsNullOrWhiteSpace(feeInstruction.MakerSize))
                result.MakerSize = double.Parse(feeInstruction.MakerSize);
            if (!string.IsNullOrWhiteSpace(feeInstruction.Size))
                result.Size = double.Parse(feeInstruction.Size);
            return result;
        }
    }
}

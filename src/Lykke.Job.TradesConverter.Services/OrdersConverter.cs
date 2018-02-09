using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.ClientAccount.Client.AutorestClient.Models;
using Lykke.Job.TradesConverter.Contract;
using Lykke.Job.TradesConverter.Core.IncomingMessages;
using Lykke.Job.TradesConverter.Core.Services;

namespace Lykke.Job.TradesConverter.Services
{
    public class OrdersConverter : IOrdersConverter
    {
        private const int _maxRetryCount = 5;
        private const int _serviceCallTimeout = 100000;

        private readonly IClientAccountClient _clientAccountClient;
        private readonly ILog _log;
        private readonly ConcurrentDictionary<string, (string, string, string, string)> _walletInfoCache
            = new ConcurrentDictionary<string, (string, string, string, string)>();

        public OrdersConverter(IClientAccountClient clientAccountClient, ILog log)
        {
            _clientAccountClient = clientAccountClient;
            _log = log;
        }

        public async Task<List<TradeLogItem>> ConvertAsync(LimitOrderWithTrades model)
        {
            var result = new List<TradeLogItem>();
            if (model.Trades == null || model.Trades.Count == 0)
                return result;

            foreach (var trade in model.Trades)
            {
                if (!_walletInfoCache.ContainsKey(trade.ClientId))
                {
                    (string userId, string hashedUserId, string walletId, string walletType) = await GetWalletInfoAsync(trade.ClientId);
                    _walletInfoCache.TryAdd(trade.ClientId, (userId, hashedUserId, walletId, walletType));
                }
                var userInfo = _walletInfoCache[trade.ClientId];
                var trades = FromModel(
                    trade,
                    model.Order,
                    userInfo.Item1,
                    userInfo.Item2,
                    userInfo.Item3,
                    userInfo.Item4);
                result.AddRange(trades);
            }
            return result;
        }

        public async Task<List<TradeLogItem>> ConvertAsync(MarketOrderWithTrades model)
        {
            var result = new List<TradeLogItem>();
            if (model.Trades == null || model.Trades.Count == 0)
                return result;

            foreach (var trade in model.Trades)
            {
                if (!_walletInfoCache.ContainsKey(trade.MarketClientId))
                {
                    (string userId, string hashedUserId, string walletId, string walletType) = await GetWalletInfoAsync(trade.MarketClientId);
                    _walletInfoCache.TryAdd(trade.MarketClientId, (userId, hashedUserId, walletId, walletType));
                }
                var userInfo = _walletInfoCache[trade.MarketClientId];
                var trades = FromModel(
                    trade,
                    model.Order,
                    userInfo.Item1,
                    userInfo.Item2,
                    userInfo.Item3,
                    userInfo.Item4);
                result.AddRange(trades);
            }
            return result;
        }

        private static string GetTradeId(string id1, string id2)
        {
            return id1.CompareTo(id2) <= 0 ? $"{id1}_{id2}" : $"{id2}_{id1}";
        }

        private List<TradeLogItem> FromModel(
            LimitTradeInfo model,
            LimitOrder order,
            string userId,
            string hashedUserId,
            string walletId,
            string walletType)
        {
            var result = new List<TradeLogItem>(4);
            string orderId = order.ExternalId;
            string oppositeOrderId = model.OppositeOrderExternalId ?? model.OppositeOrderId;
            string tradeId = GetTradeId(orderId, oppositeOrderId);
            var direction = ChooseDirection(
                order.AssetPairId,
                model.Asset,
                order.Straight,
                order.Volume);
            string orderType = "Limit";
            result.Add(
                new TradeLogItem
                {
                    TradeId = tradeId,
                    UserId = userId,
                    HashedUserId = hashedUserId,
                    WalletId = walletId,
                    WalletType = walletType,
                    OrderId = orderId,
                    OrderType = orderType,
                    Direction = direction,
                    Asset = model.Asset,
                    Volume = (decimal)Math.Abs(model.Volume),
                    Price = (decimal)model.Price,
                    DateTime = model.Timestamp,
                    OppositeOrderId = oppositeOrderId,
                    OppositeAsset = model.OppositeAsset,
                    OppositeVolume = (decimal)Math.Abs(model.OppositeVolume),
                    Fee = ConvertFee(model.Fees, model.Asset),
                });
            result.Add(
                new TradeLogItem
                {
                    TradeId = tradeId,
                    UserId = userId,
                    HashedUserId = hashedUserId,
                    WalletId = walletId,
                    WalletType = walletType,
                    OrderId = orderId,
                    OrderType = orderType,
                    Direction = direction == Direction.Sell ? Direction.Buy : Direction.Sell,
                    Asset = model.OppositeAsset,
                    Volume = (decimal)Math.Abs(model.OppositeVolume),
                    Price = (decimal)model.Price,
                    DateTime = model.Timestamp,
                    OppositeOrderId = oppositeOrderId,
                    OppositeAsset = model.Asset,
                    OppositeVolume = (decimal)Math.Abs(model.Volume),
                    Fee = ConvertFee(model.Fees, model.OppositeAsset),
                });

            return result;
        }

        private List<TradeLogItem> FromModel(
            TradeInfo model,
            MarketOrder order,
            string userId,
            string hashedUserId,
            string walletId,
            string walletType)
        {
            var result = new List<TradeLogItem>(2);
            string orderId = order.ExternalId;
            string oppositeOrderId = model.LimitOrderExternalId ?? model.LimitOrderId;
            string tradeId = GetTradeId(orderId, oppositeOrderId);
            var direction = ChooseDirection(
                order.AssetPairId,
                model.MarketAsset,
                order.Straight,
                order.Volume);
            string orderType = "Market";
            result.Add(
                new TradeLogItem
                {
                    TradeId = tradeId,
                    UserId = userId,
                    HashedUserId = hashedUserId,
                    WalletId = walletId,
                    WalletType = walletType,
                    OrderId = orderId,
                    OrderType = orderType,
                    Direction = direction,
                    Asset = model.MarketAsset,
                    Volume = (decimal)Math.Abs(model.MarketVolume),
                    Price = (decimal)model.Price,
                    DateTime = model.Timestamp,
                    OppositeOrderId = oppositeOrderId,
                    OppositeAsset = model.LimitAsset,
                    OppositeVolume = (decimal)Math.Abs(model.LimitVolume),
                    Fee = ConvertFee(model.Fees, model.MarketAsset),
                });
            result.Add(
                new TradeLogItem
                {
                    TradeId = tradeId,
                    UserId = userId,
                    HashedUserId = hashedUserId,
                    WalletId = walletId,
                    WalletType = walletType,
                    OrderId = orderId,
                    OrderType = orderType,
                    Direction = direction == Direction.Sell ? Direction.Buy : Direction.Sell,
                    Asset = model.LimitAsset,
                    Volume = (decimal)Math.Abs(model.LimitVolume),
                    Price = (decimal)model.Price,
                    DateTime = model.Timestamp,
                    OppositeOrderId = oppositeOrderId,
                    OppositeAsset = model.MarketAsset,
                    OppositeVolume = (decimal)Math.Abs(model.MarketVolume),
                    Fee = ConvertFee(model.Fees, model.LimitAsset),
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
                    var wallet = await TimeoutAfter(_clientAccountClient.GetWalletAsync(clientId), _serviceCallTimeout);
                    if (wallet != null)
                        return (wallet.ClientId, ClientIdHashHelper.GetClientIdHash(wallet.ClientId), clientId, wallet.Type);

                    var wallets = await TimeoutAfter(_clientAccountClient.GetClientWalletsByTypeAsync(clientId, WalletType.Trading), _serviceCallTimeout);
                    if (wallets == null || !wallets.Any())
                        return (clientId, clientIdHash, clientId, "N/A");

                    var tradingWallet = wallets.First();
                    return (clientId, clientIdHash, tradingWallet.Id, tradingWallet.Type);
                }
                catch (Exception ex)
                {
                    await _log.WriteInfoAsync(nameof(OrdersConverter), nameof(GetWalletInfoAsync), ex.ToString());
                }
                ++retryCount;
            } while (retryCount <= _maxRetryCount);

            await _log.WriteWarningAsync(nameof(OrdersConverter), nameof(GetWalletInfoAsync), $"Couldn't get wallet from ClientAccount service for {clientId}");

            return (clientId, clientIdHash, clientId, "N/A");
        }

        private static Direction ChooseDirection(
            string assetPair,
            string asset,
            bool straight,
            double orderVolume)
        {
            bool isBuy = !(straight ^ (orderVolume >= 0));
            if (assetPair.EndsWith(asset))
                isBuy = !isBuy;
            return isBuy ? Direction.Buy : Direction.Sell;
        }

        private static TradeLogItemFee ConvertFee(List<Fee> fees, string assetId)
        {
            if (fees == null)
                return null;
            return fees
                .Where(f => f != null && f.Transfer != null && f.Transfer.Asset == assetId)
                .Select(f =>
                    new TradeLogItemFee
                    {
                        FromClientId = f.Transfer.FromClientId,
                        ToClientId = f.Transfer.ToClientId,
                        DateTime = f.Transfer.DateTime,
                        Volume = f.Transfer.Volume,
                        Asset = f.Transfer.Asset,
                        Type = f.Instruction.Type,
                        SizeType = f.Instruction.SizeType,
                        Size = f.Instruction.Size,
                    })
                .FirstOrDefault();
        }
    }
}

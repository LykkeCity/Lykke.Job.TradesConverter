using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.ClientAccount.Client.AutorestClient.Models;
using Lykke.Job.TradesConverter.Contract;
using Lykke.Job.TradesConverter.Core.IncomingMessages;
using Lykke.Job.TradesConverter.Core.Services;

namespace Lykke.Job.TradesConverter.Services
{
    public class OrdersConverter : IOrdersConverter
    {
        private readonly IClientAccountClient _clientAccountClient;
        private readonly ConcurrentDictionary<string, (string, string, string, string)> _walletInfoCache
            = new ConcurrentDictionary<string, (string, string, string, string)>();

        public OrdersConverter(IClientAccountClient clientAccountClient)
        {
            _clientAccountClient = clientAccountClient;
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
                    Fee = ConvertFee(model.FeeInstruction),
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
                    Fee = ConvertFee(model.FeeInstruction),
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
                    Fee = ConvertFee(model.FeeInstruction),
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
                    Fee = ConvertFee(model.FeeInstruction),
                });

            return result;
        }

        private async Task<(string, string, string, string)> GetWalletInfoAsync(string clientId)
        {
            var wallet = await _clientAccountClient.GetWalletAsync(clientId);
            if (wallet != null)
                return (wallet.ClientId, ClientIdHashHelper.GetClientIdHash(wallet.ClientId), clientId, wallet.Type);

            string clientIdHash = ClientIdHashHelper.GetClientIdHash(clientId);
            var wallets = await _clientAccountClient.GetClientWalletsByTypeAsync(clientId, WalletType.Trading);
            if (wallets == null || !wallets.Any())
                return (clientId, clientIdHash, clientId, "N/A");

            var tradingWallet = wallets.First();
            return (clientId, clientIdHash, tradingWallet.Id, tradingWallet.Type);
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

        private static TradeLogItemFee ConvertFee(FeeInstruction fee)
        {
            if (fee == null)
                return null;
            return new TradeLogItemFee
            {
                Type = fee.Type,
                SourceClientId = fee.SourceClientId,
                TargetClientId = fee.TargetClientId,
                SizeType = fee.SizeType,
                Size = fee.Size,
            };
        }
    }
}

using System;
using System.Linq;
using System.Collections.Generic;
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

        public OrdersConverter(IClientAccountClient clientAccountClient)
        {
            _clientAccountClient = clientAccountClient;
        }

        public async Task<List<TradeLogItem>> ConvertAsync(LimitOrderWithTrades model)
        {
            var result = new List<TradeLogItem>();
            if (model.Trades == null || model.Trades.Count == 0)
                return result;

            var clientsCache = new Dictionary<string, (string, string)>();
            foreach (var trade in model.Trades)
            {
                if (!clientsCache.ContainsKey(trade.ClientId))
                {
                    (string userId, string walletId) = await GetWalletInfoAsync(trade.ClientId);
                    clientsCache.Add(trade.ClientId, (userId, walletId));
                }
                var userInfo = clientsCache[trade.ClientId];
                var trades = FromModel(
                    trade,
                    model.Order,
                    userInfo.Item1,
                    userInfo.Item2);
                result.AddRange(trades);
            }
            return result;
        }

        public async Task<List<TradeLogItem>> ConvertAsync(MarketOrderWithTrades model)
        {
            var result = new List<TradeLogItem>();
            if (model.Trades == null || model.Trades.Count == 0)
                return result;

            var clientsCache = new Dictionary<string, (string, string)>();
            foreach (var trade in model.Trades)
            {
                if (!clientsCache.ContainsKey(trade.MarketClientId))
                {
                    (string userId, string walletId) = await GetWalletInfoAsync(trade.MarketClientId);
                    clientsCache.Add(trade.MarketClientId, (userId, walletId));
                }
                var userInfo = clientsCache[trade.MarketClientId];
                var trades = FromModel(
                    trade,
                    model.Order,
                    userInfo.Item1,
                    userInfo.Item2);
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
            string walletId)
        {
            var result = new List<TradeLogItem>(4);
            string orderId = order.ExternalId;
            string oppositeOrderId = model.OppositeOrderExternalId ?? model.OppositeOrderId;
            string tradeId = GetTradeId(orderId, oppositeOrderId);
            string direction = ChooseDirection(
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
                    WalletId = walletId,
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
                });
            result.Add(
                new TradeLogItem
                {
                    TradeId = tradeId,
                    UserId = userId,
                    WalletId = walletId,
                    OrderId = orderId,
                    OrderType = orderType,
                    Direction = direction == "Sell" ? "Buy" : "Sell",
                    Asset = model.OppositeAsset,
                    Volume = (decimal)Math.Abs(model.OppositeVolume),
                    Price = (decimal)model.Price,
                    DateTime = model.Timestamp,
                    OppositeOrderId = oppositeOrderId,
                    OppositeAsset = model.Asset,
                    OppositeVolume = (decimal)Math.Abs(model.Volume),
                });

            return result;
        }

        private List<TradeLogItem> FromModel(
            TradeInfo model,
            MarketOrder order,
            string userId,
            string walletId)
        {
            var result = new List<TradeLogItem>(2);
            string orderId = order.ExternalId;
            string oppositeOrderId = model.LimitOrderExternalId ?? model.LimitOrderId;
            string tradeId = GetTradeId(orderId, oppositeOrderId);
            string direction = ChooseDirection(
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
                    WalletId = walletId,
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
                });
            result.Add(
                new TradeLogItem
                {
                    TradeId = tradeId,
                    UserId = userId,
                    WalletId = walletId,
                    OrderId = orderId,
                    OrderType = orderType,
                    Direction = direction == "Sell" ? "Buy" : "Sell",
                    Asset = model.LimitAsset,
                    Volume = (decimal)Math.Abs(model.LimitVolume),
                    Price = (decimal)model.Price,
                    DateTime = model.Timestamp,
                    OppositeOrderId = oppositeOrderId,
                    OppositeAsset = model.MarketAsset,
                    OppositeVolume = (decimal)Math.Abs(model.MarketVolume),
                });

            return result;
        }

        private async Task<(string ClientId, string WalletId)> GetWalletInfoAsync(string clientId)
        {
            var wallet = await _clientAccountClient.GetWalletAsync(clientId);
            if (wallet != null)
                return (ClientIdHashHelper.GetClientIdHash(wallet.ClientId), clientId);

            string clientIdHash = ClientIdHashHelper.GetClientIdHash(clientId);
            var wallets = await _clientAccountClient.GetClientWalletsByTypeAsync(clientId, WalletType.Trading);
            if (wallets == null || !wallets.Any())
                return (clientIdHash, clientIdHash);
            var tradingWallet = wallets.First();
            return (clientIdHash, tradingWallet.Id);
        }

        private static string ChooseDirection(
            string assetPair,
            string asset,
            bool straight,
            double orderVolume)
        {
            bool isBuy = !(straight ^ (orderVolume >= 0));
            if (assetPair.EndsWith(asset))
                isBuy = !isBuy;
            return isBuy ? "Buy" : "Sell";
        }
    }
}

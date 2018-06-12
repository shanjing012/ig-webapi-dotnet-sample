using System;
using System.Collections.ObjectModel;
using System.Linq;
using IGWebApiClient;
using Lightstreamer.DotNet.Client;
using System.Windows.Data;
using SampleWPFTrader.Model;
using SampleWPFTrader.Common;
using dto.endpoint.positions.create.otc.v2;
using dto.endpoint.positions.close.v1;
using dto.endpoint.prices.v2;

namespace SampleWPFTrader.ViewModel
{
    public class GraphViewModel : ViewModelBase
    {
        // LS subscriptions...
        private SubscribedTableKey _chartSubscribedTableKey;
        private ChartCandleTableListerner _chartSubscription;
        private SubscribedTableKey _chart5MinuteSubscribedTableKey;
        private ChartCandleTableListerner _chart5MinuteSubscription;
        private SubscribedTableKey _chartTickSubscribedTableKey;
        private ChartTickTableListerner _chartTickSubscription;
        private Object _graphLock = new Object();
        private IgPublicApiData.InstrumentModel positionChartTick;

        //tab selection for viewmodel
        private bool _graphTabSelected;
        public bool GraphTabSelected
        {
            get
            {
                return _graphTabSelected;
            }
            set
            {
                if (_graphTabSelected != value)
                {
                    _graphTabSelected = value;
                    GraphTabChanged();
                    RaisePropertyChanged("GraphTabSelected");
                }
            }
        }
        public void GraphTabChanged()
        {
            if (GraphTabSelected)
            {
                AddStatusMessage("== Graph Tab Selected ==");
                // Get Rest Orders and then subscribe
                if (LoggedIn)
                {
                    //GetBrowseMarketsRoot();
                    AddStatusMessage("Select some epic.");
                }
                else
                {
                    AddStatusMessage("Please log in first");
                }
            }
            else
            {
                AddStatusMessage("Graph Tab de-selected");
                UnsubscribeFromCharts();
                //clear all stuff
                ChartMarketData.Clear();
                ChartMarketHistoryData.Clear();
                PositionData.Clear();
                ComboBoxMarkets.Clear();
                selectedEpic = "";
            }
        }

        //variables on the viewmodel
        private string selectedEpic;
        private string searchQuery;
        private string currencyTraded;
        //variables public accessors
        public string SelectedEpic
        {
            get
            {
                return selectedEpic;
            }
            set
            {
                selectedEpic = value;
            }
        }
        public string SearchQuery
        {
            get
            {
                return searchQuery;
            }
            set
            {
                searchQuery = value;
            }
        }
        public string CurrencyTraded
        {
            get
            {
                return currencyTraded;
            }
            set
            {
                currencyTraded = value;
            }
        }

        //commands for the buttons on graph
        public RelayCommand SearchEpicCommand
        {
            get;
            private set;
        }
        public RelayCommand SelectEpicCommand
        {
            get;
            private set;
        }
        public RelayCommand OpenPositionCommand
        {
            get;
            private set;
        }
        public RelayCommand ClosePositionCommand
        {
            get;
            private set;
        }

        //data containers for our information
        public ObservableCollection<IgPublicApiData.ChartModel> ChartMarketData { get; set; }
        public ObservableCollection<IgPublicApiData.ChartModel> ChartMarketHistoryData { get; set; }
        public ObservableCollection<IgPublicApiData.OrderModel> PositionData { get; set; }
        public IgPublicApiData.ChartModel fiveMinChartData { get; set; }

        public ObservableCollection<String> ComboBoxMarkets { get; set; }
        public CollectionViewSource ChartMarketDataView { get; set; }

        //initialization
        public GraphViewModel()
        {
            InitialiseViewModel();

            //init data containers
            positionChartTick = new IgPublicApiData.InstrumentModel();
            ChartMarketData = new ObservableCollection<IgPublicApiData.ChartModel>(); //for charttick data
            ChartMarketHistoryData = new ObservableCollection<IgPublicApiData.ChartModel>();
            ComboBoxMarkets = new ObservableCollection<String>();
            PositionData = new ObservableCollection<IgPublicApiData.OrderModel>();
            fiveMinChartData = new IgPublicApiData.ChartModel();

            //to avoid sync error or some shit
            System.Windows.Data.BindingOperations.EnableCollectionSynchronization(ChartMarketHistoryData, _graphLock);

            //streaming of candlechart data (second)
            _chartSubscribedTableKey = new SubscribedTableKey();
            _chartSubscription = new ChartCandleTableListerner();
            _chartSubscription.Update += OnChartCandleDataUpdate;
            //streaming of candlechart data (5 mins)
            _chart5MinuteSubscribedTableKey = new SubscribedTableKey();
            _chart5MinuteSubscription = new ChartCandleTableListerner();
            _chart5MinuteSubscription.Update += OnChartCandleData5MinuteUpdate;
            //streaming of candletick data
            _chartTickSubscribedTableKey = new SubscribedTableKey();
            _chartTickSubscription = new ChartTickTableListerner();
            _chartTickSubscription.Update += OnChartTickDataUpdate;

            //view model to sort candlechart
            ChartMarketDataView = new CollectionViewSource();
            ChartMarketDataView.Source = ChartMarketHistoryData;
            //sort the historic candlechart view
            ChartMarketDataView.SortDescriptions.Add(new System.ComponentModel.SortDescription("UpdateTime", System.ComponentModel.ListSortDirection.Descending));


            WireCommands();
        }
        
        //wire button commands
        private void WireCommands()
        {
            SearchEpicCommand = new RelayCommand(SearchEpic);
            SelectEpicCommand = new RelayCommand(SelectEpic);
            OpenPositionCommand = new RelayCommand(OpenPosition);
            ClosePositionCommand = new RelayCommand(CloseSelectedPositions);
            SearchEpicCommand.IsEnabled = true;
            SelectEpicCommand.IsEnabled = false;
            OpenPositionCommand.IsEnabled = true;
            ClosePositionCommand.IsEnabled = true;
        }

        //search for epic to view
        public async void SearchEpic()
        {
            //get search query
            try
            {
                if (LoggedIn && searchQuery != "")
                {
                    // Unsubscribe from any instruments we are currently subscribed to...
                    //UnsubscribeFromBrowsePrices();

                    var response = await igRestApiClient.searchMarket(searchQuery);

                    if (response && (response.Response != null) && (response.Response.markets != null))
                    {
                        ComboBoxMarkets.Clear();
                        SelectEpicCommand.IsEnabled = true;
                        foreach (var node in response.Response.markets)
                        {
                            ComboBoxMarkets.Add(node.epic);
                        }
                        
                        AddStatusMessage(String.Format("Search epic data received for {0} nodes", response.Response.markets.Count));
                    }
                    else
                    {
                        AddStatusMessage("Search epic data recieved for 0 nodes");
                    }
                }
                else
                {
                    AddStatusMessage("Please log in first/Invalid Search query");
                }
            }
            catch (Exception ex)
            {
                AddStatusMessage(ex.Message);
            }
        }

        public void SelectEpic()
        {
            try
            {
                if (LoggedIn && selectedEpic != "")
                {
                    //add selectedepic to the epic string
                    string[] epic = new string[] { selectedEpic };
                    SubscribeToCharts(epic);
                    GetPositions(selectedEpic);
                    GetHistoricChart(selectedEpic);
                }
                else
                {
                    AddStatusMessage("Please log in first/Invalid epic");
                }
            }
            catch (Exception ex)
            {
                AddStatusMessage(ex.Message);
            }
        }

        public async void OpenPosition()
        {
            //get search query
            try
            {
                if (LoggedIn && selectedEpic != "")
                {
                    //open position for selectedepic
                    CreatePositionRequest position = new CreatePositionRequest();
                    position.epic = selectedEpic;
                    position.expiry = "-";
                    position.direction = "BUY"; //or sell, try buy first
                    position.size = 1;
                    position.orderType = "MARKET";
                    position.guaranteedStop = false;
                    position.forceOpen = true;
                    position.currencyCode = currencyTraded;
                    
                    //create position
                    var response = await igRestApiClient.createPositionV2(position);

                    if (response && (response.Response != null) && (response.Response.dealReference != null))
                    {
                        AddStatusMessage("Created position. Deal Reference: " + response.Response.dealReference);
                    }
                    else
                    {
                        AddStatusMessage("Cannot create position");
                    }
                }
                else
                {
                    AddStatusMessage("Please log in first/Invalid epic");
                }

                GetPositions(selectedEpic);
            }
            catch (Exception ex)
            {
                AddStatusMessage(ex.Message);
            }
        }

        public async void ClosePosition(Collection<IgPublicApiData.OrderModel> ClosingOrders)
        {
            //get search query , sell first of the 
            try
            {
                if (LoggedIn && ClosingOrders != null)
                {
                    ClosePositionRequest position = new ClosePositionRequest();

                    foreach (IgPublicApiData.OrderModel Order in ClosingOrders)
                    {
                        //set attributes and use api.
                        position.dealId = Order.DealId;
                        if (Order.Direction == "SELL")
                            position.direction = "BUY";
                        else
                            position.direction = "SELL";
                        position.size = 1;
                        position.orderType = "MARKET";

                        var response = await igRestApiClient.closePosition(position);

                        if (response && (response.Response != null) && (response.Response.dealReference != null))
                        {
                            AddStatusMessage("Closed position. Deal Reference: " + response.Response.dealReference);
                        }
                        else
                        {
                            AddStatusMessage("Cannot close position: " + response.Response.dealReference);
                        }
                    }
                    GetPositions(selectedEpic);
                }
                else
                {
                    AddStatusMessage("Please log in first/Invalid epic");
                }

                GetPositions(selectedEpic);
            }
            catch (Exception ex)
            {
                AddStatusMessage(ex.Message);
            }
        }

        public void CloseSelectedPositions()
        {
            Collection<IgPublicApiData.OrderModel> ClosingOrders = new Collection<IgPublicApiData.OrderModel>();
            foreach(IgPublicApiData.OrderModel Model in PositionData)
            {
                ClosingOrders.Add(Model);
            }

            ClosePosition(ClosingOrders);
        }

        //subscribe to open position with the epic being the provided
        private async void GetPositions(string epic)
        {
            PositionData.Clear();
            //to do this subscribe to position and close position 
            try
            {
                if (LoggedIn && selectedEpic != "")
                {
                    var response = await igRestApiClient.getOTCOpenPositionsV2();
                    
                    if(response && (response.Response != null) && (response.Response.positions != null))
                    {
                        foreach(var position in response.Response.positions.Where(OpenPosition => OpenPosition.market.epic == selectedEpic))
                        {
                            //we need to create a positionmodel and add it into position data.
                            IgPublicApiData.OrderModel positionWithDealID = new IgPublicApiData.OrderModel();

                            positionWithDealID.Model = positionChartTick;

                            positionWithDealID.Model.Epic = position.market.epic;
                            positionWithDealID.CreationDate = position.position.createdDate;
                            positionWithDealID.DealId = position.position.dealId;

                            positionWithDealID.OrderSize = position.position.size;
                            positionWithDealID.Direction = position.position.direction;
                            positionWithDealID.OpenLevel = position.position.level;
                            
                            positionWithDealID.Model.MarketStatus = position.market.marketStatus;
                            
                            positionWithDealID.DealId = position.position.dealId;

                            PositionData.Add(positionWithDealID);

                            if(currencyTraded != position.position.currency)
                            {
                                currencyTraded = position.position.currency;
                                ApplicationViewModel.getInstance().AddStatusMessage("Currency set to: " + currencyTraded);
                            }
                        }
                    }
                    else
                    {
                        AddStatusMessage("No Positions found");
                    }
                }
                else
                {
                    AddStatusMessage("Please log in first/Invalid epic");
                }
            }
            catch (Exception ex)
            {
                AddStatusMessage(ex.Message);
            }
        }

        //get the historic data of epic
        //may need to set minute interval manually?
        private async void GetHistoricChart(string selectedEpic)
        {
            ChartMarketHistoryData.Clear();
            try
            {
                if (LoggedIn && selectedEpic != "")
                {
                    var response = await igRestApiClient.priceSearchByNumV2(selectedEpic, "MINUTE_5", "10");

                    if(response && response.Response != null && response.Response.prices.Count != 0)
                    {
                        for (int i = 0; i < response.Response.prices.Count - 1; i++)
                        {

                            IgPublicApiData.ChartModel dataEntered = new IgPublicApiData.ChartModel();

                            dataEntered.ChartEpic = selectedEpic;
                            dataEntered.Bid = new IgPublicApiData.ChartHlocModel();
                            dataEntered.Bid.Close = response.Response.prices[i].closePrice.bid;
                            dataEntered.Bid.High = response.Response.prices[i].highPrice.bid;
                            dataEntered.Bid.Low = response.Response.prices[i].lowPrice.bid;
                            dataEntered.Bid.Open = response.Response.prices[i].openPrice.bid;

                            dataEntered.DayChange = 0;
                            dataEntered.DayChangePct = 0;
                            dataEntered.DayHigh = 0;
                            dataEntered.DayLow = 0;
                            dataEntered.DayMidOpenPrice = 0;
                            dataEntered.EndOfConsolidation = true; //dummy var
                            dataEntered.IncrimetalTradingVolume = 0;

                            if (response.Response.prices[i].lastTradedVolume != null)
                            {
                                dataEntered.LastTradedVolume = response.Response.prices[i].lastTradedVolume;
                                dataEntered.LastTradedPrice = new IgPublicApiData.ChartHlocModel();
                                dataEntered.LastTradedPrice.Close = 0;
                                dataEntered.LastTradedPrice.High = 0;
                                dataEntered.LastTradedPrice.Low = 0;
                                dataEntered.LastTradedPrice.Open = 0;
                            }

                            dataEntered.Offer = new IgPublicApiData.ChartHlocModel();
                            dataEntered.Offer.Close = response.Response.prices[i].closePrice.ask;
                            dataEntered.Offer.Open = response.Response.prices[i].openPrice.ask;
                            dataEntered.Offer.High = response.Response.prices[i].highPrice.ask;
                            dataEntered.Offer.Low = response.Response.prices[i].lowPrice.ask;

                            dataEntered.TickCount = 0;
                            dataEntered.UpdateTime = Convert.ToDateTime(response.Response.prices[i].snapshotTime).ToUniversalTime().ToLocalTime();

                            if (ChartMarketHistoryData.Count() >= 10)
                                ChartMarketHistoryData.RemoveAt(0);
                            ChartMarketHistoryData.Add(dataEntered);
                        }
                    }
                    else
                    {
                        AddStatusMessage("No response / No historic data");
                    }
                }
                else
                {
                    AddStatusMessage("Please log in first/Invalid epic");
                }
            }
            catch (Exception ex)
            {
                AddStatusMessage(ex.Message);
            }
        }

        //subscribe to chart
        //only 1 entry in chartEpics string[] parameter !!
        private void SubscribeToCharts(string[] chartEpics)
        {
            try
            {
                if (igStreamApiClient != null)
                {
                    UnsubscribeFromCharts();
                    ChartMarketData.Clear();
                    ChartMarketHistoryData.Clear();
                    foreach (var epic in chartEpics)
                    {
                        IgPublicApiData.ChartModel chartModel = new IgPublicApiData.ChartModel();
                        chartModel.ChartEpic = epic;
                        ChartMarketData.Add(chartModel);
                        AddStatusMessage("Subscribing to Chart Data (CandleStick): " + epic);
                        AddStatusMessage("Subscribing to Chart Tick Data: " + epic);
                    }

                    //chart 1
                    _chartSubscribedTableKey = igStreamApiClient.SubscribeToChartCandleData(chartEpics, ChartScale.OneSecond, _chartSubscription);
                    //chart five minute
                    _chart5MinuteSubscribedTableKey = igStreamApiClient.SubscribeToChartCandleData(chartEpics, ChartScale.OneMinute, _chart5MinuteSubscription);
                    //chart tick
                    _chartTickSubscribedTableKey = igStreamApiClient.SubscribeToChartTicks(chartEpics, _chartTickSubscription);
                }
            }
            catch (Exception ex)
            {
                AddStatusMessage("Exception when trying to subscribe to Chart Data: " + ex.Message);
            }
        }

        //unsub chart
        private void UnsubscribeFromCharts()
        {
            if ((igStreamApiClient != null) && (_chartSubscribedTableKey != null) && (_chartTickSubscribedTableKey != null) && (LoggedIn))
            {
                igStreamApiClient.UnsubscribeTableKey(_chartSubscribedTableKey);
                _chartSubscribedTableKey = null;

                igStreamApiClient.UnsubscribeTableKey(_chart5MinuteSubscribedTableKey);
                _chart5MinuteSubscribedTableKey = null;

                igStreamApiClient.UnsubscribeTableKey(_chartTickSubscribedTableKey);
                _chartTickSubscribedTableKey = null;

                AddStatusMessage("GraphViewModel : Unsubscribing from candle data from charts");
            }
        }

        //update of candle tick data for position calculation of $$
        private void OnChartTickDataUpdate(object sender, UpdateArgs<ChartTickData> e)
        {
            var updateTick = e.UpdateData;
            positionChartTick.Bid = updateTick.Bid;
            positionChartTick.Offer = updateTick.Offer;
            //do calculation here?

            foreach(IgPublicApiData.OrderModel orderModel in PositionData)
            {
                if(orderModel.Direction == "SELL")
                {
                    orderModel.Profit = (orderModel.OpenLevel - positionChartTick.Offer) * 100000 * orderModel.OrderSize;
                }
                else
                {
                    orderModel.Profit = (positionChartTick.Bid - orderModel.OpenLevel) * 100000 * orderModel.OrderSize;
                }
            }
        }

        //update of candle chart data (SECOND)
        private void OnChartCandleDataUpdate(object sender, UpdateArgs<ChartCandelData> e)
        {
            var candleUpdate = e.UpdateData;
            var tempEpic = e.ItemName.Replace("CHART:", "");
            var tempArray = tempEpic.Split(':');
            var epic = tempArray[0];
            var time = tempArray[1];

            //get the first chartmodel inside the chartmarketdata collection and edit it.
            //should be foreach so the subscribe wont fck up.
            foreach (IgPublicApiData.ChartModel chartModel in ChartMarketData.Where(cmd => cmd.ChartEpic == epic))
            {
                chartModel.UpdateTime = candleUpdate.UpdateTime.Value.ToLocalTime();

                chartModel.Bid = new IgPublicApiData.ChartHlocModel();
                chartModel.Bid.Close = candleUpdate.Bid.Close;
                chartModel.Bid.High = candleUpdate.Bid.High;
                chartModel.Bid.Low = candleUpdate.Bid.Low;
                chartModel.Bid.Open = candleUpdate.Bid.Open;

                chartModel.DayChange = candleUpdate.DayChange;
                chartModel.DayChangePct = candleUpdate.DayChangePct;
                chartModel.DayHigh = candleUpdate.DayHigh;
                chartModel.DayLow = candleUpdate.DayLow;
                chartModel.DayMidOpenPrice = candleUpdate.DayMidOpenPrice;
                chartModel.EndOfConsolidation = candleUpdate.EndOfConsolidation;
                chartModel.IncrimetalTradingVolume = candleUpdate.IncrimetalTradingVolume;

                if (candleUpdate.LastTradedVolume != null)
                {
                    chartModel.LastTradedVolume = candleUpdate.LastTradedVolume;
                    chartModel.LastTradedPrice = new IgPublicApiData.ChartHlocModel();
                    chartModel.LastTradedPrice.Close = candleUpdate.LastTradedPrice.Close;
                    chartModel.LastTradedPrice.High = candleUpdate.LastTradedPrice.High;
                    chartModel.LastTradedPrice.Low = candleUpdate.LastTradedPrice.Low;
                    chartModel.LastTradedPrice.Open = candleUpdate.LastTradedPrice.Open;
                }

                chartModel.Offer = new IgPublicApiData.ChartHlocModel();
                chartModel.Offer.Close = candleUpdate.Offer.Close;
                chartModel.Offer.Open = candleUpdate.Offer.Open;
                chartModel.Offer.High = candleUpdate.Offer.High;
                chartModel.Offer.Low = candleUpdate.Offer.Low;

                chartModel.TickCount = candleUpdate.TickCount;
            }
        }

        //update of candle chart data (5 MINUTES)
        private void OnChartCandleData5MinuteUpdate(object sender, UpdateArgs<ChartCandelData> e)
        {
            var candleUpdate = e.UpdateData;
            var tempEpic = e.ItemName.Replace("CHART:", "");
            var tempArray = tempEpic.Split(':');
            var epic = tempArray[0];
            var time = tempArray[1];

            //check if the time changed, if change, add the new one to the collection. Else, just update fiveminchartdata
            if (fiveMinChartData.UpdateTime != null && fiveMinChartData.UpdateTime != candleUpdate.UpdateTime.Value.ToLocalTime())
            {

                //create fiveminchartdata entry and add into datagrid
                IgPublicApiData.ChartModel dataEntered = new IgPublicApiData.ChartModel();

                dataEntered.ChartEpic = fiveMinChartData.ChartEpic;
                dataEntered.Bid = new IgPublicApiData.ChartHlocModel();
                dataEntered.Bid.Close = fiveMinChartData.Bid.Close;
                dataEntered.Bid.High = fiveMinChartData.Bid.High;
                dataEntered.Bid.Low = fiveMinChartData.Bid.Low;
                dataEntered.Bid.Open = fiveMinChartData.Bid.Open;

                dataEntered.DayChange = fiveMinChartData.DayChange;
                dataEntered.DayChangePct = fiveMinChartData.DayChangePct;
                dataEntered.DayHigh = fiveMinChartData.DayHigh;
                dataEntered.DayLow = fiveMinChartData.DayLow;
                dataEntered.DayMidOpenPrice = fiveMinChartData.DayMidOpenPrice;
                dataEntered.EndOfConsolidation = fiveMinChartData.EndOfConsolidation;
                dataEntered.IncrimetalTradingVolume = fiveMinChartData.IncrimetalTradingVolume;

                if (fiveMinChartData.LastTradedVolume != null)
                {
                    dataEntered.LastTradedVolume = fiveMinChartData.LastTradedVolume;
                    dataEntered.LastTradedPrice = new IgPublicApiData.ChartHlocModel();
                    dataEntered.LastTradedPrice.Close = fiveMinChartData.LastTradedPrice.Close;
                    dataEntered.LastTradedPrice.High = fiveMinChartData.LastTradedPrice.High;
                    dataEntered.LastTradedPrice.Low = fiveMinChartData.LastTradedPrice.Low;
                    dataEntered.LastTradedPrice.Open = fiveMinChartData.LastTradedPrice.Open;
                }

                dataEntered.Offer = new IgPublicApiData.ChartHlocModel();
                dataEntered.Offer.Close = fiveMinChartData.Offer.Close;
                dataEntered.Offer.Open = fiveMinChartData.Offer.Open;
                dataEntered.Offer.High = fiveMinChartData.Offer.High;
                dataEntered.Offer.Low = fiveMinChartData.Offer.Low;

                dataEntered.TickCount = fiveMinChartData.TickCount;
                dataEntered.UpdateTime = fiveMinChartData.UpdateTime.Value.ToLocalTime();

                //add to datagrid
                if (ChartMarketHistoryData.Count() >= 10)
                    ChartMarketHistoryData.RemoveAt(0);
                ChartMarketHistoryData.Add(dataEntered);
            }

            //update fiveminchartdata
            fiveMinChartData.ChartEpic = epic;
            fiveMinChartData.Bid = new IgPublicApiData.ChartHlocModel();
            fiveMinChartData.Bid.Close = candleUpdate.Bid.Close;
            fiveMinChartData.Bid.High = candleUpdate.Bid.High;
            fiveMinChartData.Bid.Low = candleUpdate.Bid.Low;
            fiveMinChartData.Bid.Open = candleUpdate.Bid.Open;

            fiveMinChartData.DayChange = candleUpdate.DayChange;
            fiveMinChartData.DayChangePct = candleUpdate.DayChangePct;
            fiveMinChartData.DayHigh = candleUpdate.DayHigh;
            fiveMinChartData.DayLow = candleUpdate.DayLow;
            fiveMinChartData.DayMidOpenPrice = candleUpdate.DayMidOpenPrice;
            fiveMinChartData.EndOfConsolidation = candleUpdate.EndOfConsolidation;
            fiveMinChartData.IncrimetalTradingVolume = candleUpdate.IncrimetalTradingVolume;

            if (candleUpdate.LastTradedVolume != null)
            {
                fiveMinChartData.LastTradedVolume = candleUpdate.LastTradedVolume;
                fiveMinChartData.LastTradedPrice = new IgPublicApiData.ChartHlocModel();
                fiveMinChartData.LastTradedPrice.Close = candleUpdate.LastTradedPrice.Close;
                fiveMinChartData.LastTradedPrice.High = candleUpdate.LastTradedPrice.High;
                fiveMinChartData.LastTradedPrice.Low = candleUpdate.LastTradedPrice.Low;
                fiveMinChartData.LastTradedPrice.Open = candleUpdate.LastTradedPrice.Open;
            }

            fiveMinChartData.Offer = new IgPublicApiData.ChartHlocModel();
            fiveMinChartData.Offer.Close = candleUpdate.Offer.Close;
            fiveMinChartData.Offer.Open = candleUpdate.Offer.Open;
            fiveMinChartData.Offer.High = candleUpdate.Offer.High;
            fiveMinChartData.Offer.Low = candleUpdate.Offer.Low;

            fiveMinChartData.TickCount = candleUpdate.TickCount;
            fiveMinChartData.UpdateTime = candleUpdate.UpdateTime.Value.ToLocalTime();
            
        }
    }
}

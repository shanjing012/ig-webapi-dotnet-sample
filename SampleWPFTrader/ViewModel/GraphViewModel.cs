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
using LiveCharts;
using LiveCharts.Wpf;
using LiveCharts.Defaults;
using System.Windows.Media;

namespace SampleWPFTrader.ViewModel
{
    public class GraphViewModel : ViewModelBase
    {
        // LS subscriptions...
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
        private string strFormat = "h:mm tt";
        private string selectedEpic;
        private string searchQuery;
        private string selectedLimitDistance;
        private string currencyTraded;
        private string positionOpenSize;
        private double chartLow;
        private double chartHigh;
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
        public string LimitDistance
        {
            get
            {
                return selectedLimitDistance;
            }
            set
            {
                selectedLimitDistance = value;
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
        public string PositionOpenSize
        {
            get
            {
                return positionOpenSize;
            }
            set
            {
                positionOpenSize = value;
            }
        }
        public double ChartLow
        {
            get
            {
                return chartLow;
            }
            set
            {
                chartLow = value;
                RaisePropertyChanged("ChartLow");
            }
        }
        public double ChartHigh
        {
            get
            {
                return chartHigh;
            }
            set
            {
                chartHigh = value;
                RaisePropertyChanged("ChartHigh");
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
        public RelayCommand OpenBuyPositionCommand
        {
            get;
            private set;
        }
        public RelayCommand OpenSellPositionCommand
        {
            get;
            private set;
        }
        public RelayCommand CloseAllPositionCommand
        {
            get;
            private set;
        }
        public RelayCommand ClosePositivePositionCommand
        {
            get;
            private set;
        }

        //data containers for our information
        public ObservableCollection<IgPublicApiData.ChartModel> ChartMarketData { get; set; }
        public ObservableCollection<IgPublicApiData.ChartModel> ChartMarketHistoryData { get; set; }
        public SeriesCollection SeriesCollection { get; set; }
        public ObservableCollection<IgPublicApiData.OrderModel> PositionData { get; set; }
        public IgPublicApiData.ChartModel fiveMinChartData { get; set; }
        public ObservableCollection<string> GraphLabels { get; set; }


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
            selectedLimitDistance = "0";
            positionOpenSize = "1";
            chartLow = 0;
            ChartHigh = 1;
            GraphLabels = new ObservableCollection<string>();

            //series collection try
            SeriesCollection = new SeriesCollection();
            //add values with SeriesCollection[0].Values....?
            SeriesCollection.Add(new OhlcSeries { Values = new ChartValues<OhlcPoint>() });

            //to avoid sync error or some shit
            System.Windows.Data.BindingOperations.EnableCollectionSynchronization(ChartMarketHistoryData, _graphLock);
            
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
            OpenBuyPositionCommand = new RelayCommand(OpenBuyPosition);
            OpenSellPositionCommand = new RelayCommand(OpenSellPosition);
            ClosePositivePositionCommand = new RelayCommand(ClosePositivePositions);
            CloseAllPositionCommand = new RelayCommand(CloseAllPositions);
            SearchEpicCommand.IsEnabled = true;
            SelectEpicCommand.IsEnabled = false;
            OpenBuyPositionCommand.IsEnabled = true;
            OpenSellPositionCommand.IsEnabled = true;
            ClosePositivePositionCommand.IsEnabled = true;
            CloseAllPositionCommand.IsEnabled = true;
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
                    PositionData.Clear();
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

        //get the historic data of epic
        //may need to set minute interval manually?
        private async void GetHistoricChart(string selectedEpic)
        {
            ChartMarketHistoryData.Clear();
            SeriesCollection[0].Values.Clear();

            try
            {
                if (LoggedIn && selectedEpic != "")
                {
                    var response = await igRestApiClient.priceSearchByNumV2(selectedEpic, "MINUTE_5", "18");

                    if (response && response.Response != null && response.Response.prices.Count != 0)
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

                            //add to chart
                            addToSeries(
                                double.Parse(((dataEntered.Bid.Open + dataEntered.Offer.Open)/2).ToString()),
                                double.Parse(((dataEntered.Bid.High + dataEntered.Offer.High) / 2).ToString()),
                                double.Parse(((dataEntered.Bid.Low + dataEntered.Offer.Low) / 2).ToString()),
                                double.Parse(((dataEntered.Bid.Close + dataEntered.Offer.Close) / 2).ToString()),
                                Convert.ToDateTime(response.Response.prices[i].snapshotTime).ToUniversalTime().ToLocalTime().ToString(strFormat),
                                (i == 0) //check for 1st
                            );
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

        public void OpenBuyPosition()
        {
            OpenPosition("BUY");
            PositionData.Clear();
            GetPositions(selectedEpic);
        }

        public void OpenSellPosition()
        {
            OpenPosition("SELL");
            PositionData.Clear();
            GetPositions(selectedEpic);
        }

        public async void OpenPosition(string direction)
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
                    position.direction = direction;
                    if (decimal.Parse(positionOpenSize) > 0 || selectedLimitDistance != "")
                        position.size = decimal.Parse(positionOpenSize);
                    else
                        position.size = 1;
                    position.orderType = "MARKET";
                    position.guaranteedStop = false;
                    if (selectedLimitDistance != "0" || selectedLimitDistance != "")
                        position.limitDistance = decimal.Parse(selectedLimitDistance);
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
                        position.size = Order.OrderSize;
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

        //only positive positions close
        public void ClosePositivePositions()
        {
            Collection<IgPublicApiData.OrderModel> ClosingOrders = new Collection<IgPublicApiData.OrderModel>();
            foreach(IgPublicApiData.OrderModel Model in PositionData)
            {
                if (Model.Profit > 0)
                {
                    ClosingOrders.Add(Model);
                }
            }

            ClosePosition(ClosingOrders);
            PositionData.Clear();
            GetPositions(selectedEpic);
        }

        //close all positions
        public void CloseAllPositions()
        {
            Collection<IgPublicApiData.OrderModel> ClosingOrders = new Collection<IgPublicApiData.OrderModel>();
            foreach (IgPublicApiData.OrderModel Model in PositionData)
            {
                ClosingOrders.Add(Model);
            }

            ClosePosition(ClosingOrders);
            PositionData.Clear();
            GetPositions(selectedEpic);
        }

        //get market details and set currency traded first. then ->
        //get open position with the epic being the provided
        //use clear position before this method.
        private async void GetPositions(string epic)
        {
            //to do this subscribe to position and close position 
            try
            {
                if (LoggedIn && selectedEpic != "")
                {
                    //get market details (currency only as of now)
                    var response = await igRestApiClient.marketDetails(selectedEpic);

                    if (response && (response.Response != null) && (response.Response.instrument != null))
                    {
                        var defaultCurrency = response.Response.instrument.currencies.Where(currency => currency.isDefault == true).First();
                        if (currencyTraded != defaultCurrency.name)
                        {
                            currencyTraded = defaultCurrency.name;
                            ApplicationViewModel.getInstance().AddStatusMessage("Currency set to: " + currencyTraded);
                        }
                    }

                    //get all positions
                    var response1 = await igRestApiClient.getOTCOpenPositionsV2();
                    
                    if(response1 && (response1.Response != null) && (response1.Response.positions != null))
                    {
                        foreach(var position in response1.Response.positions.Where(OpenPosition => OpenPosition.market.epic == selectedEpic))
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

                            if (positionWithDealID.Direction == "SELL")
                            {
                                positionWithDealID.Profit = (positionWithDealID.OpenLevel - ChartMarketData.First().Offer.Open) * positionWithDealID.OrderSize * 10000;
                            }
                            else
                            {
                                positionWithDealID.Profit = (ChartMarketData.First().Bid.Open - positionWithDealID.OpenLevel) * positionWithDealID.OrderSize * 10000;
                            }

                            PositionData.Add(positionWithDealID);
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
            if ((igStreamApiClient != null) && (_chart5MinuteSubscribedTableKey != null) && (_chartTickSubscribedTableKey != null) && (LoggedIn))
            {

                igStreamApiClient.UnsubscribeTableKey(_chart5MinuteSubscribedTableKey);
                _chart5MinuteSubscribedTableKey = null;

                igStreamApiClient.UnsubscribeTableKey(_chartTickSubscribedTableKey);
                _chartTickSubscribedTableKey = null;

                AddStatusMessage("GraphViewModel : Unsubscribing from candle data from charts");
            }
        }

        //update of candle tick data for position calculation of POINTS
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
                    orderModel.Profit = (orderModel.OpenLevel - positionChartTick.Offer) * 10000;// * orderModel.OrderSize;
                }
                else
                {
                    orderModel.Profit = (positionChartTick.Bid - orderModel.OpenLevel) * 10000;// * orderModel.OrderSize;
                }
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

            try
            {
                if (GraphLabels.Count != 0)
                {
                    if (candleUpdate.UpdateTime.Value.ToLocalTime().ToString(strFormat) != GraphLabels.Last())
                    {
                        //add to chart
                        addToSeries(
                            double.Parse(((candleUpdate.Bid.Open + candleUpdate.Offer.Open) / 2).ToString()),
                            double.Parse(((candleUpdate.Bid.High + candleUpdate.Offer.High) / 2).ToString()),
                            double.Parse(((candleUpdate.Bid.Low + candleUpdate.Offer.Low) / 2).ToString()),
                            double.Parse(((candleUpdate.Bid.Close + candleUpdate.Offer.Close) / 2).ToString()),
                            candleUpdate.UpdateTime.Value.ToLocalTime().ToString(strFormat),
                            false //check for 1st
                        );
                    }
                    else
                    {
                        //update the chart because time is same
                        SeriesCollection[0].Values.RemoveAt(SeriesCollection[0].Values.Count - 1);
                        GraphLabels.RemoveAt(GraphLabels.Count - 1);
                        addToSeries(
                            double.Parse(((candleUpdate.Bid.Open + candleUpdate.Offer.Open) / 2).ToString()),
                            double.Parse(((candleUpdate.Bid.High + candleUpdate.Offer.High) / 2).ToString()),
                            double.Parse(((candleUpdate.Bid.Low + candleUpdate.Offer.Low) / 2).ToString()),
                            double.Parse(((candleUpdate.Bid.Close + candleUpdate.Offer.Close) / 2).ToString()),
                            candleUpdate.UpdateTime.Value.ToLocalTime().ToString(strFormat),
                            false //check for 1st
                        );
                    }
                }
            }
            catch(Exception ex)
            {
                AddStatusMessage(ex.ToString());
            }
        }

        //bool first is for when it is the first entry, only used in get historic.
        //only remove historic when more than 20 
        private void addToSeries(double open, double high, double low, double close, string label, bool first)
        {
            OhlcPoint graphPoint = new OhlcPoint(open, high, low, close);

            if (ChartLow > graphPoint.Low - 0.00003 || first)
                ChartLow = graphPoint.Low - 0.00003;

            if (ChartHigh < graphPoint.High + 0.00003 || first)
                ChartHigh = graphPoint.High + 0.00003;

            if (SeriesCollection[0].Values.Count >= 20)
            {
                GraphLabels.RemoveAt(0);
                SeriesCollection[0].Values.RemoveAt(0);
            }
            GraphLabels.Add(label);
            SeriesCollection[0].Values.Add(graphPoint);

        }
    }
}

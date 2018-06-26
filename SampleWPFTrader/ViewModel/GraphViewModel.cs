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
                ApplicationViewModel.getInstance().AddStatusMessage("== Graph Tab Selected ==");
                // Get Rest Orders and then subscribe
                if (LoggedIn)
                {
                    ApplicationViewModel.getInstance().AddStatusMessage("Search for an epic corresponding to the market");
                }
                else
                {
                    ApplicationViewModel.getInstance().AddStatusMessage("Please log in first");
                }
            }
            else
            {
                ApplicationViewModel.getInstance().AddStatusMessage("Graph Tab de-selected");
                clearAll();
            }
        }

        public void clearAll()
        {
            //clear all stuff
            UnsubscribeFromCharts();
            SeriesCollection[0].Values.Clear();
            GraphLabels.Clear();
            PositionData.Clear();
            ComboBoxMarkets.Clear();
            selectedEpic = "";
        }

        //variables on the viewmodel
        private string strFormat = "h:mm tt"; //x axis label format
        private string selectedEpic;
        private string searchQuery;
        private string selectedLimitDistance;
        private string currencyTraded;
        private string positionOpenSize;
        private double chartLow;
        private double chartHigh;
        private double chartZoom;
        private double graphEntries;
        private string minuteInterval = "MINUTE_5"; //default 5 min
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
                return chartLow - (0.00003 * ChartZoom);
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
                return chartHigh + (0.00003 * ChartZoom);
            }
            set
            {
                chartHigh = value;
                RaisePropertyChanged("ChartHigh");
            }
        }
        public double ChartZoom
        {
            get
            {
                return chartZoom;
            }
            set
            {
                chartZoom = value;
                RaisePropertyChanged("ChartZoom");
                RaisePropertyChanged("ChartHigh");
                RaisePropertyChanged("ChartLow");
            }
        }
        public double GraphEntries
        {
            get
            {
                return graphEntries;
            }
            set
            {
                graphEntries = value;
                RaisePropertyChanged("GraphEntries");
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
        public RelayCommand SetOneMinuteCommand
        {
            get;
            private set;
        }
        public RelayCommand SetFiveMinuteCommand
        {
            get;
            private set;
        }
        public RelayCommand ShowOpenLevelCommand
        {
            get;
            private set;
        }

        //data containers for our information
        public SeriesCollection SeriesCollection { get; set; }
        public ObservableCollection<IgPublicApiData.OrderModel> PositionData { get; set; }
        public ObservableCollection<string> GraphLabels { get; set; }
        public ObservableCollection<String> ComboBoxMarkets { get; set; }

        //initialization
        public GraphViewModel()
        {
            InitialiseViewModel();

            //init data containers
            positionChartTick = new IgPublicApiData.InstrumentModel();
            ComboBoxMarkets = new ObservableCollection<String>();
            PositionData = new ObservableCollection<IgPublicApiData.OrderModel>();
            selectedLimitDistance = "0";
            positionOpenSize = "1";
            chartLow = 0;
            chartHigh = 1;
            chartZoom = 1;
            graphEntries = 50;
            GraphLabels = new ObservableCollection<string>();
            

            //series collection try
            SeriesCollection = new SeriesCollection();
            //add values with SeriesCollection[0].Values....?
            SeriesCollection.Add(new OhlcSeries { Values = new ChartValues<OhlcPoint>() }); //, MaxColumnWidth = 25

            //to avoid sync error or some shit
            //System.Windows.Data.BindingOperations.EnableCollectionSynchronization(ChartMarketHistoryData, _graphLock);

            //streaming of candlechart data (5 mins)
            _chart5MinuteSubscribedTableKey = new SubscribedTableKey();
            _chart5MinuteSubscription = new ChartCandleTableListerner();
            _chart5MinuteSubscription.Update += OnChartCandleData5MinuteUpdate;
            //streaming of candletick data
            _chartTickSubscribedTableKey = new SubscribedTableKey();
            _chartTickSubscription = new ChartTickTableListerner();
            _chartTickSubscription.Update += OnChartTickDataUpdate;
            
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
            SetOneMinuteCommand = new RelayCommand(SetOneMinuteChart);
            SetFiveMinuteCommand = new RelayCommand(SetFiveMinuteChart);
            ShowOpenLevelCommand = new RelayCommand(ShowOpenLevel);
            ShowOpenLevelCommand.IsEnabled = true;
            SearchEpicCommand.IsEnabled = true;
            SelectEpicCommand.IsEnabled = false;
            OpenBuyPositionCommand.IsEnabled = false;
            OpenSellPositionCommand.IsEnabled = false;
            ClosePositivePositionCommand.IsEnabled = false;
            CloseAllPositionCommand.IsEnabled = false;
            SetOneMinuteCommand.IsEnabled = false;
            SetFiveMinuteCommand.IsEnabled = false;
        }

        //search for epic to view
        public async void SearchEpic()
        {
            //get search query
            try
            {
                if (LoggedIn && searchQuery != "")
                {
                    var response = await igRestApiClient.searchMarket(searchQuery);

                    if (response && (response.Response != null) && (response.Response.markets != null))
                    {
                        ComboBoxMarkets.Clear();
                        SelectEpicCommand.IsEnabled = true;
                        foreach (var node in response.Response.markets)
                        {
                            ComboBoxMarkets.Add(node.epic);
                        }
                        
                        ApplicationViewModel.getInstance().AddStatusMessage(String.Format("Search epic data received for {0} nodes", response.Response.markets.Count));
                    }
                    else
                    {
                        ApplicationViewModel.getInstance().AddStatusMessage("Search epic data recieved for 0 nodes");
                    }
                }
                else
                {
                    ApplicationViewModel.getInstance().AddStatusMessage("Please log in first/Invalid Search query");
                }
            }
            catch (Exception ex)
            {
                ApplicationViewModel.getInstance().AddStatusMessage(ex.Message);
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
                    SetOneMinuteCommand.IsEnabled = true;
                    PositionData.Clear();
                    GetPositions(selectedEpic);
                    GetHistoricChart(selectedEpic);
                    OpenBuyPositionCommand.IsEnabled = true;
                    OpenSellPositionCommand.IsEnabled = true;
                }
                else
                {
                    ApplicationViewModel.getInstance().AddStatusMessage("Please log in first/Invalid epic");
                }
            }
            catch (Exception ex)
            {
                ApplicationViewModel.getInstance().AddStatusMessage(ex.Message);
            }
        }

        //get the historic data of epic
        //may need to set minute interval manually?
        private async void GetHistoricChart(string selectedEpic)
        {
            //ChartMarketHistoryData.Clear();
            GraphLabels.Clear();
            SeriesCollection[0].Values.Clear();

            try
            {
                if (LoggedIn && selectedEpic != "")
                {
                    var response = await igRestApiClient.priceSearchByNumV2(selectedEpic, minuteInterval, "49");
                    
                    if (response && response.Response != null && response.Response.prices.Count != 0)
                    {
                        ApplicationViewModel.getInstance().AddStatusMessage("Remaining allowances this week: " + response.Response.allowance.remainingAllowance + ".");

                        for (int i = 0; i < response.Response.prices.Count - 1; i++)
                        {
                            //add to chart
                            addToSeries(
                                double.Parse(((response.Response.prices[i].openPrice.bid + response.Response.prices[i].openPrice.ask) /2).ToString()),
                                double.Parse(((response.Response.prices[i].highPrice.bid + response.Response.prices[i].highPrice.ask) / 2).ToString()),
                                double.Parse(((response.Response.prices[i].lowPrice.bid + response.Response.prices[i].lowPrice.ask) / 2).ToString()),
                                double.Parse(((response.Response.prices[i].closePrice.bid + response.Response.prices[i].closePrice.ask) / 2).ToString()),
                                Convert.ToDateTime(response.Response.prices[i].snapshotTime).ToUniversalTime().ToLocalTime().ToString(strFormat),
                                (i == 0) //check for 1st
                            );
                        }
                    }
                    else
                    {
                        ApplicationViewModel.getInstance().AddStatusMessage("No response / No historic data");
                    }
                }
                else
                {
                    ApplicationViewModel.getInstance().AddStatusMessage("Please log in first/Invalid epic");
                }
            }
            catch (Exception ex)
            {
                ApplicationViewModel.getInstance().AddStatusMessage(ex.Message);
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
                    if (decimal.Parse(positionOpenSize) > 0)
                        position.size = decimal.Parse(positionOpenSize);
                    else
                        position.size = 1;
                    position.orderType = "MARKET";
                    position.guaranteedStop = false;
                    if (decimal.Parse(selectedLimitDistance) > 0)
                        position.limitDistance = decimal.Parse(selectedLimitDistance);
                    ApplicationViewModel.getInstance().AddStatusMessage(position.limitDistance.ToString());
                    position.forceOpen = true;
                    position.currencyCode = currencyTraded;
                    
                    //create position
                    var response = await igRestApiClient.createPositionV2(position);

                    if (response && (response.Response != null) && (response.Response.dealReference != null))
                    {
                        ApplicationViewModel.getInstance().AddStatusMessage("Created position. Deal Reference: " + response.Response.dealReference);
                    }
                    else
                    {
                        ApplicationViewModel.getInstance().AddStatusMessage("Cannot create position");
                    }
                }
                else
                {
                    ApplicationViewModel.getInstance().AddStatusMessage("Please log in first/Invalid epic");
                }
            }
            catch (Exception ex)
            {
                ApplicationViewModel.getInstance().AddStatusMessage(ex.Message);
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
                            ApplicationViewModel.getInstance().AddStatusMessage("Closed position. Deal Reference: " + response.Response.dealReference);
                        }
                        else
                        {
                            ApplicationViewModel.getInstance().AddStatusMessage("Cannot close position: " + response.Response.dealReference);
                        }
                    }
                }
                else
                {
                    ApplicationViewModel.getInstance().AddStatusMessage("Please log in first/Invalid epic");
                }
            }
            catch (Exception ex)
            {
                ApplicationViewModel.getInstance().AddStatusMessage(ex.Message);
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

                    if (response1 && (response1.Response != null) && (response1.Response.positions.Count > 0))
                    {
                        ClosePositivePositionCommand.IsEnabled = false;
                        CloseAllPositionCommand.IsEnabled = false;

                        foreach (var position in response1.Response.positions.Where(OpenPosition => OpenPosition.market.epic == selectedEpic))
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
                                positionWithDealID.Profit = (positionWithDealID.OpenLevel - positionChartTick.Offer) * 10000; //* positionWithDealID.OrderSize 
                            }
                            else
                            {
                                positionWithDealID.Profit = (positionChartTick.Bid - positionWithDealID.OpenLevel) * 10000; //* positionWithDealID.OrderSize
                            }

                            PositionData.Add(positionWithDealID);

                            ClosePositivePositionCommand.IsEnabled = true;
                            CloseAllPositionCommand.IsEnabled = true;
                        }
                    }
                    else
                    {
                        ApplicationViewModel.getInstance().AddStatusMessage("No Positions found");
                    }
                }
                else
                {
                    ApplicationViewModel.getInstance().AddStatusMessage("Please log in first/Invalid epic");
                }
            }
            catch (Exception ex)
            {
                ApplicationViewModel.getInstance().AddStatusMessage(ex.Message);
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
                    foreach (var epic in chartEpics)
                    {
                        ApplicationViewModel.getInstance().AddStatusMessage("Subscribing to Chart Data (CandleStick): " + epic);
                        ApplicationViewModel.getInstance().AddStatusMessage("Subscribing to Chart Tick Data: " + epic);
                    }
                    IGWebApiClient.ChartScale scale;
                    if (minuteInterval == "MINUTE_5")
                        scale = ChartScale.FiveMinute;
                    else
                        scale = ChartScale.OneMinute;

                    //chart five minute
                    _chart5MinuteSubscribedTableKey = igStreamApiClient.SubscribeToChartCandleData(chartEpics, scale, _chart5MinuteSubscription);
                    //chart tick
                    _chartTickSubscribedTableKey = igStreamApiClient.SubscribeToChartTicks(chartEpics, _chartTickSubscription);
                }
            }
            catch (Exception ex)
            {
                ApplicationViewModel.getInstance().AddStatusMessage("Exception when trying to subscribe to Chart Data: " + ex.Message);
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

                ApplicationViewModel.getInstance().AddStatusMessage("GraphViewModel : Unsubscribing from candle data from charts");
            }
        }

        //update of candle tick data for position calculation of POINTS
        private void OnChartTickDataUpdate(object sender, UpdateArgs<ChartTickData> e)
        {
            var updateTick = e.UpdateData;
            //update chart tick data here to update all positions
            positionChartTick.Bid = updateTick.Bid;
            positionChartTick.Offer = updateTick.Offer;

            //each ordermodel in position data collection, calculate the points
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

            try
            {
                if (GraphLabels.Count != 0 && SeriesCollection[0].Values.Count != 0)
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
                ApplicationViewModel.getInstance().AddStatusMessage(ex.ToString());
            }
        }

        //to add to the chart series (display chart)
        //bool first is for when it is the first entry, only used in get historic.
        //only remove historic when more than 20 
        private void addToSeries(double open, double high, double low, double close, string label, bool first)
        {
            OhlcPoint graphPoint = new OhlcPoint(open, high, low, close);

            if (ChartLow > graphPoint.Low || first)
                ChartLow = graphPoint.Low;

            if (ChartHigh < graphPoint.High || first)
                ChartHigh = graphPoint.High;

            while (SeriesCollection[0].Values.Count >= graphEntries)
            {
                GraphLabels.RemoveAt(0);
                SeriesCollection[0].Values.RemoveAt(0);
            }
            GraphLabels.Add(label);
            SeriesCollection[0].Values.Add(graphPoint);

        }

        //set chart to one minute, get historic data
        private void SetOneMinuteChart()
        {
            minuteInterval = "MINUTE";
            SetFiveMinuteCommand.IsEnabled = true;
            SetOneMinuteCommand.IsEnabled = false;
            try
            {
                if (LoggedIn && selectedEpic != "")
                {
                    //add selectedepic to the epic string
                    string[] epic = new string[] { selectedEpic };
                    SubscribeToCharts(epic);
                    GetHistoricChart(selectedEpic);
                }
                else
                {
                    ApplicationViewModel.getInstance().AddStatusMessage("Please log in first/Invalid epic");
                }
            }
            catch (Exception ex)
            {
                ApplicationViewModel.getInstance().AddStatusMessage(ex.Message);
            }
        }

        //set chart to five minute, get historic data
        private void SetFiveMinuteChart()
        {
            minuteInterval = "MINUTE_5";
            SetOneMinuteCommand.IsEnabled = true;
            SetFiveMinuteCommand.IsEnabled = false;
            try
            {
                if (LoggedIn && selectedEpic != "")
                {
                    //add selectedepic to the epic string
                    string[] epic = new string[] { selectedEpic };
                    SubscribeToCharts(epic);
                    GetHistoricChart(selectedEpic);
                }
                else
                {
                    ApplicationViewModel.getInstance().AddStatusMessage("Please log in first/Invalid epic");
                }
            }
            catch (Exception ex)
            {
                ApplicationViewModel.getInstance().AddStatusMessage(ex.Message);
            }
        }

        //Show open level
        private void ShowOpenLevel()
        {
        }

    }
}

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
        private Boolean updateChart = false;
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

        //variables for algo stuff
        private Boolean entry1; //entry 1 checkbox
        private Boolean exit1; //exit 1 checkbox
        private Boolean entry2; //entry 2 checkbox
        private Boolean exit2; //exit 2 checkbox
        private Boolean entry3; //entry 2 checkbox
        private Boolean exit3; //exit 2 checkbox
        private Boolean entry4; //entry 2 checkbox
        private Boolean exit4; //exit 2 checkbox

        //selectors for algo stuff
        public Boolean EntryOne
        {
            get
            {
                return entry1;
            }
            set
            {
                entry1 = value;
                RaisePropertyChanged("EntryOne");
            }
        } //Test one checkbox
        public Boolean ExitOne
        {
            get
            {
                return exit1;
            }
            set
            {
                exit1 = value;
                RaisePropertyChanged("ExitOne");
            }
        } //Test one checkbox
        public Boolean EntryTwo
        {
            get
            {
                return entry2;
            }
            set
            {
                entry2 = value;
                RaisePropertyChanged("EntryTwo");
            }
        } //Test one checkbox
        public Boolean ExitTwo
        {
            get
            {
                return exit2;
            }
            set
            {
                exit2 = value;
                RaisePropertyChanged("ExitTwo");
            }
        } //Test one checkbox
        public Boolean EntryThree
        {
            get
            {
                return entry3;
            }
            set
            {
                entry3 = value;
                RaisePropertyChanged("EntryThree");
            }
        }
        public Boolean ExitThree
        {
            get
            {
                return exit3;
            }
            set
            {
                exit3 = value;
                RaisePropertyChanged("ExitThree");
            }
        }
        public Boolean EntryFour
        {
            get
            {
                return entry4;
            }
            set
            {
                entry4 = value;
                RaisePropertyChanged("EntryFour");
            }
        }
        public Boolean ExitFour
        {
            get
            {
                return exit4;
            }
            set
            {
                exit4 = value;
                RaisePropertyChanged("ExitFour");
            }
        }

        private class Trend
        {
            private double lowerResistance;
            private double upperResistance;
            private Boolean opened;
            private Boolean currentTrend;

            public Trend(Boolean openedLast)
            {
                opened = openedLast;
            }

            public Boolean getOpened()
            {
                return opened;
            }
            public void setOpened(Boolean openedLast)
            {
                opened = openedLast;
            }
            public Boolean getCurrentTrend()
            {
                return currentTrend;
            }
            public void setCurrentTrend(Boolean currTrend)
            {
                currentTrend = currTrend;
            }
            public double getUpperResistance()
            {
                return upperResistance;
            }
            public void setUpperResistance(double upResistance)
            {
                upperResistance = upResistance;
            }
            public double getLowerResistance()
            {
                return lowerResistance;
            }
            public void setLowerResistance(double lowResistance)
            {
                lowerResistance = lowResistance;
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

        private Trend t1 = new Trend(false);

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
            System.Windows.Data.BindingOperations.EnableCollectionSynchronization(PositionData, _graphLock);

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
            SearchEpicCommand.IsEnabled = true;
            SelectEpicCommand.IsEnabled = false;
            OpenBuyPositionCommand.IsEnabled = false;
            OpenSellPositionCommand.IsEnabled = false;
            ClosePositivePositionCommand.IsEnabled = false;
            CloseAllPositionCommand.IsEnabled = false;
            SetOneMinuteCommand.IsEnabled = false;
            SetFiveMinuteCommand.IsEnabled = false;
        }

        /*==================================================================================
                SearchEpic()
                search for epic from IGLabs using the search query provided on UI
                populate dropdownbox with the results.
        ==================================================================================*/
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

        /*==================================================================================
                SelectEpic()
                select epic from the dropdownbox
                use this epic to initialize the chart and positions.
        ==================================================================================*/
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
                    CloseAllPositionCommand.IsEnabled = true;
                    ClosePositivePositionCommand.IsEnabled = true;
                    EntryOne = false;
                    EntryTwo = false;
                    EntryThree = false;
                    EntryFour = false;
                    ExitOne = false;
                    ExitTwo = false;
                    ExitThree = false;
                    ExitFour = false;
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

        /*==================================================================================
                GetHistoricChart()
                - string selectedEpic
                Uses selectedEpic to find the historic chart data from IGLabs
        ==================================================================================*/
        private async void GetHistoricChart(string selectedEpic)
        {
            //ChartMarketHistoryData.Clear();
            GraphLabels.Clear();
            SeriesCollection[0].Values.Clear();
            chartHigh = 1;
            chartLow = 0;

            try
            {
                if (LoggedIn && selectedEpic != "")
                {
                    var response = await igRestApiClient.priceSearchByNumV2(selectedEpic, minuteInterval, graphEntries.ToString());

                    if (response && response.Response != null && response.Response.prices.Count != 0)
                    {
                        ApplicationViewModel.getInstance().AddStatusMessage("Remaining allowances this week: " + response.Response.allowance.remainingAllowance + ".");

                        for (int i = 0; i < response.Response.prices.Count - 1; i++)
                        {
                            //add to chart
                            addToSeries(
                                double.Parse(((response.Response.prices[i].openPrice.bid + response.Response.prices[i].openPrice.ask) / 2).ToString()),
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

        /*==================================================================================
                OpenBuyPosition()
                Method wrapper for OpenPosition
                Open a BUY position and updates position data UI
                Method called by UI to open a BUY position
        ==================================================================================*/
        public void OpenBuyPosition()
        {
            OpenPosition("BUY", -1, -1);
            PositionData.Clear();
            GetPositions(selectedEpic);
        }


        /*==================================================================================
                OpenSellPosition()
                Method wrapper for OpenPosition
                Open a SELL position and updates position data UI
                Method called by UI to open a SELL position
        ==================================================================================*/
        public void OpenSellPosition()
        {
            OpenPosition("SELL", -1, -1);
            PositionData.Clear();
            GetPositions(selectedEpic);
        }

        /*==================================================================================
                OpenBuyPosition() (Overwrite)
                - decimal limit (limit distance (limit of earn))
                - decimal stop (stop distance, must be above 2)
                Method wrapper for OpenPosition
                Open a BUY position and updates position data UI
        ==================================================================================*/
        public void OpenBuyPosition(decimal limit, decimal stop)
        {
            OpenPosition("BUY", limit, stop);
            PositionData.Clear();
            GetPositions(selectedEpic);
        }

        /*==================================================================================
                OpenSellPosition() (Overwrite)
                - decimal limit (limit distance (limit of earn))
                - decimal stop (stop distance, must be above 2)
                Method wrapper for OpenPosition
                Open a SELL position and updates position data UI
        ==================================================================================*/
        public void OpenSellPosition(decimal limit, decimal stop)
        {
            OpenPosition("SELL", limit, stop);
            PositionData.Clear();
            GetPositions(selectedEpic);
        }

        /*==================================================================================
                OpenPosition()
                - string direction (direction "SELL" or "BUY")
                - decimal limit (limit distance (limit of earn))
                - decimal stop (stop distance, must be above 2)
                open a position with the instrument epic
                size is given by the UI
        ==================================================================================*/
        public async void OpenPosition(string direction, decimal limit, decimal stop)
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
                    //if (decimal.Parse(selectedLimitDistance) > 0)
                    //    position.limitDistance = decimal.Parse(selectedLimitDistance);
                    if (limit > 0)
                        position.limitDistance = limit;
                    if (stop > 0)
                        position.stopDistance = stop;
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

        /*==================================================================================
                ClosePosition()
                - Collection<IgPublicApiData.OrderModel> ClosingOrders (collection of all positions to close.)
                close all positions provided in the collection
        ==================================================================================*/
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

        /*==================================================================================
                ClosePositivePositions()
                close all current POSITIVE positions in the positions data list
        ==================================================================================*/
        public void ClosePositivePositions()
        {
            if (PositionData.Count != 0)
            {
                Collection<IgPublicApiData.OrderModel> ClosingOrders = new Collection<IgPublicApiData.OrderModel>();
                foreach (IgPublicApiData.OrderModel Model in PositionData)
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
        }

        /*==================================================================================
                CloseAllPositions()
                close all current positions in the positions data list
        ==================================================================================*/
        public void CloseAllPositions()
        {
            if (PositionData.Count != 0)
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
        }

        /*==================================================================================
                GetPositions()
                - string epic (epic of instrument to get positions from)
                get market details and set currency traded first. 
                then get open position with the epic being the provided
                ! use clear position before this method.
        ==================================================================================*/
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

                        }
                    }
                    else
                    {
                        //ApplicationViewModel.getInstance().AddStatusMessage("No Positions found");
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

        /*==================================================================================
                SubscribeToCharts()
                - string[] chartEpics (chart epics should contain only 1 epic!!)
                subscribe to IGLabs lightstreamer subscription
        ==================================================================================*/
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

        /*==================================================================================
                UnsubscribeFromCharts()
                unsub from IGLabs lightstreamer subscription
        ==================================================================================*/
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

        /*==================================================================================
                OnChartTickDataUpdate()
                update position points from subscription api from IGlabs
        ==================================================================================*/
        private void OnChartTickDataUpdate(object sender, UpdateArgs<ChartTickData> e)
        {
            var updateTick = e.UpdateData;
            //update chart tick data here to update all positions
            positionChartTick.Bid = updateTick.Bid;
            positionChartTick.Offer = updateTick.Offer;

            //each ordermodel in position data collection, calculate the points
            foreach (IgPublicApiData.OrderModel orderModel in PositionData)
            {
                if (orderModel.Direction == "SELL")
                {
                    orderModel.Profit = (orderModel.OpenLevel - positionChartTick.Offer) * 10000;// * orderModel.OrderSize;
                }
                else
                {
                    orderModel.Profit = (positionChartTick.Bid - orderModel.OpenLevel) * 10000;// * orderModel.OrderSize;
                }
            }
        }

        /*==================================================================================
                OnChartCandleData5MinuteUpdate()
                update 5 minute chart data from the subscription api from IGlabs
                Determine entry points here
        ==================================================================================*/
        private void OnChartCandleData5MinuteUpdate(object sender, UpdateArgs<ChartCandelData> e)
        {

            var candleUpdate = e.UpdateData;
            var tempEpic = e.ItemName.Replace("CHART:", "");
            var tempArray = tempEpic.Split(':');
            var epic = tempArray[0];
            var time = tempArray[1];

            try
            {
                //update the graph first
                if (GraphLabels.Count != 0 && SeriesCollection[0].Values.Count != 0)
                {

                    if (candleUpdate.UpdateTime.Value.ToLocalTime().ToString(strFormat) != GraphLabels.Last())
                    {
                        //this is for new 5minute entry
                        //add to chart
                        addToSeries(
                            double.Parse(((candleUpdate.Bid.Open + candleUpdate.Offer.Open) / 2).ToString()),
                            double.Parse(((candleUpdate.Bid.High + candleUpdate.Offer.High) / 2).ToString()),
                            double.Parse(((candleUpdate.Bid.Low + candleUpdate.Offer.Low) / 2).ToString()),
                            double.Parse(((candleUpdate.Bid.Close + candleUpdate.Offer.Close) / 2).ToString()),
                            candleUpdate.UpdateTime.Value.ToLocalTime().ToString(strFormat),
                            false //check for 1st
                        );

                        if (entry1) //if the 
                        {
                            /*====================================================================
                            Entry 1:
                            last 20 minutes all upwards / downwards trend, open a position which follows that trend
                            ====================================================================*/

                            ApplicationViewModel.getInstance().AddStatusMessage("-----------");
                            //if last 4 are green, follow open buy, if last 4 are negative, follow open sell
                            OhlcPoint pt1 = SeriesCollection[0].Values[SeriesCollection[0].Values.Count - 2] as OhlcPoint;
                            OhlcPoint pt2 = SeriesCollection[0].Values[SeriesCollection[0].Values.Count - 3] as OhlcPoint;
                            OhlcPoint pt3 = SeriesCollection[0].Values[SeriesCollection[0].Values.Count - 4] as OhlcPoint;
                            OhlcPoint pt4 = SeriesCollection[0].Values[SeriesCollection[0].Values.Count - 5] as OhlcPoint;
                            ApplicationViewModel.getInstance().AddStatusMessage("TREND: " + currentPointGreen(pt1) + " " + currentPointGreen(pt2) + " " + currentPointGreen(pt3) + " " + currentPointGreen(pt4));
                            ApplicationViewModel.getInstance().AddStatusMessage("Trend 1: " + t1.getOpened());
                            if (!t1.getOpened())
                            {
                                if (currentPointGreen(pt1) && currentPointGreen(pt2) && currentPointGreen(pt3) && currentPointGreen(pt4) && ((chartHigh - pt1.High) < 0.0005 || pt1.High > chartHigh))
                                {
                                    OpenBuyPosition();
                                    //want to stop here running of algorithm.
                                    t1.setOpened(true);
                                    t1.setCurrentTrend(true);

                                }
                                else if (!currentPointGreen(pt1) && !currentPointGreen(pt2) && !currentPointGreen(pt3) && !currentPointGreen(pt4) && ((pt1.Low - chartLow) < 0.0005 || pt1.Low < chartHigh))
                                {
                                    OpenSellPosition();
                                    t1.setOpened(true);
                                    t1.setCurrentTrend(false);
                                }
                            } else
                            {
                                if (t1.getCurrentTrend() != currentPointGreen(pt1))
                                    t1.setOpened(false);
                            }
                            ApplicationViewModel.getInstance().AddStatusMessage("--ENTRY 1--");

                        }
                        if (entry2)
                        {
                            /*====================================================================
                            Entry 2:
                            10 minutes downward, 5 minutes upward followed by a steep increase of 0.0003 points => Open a sell position
                            10 minutes upward, 5 minutes downward followed by a steep decrease of 0.0003 points => Open a buy position
                            ====================================================================*/

                            ApplicationViewModel.getInstance().AddStatusMessage("-----------");
                            //red red green
                            //calculate the trueness of the algo:
                            int green1 = SeriesCollection[0].Values.Count - 3;
                            int red1 = SeriesCollection[0].Values.Count - 4;
                            int red2 = SeriesCollection[0].Values.Count - 5;
                            OhlcPoint currPoint = SeriesCollection[0].Values[SeriesCollection[0].Values.Count - 2] as OhlcPoint;
                            OhlcPoint prevPt1 = SeriesCollection[0].Values[green1] as OhlcPoint;
                            OhlcPoint prevPt2 = SeriesCollection[0].Values[red1] as OhlcPoint;
                            OhlcPoint prevPt3 = SeriesCollection[0].Values[red2] as OhlcPoint;
                            if (currPoint.Close - currPoint.Open > 0.00030)
                            {
                                ApplicationViewModel.getInstance().AddStatusMessage("open - close > 30, green bar");
                                ApplicationViewModel.getInstance().AddStatusMessage("TREND: " + currentPointGreen(prevPt1) + " " + !currentPointGreen(prevPt2) + " " + !currentPointGreen(prevPt3));
                                if (currentPointGreen(prevPt1) && !currentPointGreen(prevPt2) && !currentPointGreen(prevPt3))
                                {
                                    ApplicationViewModel.getInstance().AddStatusMessage("conditions fulfilled, should open sell here");
                                    OpenSellPosition();
                                }
                            } else if (currPoint.Close - currPoint.Open < -0.00030)
                            {
                                ApplicationViewModel.getInstance().AddStatusMessage("open - close < -30, red bar");
                                ApplicationViewModel.getInstance().AddStatusMessage("TREND: " + !currentPointGreen(prevPt1) + " " + currentPointGreen(prevPt2) + " " + currentPointGreen(prevPt3));
                                if (!currentPointGreen(prevPt1) && currentPointGreen(prevPt2) && currentPointGreen(prevPt3))
                                {
                                    ApplicationViewModel.getInstance().AddStatusMessage("conditions fulfilled, should open buy here");
                                    OpenBuyPosition();
                                }
                            }
                            ApplicationViewModel.getInstance().AddStatusMessage("--ENTRY 2--");
                        }
                        if (entry3)
                        {
                            /*====================================================================
                            Entry 3:
                            15minutes of downward and 15minutes of upward trend => Open a sell position
                            ====================================================================*/

                            ApplicationViewModel.getInstance().AddStatusMessage("-----------");

                            OhlcPoint pt1 = SeriesCollection[0].Values[SeriesCollection[0].Values.Count - 2] as OhlcPoint;
                            OhlcPoint pt2 = SeriesCollection[0].Values[SeriesCollection[0].Values.Count - 3] as OhlcPoint;
                            OhlcPoint pt3 = SeriesCollection[0].Values[SeriesCollection[0].Values.Count - 4] as OhlcPoint;
                            OhlcPoint pt4 = SeriesCollection[0].Values[SeriesCollection[0].Values.Count - 5] as OhlcPoint;
                            OhlcPoint pt5 = SeriesCollection[0].Values[SeriesCollection[0].Values.Count - 6] as OhlcPoint;
                            OhlcPoint pt6 = SeriesCollection[0].Values[SeriesCollection[0].Values.Count - 7] as OhlcPoint;

                            if (currentPointGreen(pt1) && currentPointGreen(pt2) && currentPointGreen(pt3) && !currentPointGreen(pt4) && !currentPointGreen(pt5) && !currentPointGreen(pt6))
                            {
                                ApplicationViewModel.getInstance().AddStatusMessage("3 negative, 3 positive, predict negative next");
                                OpenSellPosition();
                            }
                            ApplicationViewModel.getInstance().AddStatusMessage("--ENTRY 3--");
                        }
                        if (entry4)
                        {
                            /*====================================================================
                            Entry 4:
                            Steep increase of 0.0007 points => Open a buy position
                            Steep decrease of 0.0007 points => Open a sell position
                            ====================================================================*/

                            ApplicationViewModel.getInstance().AddStatusMessage("-----------");
                            OhlcPoint pt = SeriesCollection[0].Values[SeriesCollection[0].Values.Count - 2] as OhlcPoint;
                            if (pt.Close - pt.Open > 0.0007)
                            {
                                ApplicationViewModel.getInstance().AddStatusMessage("Steep of +0.0007");
                                OpenBuyPosition();
                            }
                            if (pt.Close - pt.Open < -0.0007)
                            {
                                ApplicationViewModel.getInstance().AddStatusMessage("Steep of -0.0007");
                                OpenSellPosition();
                            }
                            ApplicationViewModel.getInstance().AddStatusMessage("--ENTRY 4--");
                        }
                        if (exit1)
                        {
                        }
                        if (exit2)
                        {
                        }
                        if (exit3)
                        {
                        }
                        if (exit4)
                        {
                        }

                        PositionData.Clear();
                        GetPositions(selectedEpic);
                    }
                    else //update the chart first entry.
                    {
                        //update the chart because time is same
                        //SeriesCollection[0].Values.RemoveAt(SeriesCollection[0].Values.Count - 1);
                        //GraphLabels.RemoveAt(GraphLabels.Count - 1);
                        updateSeries(
                            double.Parse(((candleUpdate.Bid.Open + candleUpdate.Offer.Open) / 2).ToString()),
                            double.Parse(((candleUpdate.Bid.High + candleUpdate.Offer.High) / 2).ToString()),
                            double.Parse(((candleUpdate.Bid.Low + candleUpdate.Offer.Low) / 2).ToString()),
                            double.Parse(((candleUpdate.Bid.Close + candleUpdate.Offer.Close) / 2).ToString()),
                            candleUpdate.UpdateTime.Value.ToLocalTime().ToString(strFormat)
                        );                        
                    }
                }
            }
            catch (Exception ex)
            {
                ApplicationViewModel.getInstance().AddStatusMessage(ex.ToString());
            }
        }

        /*==================================================================================
                addToSeries()
                - double open
                - double high
                - double low
                - double close
                - string label (chart label)
                - boolean first (Whether it is the first in the chart.)
                add to the chart series (display chart)
                bool first is for when it is the first entry, only used in get historic.
                only remove historic when more than graphEntries 
        ==================================================================================*/
        private void addToSeries(double open, double high, double low, double close, string label, bool first)
        {
            OhlcPoint graphPoint = new OhlcPoint(open, high, low, close);

            if (ChartLow > graphPoint.Low || first)
                ChartLow = graphPoint.Low;

            if (ChartHigh < graphPoint.High || first)
                ChartHigh = graphPoint.High;

            while (SeriesCollection[0].Values.Count >= graphEntries)
            {
                var toBeRemoved = SeriesCollection[0].Values[0] as OhlcPoint;
                ApplicationViewModel.getInstance().AddStatusMessage(toBeRemoved.High + " " + toBeRemoved.Low + " tobermeoved");
                ApplicationViewModel.getInstance().AddStatusMessage(chartHigh + " " + chartLow + " chartdata");

                if (toBeRemoved.Low == chartLow)
                {
                    GraphLabels.RemoveAt(0);
                    SeriesCollection[0].Values.RemoveAt(0);
                    //find new low
                    var newLow = chartHigh;
                    foreach(OhlcPoint point in SeriesCollection[0].Values)
                    {
                        if (point.Low < newLow)
                            newLow = point.Low;
                    }
                    ChartLow = newLow;
                }
                else if (toBeRemoved.High == chartHigh)
                {
                    GraphLabels.RemoveAt(0);
                    SeriesCollection[0].Values.RemoveAt(0);
                    //find new high
                    var newHigh = chartLow;
                    foreach (OhlcPoint point in SeriesCollection[0].Values)
                    {
                        if (point.High > newHigh)
                            newHigh = point.High;
                    }
                    ChartHigh = newHigh;
                }
                else
                {
                    GraphLabels.RemoveAt(0);
                    SeriesCollection[0].Values.RemoveAt(0);
                }
            }
            GraphLabels.Add(label);
            SeriesCollection[0].Values.Add(graphPoint);
        }


        /*==================================================================================
                updateSeries()
                - double open
                - double high
                - double low
                - double close
                - string label (chart label)
                to update the first entry of the series (display chart)
                get the last of the ohlcpoint and change it.
        ==================================================================================*/
        private void updateSeries(double open, double high, double low, double close, string label)
        {
            OhlcPoint graphPoint = SeriesCollection[0].Values[SeriesCollection[0].Values.Count - 1] as OhlcPoint;

            //update the point
            if (graphPoint.Open != open)
                graphPoint.Open = open;
            if (graphPoint.High != high)
            {
                graphPoint.High = high;
                if (ChartHigh < high)
                    ChartHigh = high;
            }
            if (graphPoint.Low != low)
            {
                graphPoint.Low = low;
                if (ChartLow > low)
                    ChartLow = low;
            }
            if (graphPoint.Close != close)
                graphPoint.Close = close;
        }

        /*==================================================================================
                SetOneMinuteChart()
                change chart to populate 1 minute data with historic data
        ==================================================================================*/
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

        /*==================================================================================
                SetFiveMinuteChart()
                change chart to populate 5 minute data with historic data
        ==================================================================================*/
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

        /*==================================================================================
                currentPointGreen()
                ! Returns true or false value (Boolean)
                - OhlcPoint point (OhlcPoint in chart)
                calculate whether OhlcPoint is green or red (upward or downward)
        ==================================================================================*/
        private Boolean currentPointGreen(OhlcPoint point)
        {
            if (point.Close > point.Open)
                return true;
            else
                return false;
        }
        
    }
}

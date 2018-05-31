using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IGWebApiClient;
using Lightstreamer.DotNet.Client;
using SampleWPFTrader.Model;
using dto.endpoint.search;
using System.Windows.Data;

namespace SampleWPFTrader.ViewModel
{
    public class GraphViewModel : ViewModelBase
    {
        // LS subscriptions...
        private SubscribedTableKey _chartSubscribedTableKey;
        private ChartCandleTableListerner _chartSubscription;
        private Object _graphLock = new Object();

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
                AddStatusMessage("Graph Tab selected");
                // Get Rest Orders and then subscribe
                if (LoggedIn)
                {
                    //GetBrowseMarketsRoot();
                    AddStatusMessage("Get browse root.");
                }
                else
                {
                    AddStatusMessage("Please log in first");
                }
            }
            else
            {
                AddStatusMessage("Browse Tab de-selected");
                /*UnsubscribeFromGraphPrices();*/
            }
        }  

        //variables on the viewmodel
        private string selectedEpic;
        private string searchQuery;
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
        public ObservableCollection<String> ComboBoxMarkets { get; set; }
        public CollectionViewSource ChartMarketDataView { get; set; }

        //initialization
        public GraphViewModel()
        {
            InitialiseViewModel();
            ChartMarketData = new ObservableCollection<IgPublicApiData.ChartModel>();
            ChartMarketHistoryData = new ObservableCollection<IgPublicApiData.ChartModel>();
            ChartMarketDataView = new CollectionViewSource();
            ChartMarketDataView.Source = ChartMarketHistoryData;
            ComboBoxMarkets = new ObservableCollection<String>();

            System.Windows.Data.BindingOperations.EnableCollectionSynchronization(ChartMarketHistoryData, _graphLock);

            _chartSubscribedTableKey = new SubscribedTableKey();
            ChartMarketDataView.SortDescriptions.Add(new System.ComponentModel.SortDescription("UpdateTime", System.ComponentModel.ListSortDirection.Descending));
            _chartSubscription = new ChartCandleTableListerner();
            _chartSubscription.Update += OnChartCandleDataUpdate;

            WireCommands();
        }
        
        //wire button commands
        private void WireCommands()
        {
            SearchEpicCommand = new RelayCommand(SearchEpic);
            SelectEpicCommand = new RelayCommand(SelectEpic);
            OpenPositionCommand = new RelayCommand(OpenPosition);
            ClosePositionCommand = new RelayCommand(ClosePosition);
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
                        
                        AddStatusMessage(String.Format("Browse Market data received for {0} nodes", response.Response.markets.Count));
                    }
                    else
                    {
                        AddStatusMessage("BrowseMarkets : no browse root nodes");
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

        public async void OpenPosition()
        {
            //get search query
            try
            {
                if (LoggedIn && selectedEpic != "")
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

                        AddStatusMessage(String.Format("Browse Market data received for {0} nodes", response.Response.markets.Count));
                    }
                    else
                    {
                        AddStatusMessage("BrowseMarkets : no browse root nodes");
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

        public void ClosePosition()
        {
            AddStatusMessage("Add fucking status message");

        }

        private void SubscribeToCharts(string[] chartEpics)
        {
            try
            {
                if (igStreamApiClient != null)
                {
                    ChartMarketData.Clear();
                    foreach (var epic in chartEpics)
                    {
                        IgPublicApiData.ChartModel ccd = new IgPublicApiData.ChartModel();
                        ccd.ChartEpic = epic;
                        ChartMarketData.Add(ccd);

                        AddStatusMessage("Subscribing to Chart Data (CandleStick ): " + ccd.ChartEpic);
                    }

                    _chartSubscribedTableKey = igStreamApiClient.SubscribeToChartCandleData(chartEpics, ChartScale.OneSecond, _chartSubscription);

                }
            }
            catch (Exception ex)
            {
                AddStatusMessage("Exception when trying to subscribe to Chart Candle Data: " + ex.Message);
            }
        }

        private void UnsubscribeFromCharts()
        {
            if ((igStreamApiClient != null) && (_chartSubscribedTableKey != null) && (LoggedIn))
            {
                igStreamApiClient.UnsubscribeTableKey(_chartSubscribedTableKey);
                _chartSubscribedTableKey = null;

                AddStatusMessage("GraphViewModel : Unsubscribing from candle data from charts");
            }
        }

        //update of candle chart data
        private void OnChartCandleDataUpdate(object sender, UpdateArgs<ChartCandelData> e)
        {
            var candleUpdate = e.UpdateData;
            var tempEpic = e.ItemName.Replace("CHART:", "");
            var tempArray = tempEpic.Split(':');
            var epic = tempArray[0];
            var time = tempArray[1]; //update time here?

            foreach (var candleData in ChartMarketData.Where(chartItem => chartItem.ChartEpic == epic))
            {
                if (!candleData.UpdateTime.Equals(candleUpdate.UpdateTime) && candleData.UpdateTime != null)
                {
                    UpdateChartHistory(candleData);
                }

                candleData.UpdateTime = candleUpdate.UpdateTime;

                candleData.Bid = new IgPublicApiData.ChartHlocModel();
                candleData.Bid.Close = candleUpdate.Bid.Close;
                candleData.Bid.High = candleUpdate.Bid.High;
                candleData.Bid.Low = candleUpdate.Bid.Low;
                candleData.Bid.Open = candleUpdate.Bid.Open;

                candleData.DayChange = candleUpdate.DayChange;
                candleData.DayChangePct = candleUpdate.DayChangePct;
                candleData.DayHigh = candleUpdate.DayHigh;
                candleData.DayLow = candleUpdate.DayLow;
                candleData.DayMidOpenPrice = candleUpdate.DayMidOpenPrice;
                candleData.EndOfConsolidation = candleUpdate.EndOfConsolidation;
                candleData.IncrimetalTradingVolume = candleUpdate.IncrimetalTradingVolume;

                if (candleUpdate.LastTradedVolume != null)
                {
                    candleData.LastTradedVolume = candleUpdate.LastTradedVolume;
                    candleData.LastTradedPrice = new IgPublicApiData.ChartHlocModel();
                    candleData.LastTradedPrice.Close = candleUpdate.LastTradedPrice.Close;
                    candleData.LastTradedPrice.High = candleUpdate.LastTradedPrice.High;
                    candleData.LastTradedPrice.Low = candleUpdate.LastTradedPrice.Low;
                    candleData.LastTradedPrice.Open = candleUpdate.LastTradedPrice.Open;
                }

                candleData.Offer = new IgPublicApiData.ChartHlocModel();
                candleData.Offer.Close = candleUpdate.Offer.Close;
                candleData.Offer.Open = candleUpdate.Offer.Open;
                candleData.Offer.High = candleUpdate.Offer.High;
                candleData.Offer.Low = candleUpdate.Offer.Low;

                candleData.TickCount = candleUpdate.TickCount;

            }
        }

        //create copy of current chart data at specific time and add to second drop down.
        private void UpdateChartHistory(IgPublicApiData.ChartModel candleData)
        {
            //create new IgPublicApiData
            IgPublicApiData.ChartModel data1 = new IgPublicApiData.ChartModel();

            data1.ChartEpic = candleData.ChartEpic;
            data1.Bid = new IgPublicApiData.ChartHlocModel();
            data1.Bid.Close = candleData.Bid.Close;
            data1.Bid.High = candleData.Bid.High;
            data1.Bid.Low = candleData.Bid.Low;
            data1.Bid.Open = candleData.Bid.Open;

            data1.DayChange = candleData.DayChange;
            data1.DayChangePct = candleData.DayChangePct;
            data1.DayHigh = candleData.DayHigh;
            data1.DayLow = candleData.DayLow;
            data1.DayMidOpenPrice = candleData.DayMidOpenPrice;
            data1.EndOfConsolidation = candleData.EndOfConsolidation;
            data1.IncrimetalTradingVolume = candleData.IncrimetalTradingVolume;

            data1.LastTradedVolume = candleData.LastTradedVolume;
            data1.LastTradedPrice = new IgPublicApiData.ChartHlocModel();
            data1.LastTradedPrice.Close = candleData.LastTradedPrice.Close;
            data1.LastTradedPrice.High = candleData.LastTradedPrice.High;
            data1.LastTradedPrice.Low = candleData.LastTradedPrice.Low;
            data1.LastTradedPrice.Open = candleData.LastTradedPrice.Open;

            data1.Offer = new IgPublicApiData.ChartHlocModel();
            data1.Offer.Close = candleData.Offer.Close;
            data1.Offer.Open = candleData.Offer.Open;
            data1.Offer.High = candleData.Offer.High;
            data1.Offer.Low = candleData.Offer.Low;

            data1.TickCount = candleData.TickCount;
            data1.UpdateTime = candleData.UpdateTime;

            //add to datagrid
            if (ChartMarketHistoryData.Count() >= 10)
                ChartMarketHistoryData.RemoveAt(0);
            ChartMarketHistoryData.Add(data1);
        }

    }
}

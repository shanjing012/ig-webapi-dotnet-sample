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

namespace SampleWPFTrader.ViewModel
{
    public class GraphViewModel : ViewModelBase
    {
        // LS subscriptions...
        private SubscribedTableKey _chartSubscribedTableKey;
        private ChartCandleTableListerner _chartSubscription;

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

        //graph epic
        private string selectedEpic;
        private string searchQuery;

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

        public ObservableCollection<IgPublicApiData.ChartModel> ChartMarketData { get; set; }
        public ObservableCollection<IgPublicApiData.ChartModel> ChartMarketHistoryData { get; set; }
        public ObservableCollection<String> ComboBoxMarkets { get; set; }

        //initialization
        public GraphViewModel()
        {
            InitialiseViewModel();
            ChartMarketData = new ObservableCollection<IgPublicApiData.ChartModel>();
            ChartMarketHistoryData = new ObservableCollection<IgPublicApiData.ChartModel>();
            ComboBoxMarkets = new ObservableCollection<String>();
            
            _chartSubscribedTableKey = new SubscribedTableKey();

            _chartSubscription = new ChartCandleTableListerner();
            _chartSubscription.Update += OnChartCandleDataUpdate;

            WireCommands();
        }


        

        //wire button commands
        private void WireCommands()
        {
            SearchEpicCommand = new RelayCommand(SearchEpic);
            SelectEpicCommand = new RelayCommand(SelectEpic);
            SearchEpicCommand.IsEnabled = true;
            SelectEpicCommand.IsEnabled = false;
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
                if (!candleData.UpdateTime.Equals(candleUpdate.UpdateTime))
                {
                    UpdateChartHistory(candleData);
                    candleData.UpdateTime = candleUpdate.UpdateTime;

                }

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

        //todo
        private void UpdateChartHistory(IgPublicApiData.ChartModel candleData)
        {
            
        }
    }
}

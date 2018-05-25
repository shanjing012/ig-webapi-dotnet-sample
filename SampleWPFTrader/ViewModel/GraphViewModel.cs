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
        public ObservableCollection<IgPublicApiData.ChartModel> ChartMarketData { get; set; }

        // LS subscriptions...
        private MarketDetailsTableListerner _l1GraphPricesSubscription;
        private SubscribedTableKey _graphSubscriptionTableKey;

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
                UnsubscribeFromGraphPrices();
            }
        }

        public void SubscribeToBrowsePrices(string[] epics)
        {
            try
            {
                // Subscribe to L1 price updates for the instruments contained in this browse node...
                _graphSubscriptionTableKey = igStreamApiClient.SubscribeToMarketDetails(epics, _l1GraphPricesSubscription);
                AddStatusMessage("Subscribed Successfully to instruments contained within this browse node.");
            }
            catch (Exception ex)
            {
                AddStatusMessage("Could not subscribe to browse instruments : " + ex.Message);
            }
        }

        public void UnsubscribeFromGraphPrices()
        {
            if ((igStreamApiClient != null) && (_graphSubscriptionTableKey != null) && (LoggedIn))
            {
                igStreamApiClient.UnsubscribeTableKey(_graphSubscriptionTableKey);
                _graphSubscriptionTableKey = null;
                AddStatusMessage("Unsubscribed from Browse Node Prices");
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
        public ObservableCollection<String> ComboBoxMarkets { get; set; }

        public GraphViewModel()
        {
            InitialiseViewModel();
            ComboBoxMarkets = new ObservableCollection<String>();

            // Initialise LS subscriptions            
            _l1GraphPricesSubscription = new MarketDetailsTableListerner();
            //_l1GraphPricesSubscription.Update += OnMarketUpdate;
            // initialise the LS SubscriptionTableKeys          
            _graphSubscriptionTableKey = new SubscribedTableKey();
            _graphSubscriptionTableKey = null;

            WireCommands();
        }

        //some update
        /*private void OnMarketUpdate(object sender, UpdateArgs<L1LsPriceData> e)
        {
            try
            {


                var wlmUpdate = e.UpdateData;

                var epic = e.ItemName.Replace("L1:", "");

                foreach (var browseModel in BrowseMarkets.Where(watchlistItem => watchlistItem.Model.Epic == epic))
                {
                    browseModel.Model.Epic = epic;
                    browseModel.Model.Bid = wlmUpdate.Bid;
                    browseModel.Model.Offer = wlmUpdate.Offer;
                    browseModel.Model.NetChange = wlmUpdate.Change;
                    browseModel.Model.PctChange = wlmUpdate.ChangePct;
                    browseModel.Model.Low = wlmUpdate.Low;
                    browseModel.Model.High = wlmUpdate.High;
                    browseModel.Model.Open = wlmUpdate.MidOpen;
                    browseModel.Model.UpdateTime = wlmUpdate.UpdateTime;
                    browseModel.Model.MarketStatus = wlmUpdate.MarketState;
                }
            }

            catch (Exception ex)
            {
                AddStatusMessage(ex.Message);
            }
        }*/

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
            //SubscribeToBrowsePrices(selectedEpic);
        }


    }
}

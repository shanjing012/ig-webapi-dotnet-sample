using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IGWebApiClient;
using Lightstreamer.DotNet.Client;
using SampleWPFTrader.Model;

namespace SampleWPFTrader.ViewModel
{
    public class GraphViewModel : ViewModelBase
    {
        private string selectedEpic;

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

        
        public ObservableCollection<IgPublicApiData.ChartModel> ChartMarketData { get; set; }

        // LS subscriptions...
        private MarketDetailsTableListerner _l1GraphPricesSubscription;
        private SubscribedTableKey _graphSubscriptionTableKey;

        public GraphViewModel()
        {
            InitialiseViewModel();
            ChartMarketData = new ObservableCollection<IgPublicApiData.ChartModel>();

        }
    }
}

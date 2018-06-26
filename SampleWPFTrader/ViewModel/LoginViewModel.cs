using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using GalaSoft.MvvmLight.Command;
using SampleWPFTrader.Model;


namespace SampleWPFTrader.ViewModel
{
    class LoginViewModel : ViewModelBase
    {
        private string username;
        private string password;
        private string apikey;
        private string env;

        public string Username
        {
            get
            {
                return username;
            }
            set
            {
                username = value;
            }
        }
        public string Password
        {
            get
            {
                return password;
            }
            set
            {
                password = value;
            }
        }
        public string APIKey
        {
            get
            {
                return apikey;
            }
            set
            {
                apikey = value;
            }
        }
        public string Environment
        {
            get
            {
                return env;
            }
            set
            {
                env = value;
            }
        }

        public RelayCommand<Window> LoginCommand
        {
            get;
            private set;
        }

        public LoginViewModel()
        {
            InitialiseViewModel();

            LoginCommand = new RelayCommand<Window>(Login);
        }

        private void Login(Window w)
        {
            SetLoginInfo(username, password, apikey, env);
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            if(w != null)
                w.Close();
        }
    }
}

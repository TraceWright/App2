using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Locations;
//using Location.Droid.Services;
using Android.Content.PM;
using System.Collections.Generic;
using Android.Util;
using System.Linq;
using System.Threading;
using Org.Json;
using RestSharp;

namespace App2
{

    [Activity(Label = "App2", MainLauncher = true, Icon = "@drawable/icon", ScreenOrientation = ScreenOrientation.Portrait)]
    public class MainActivity : Activity, ILocationListener
    {

        private static String TAG = "App2Log";

        TextView txtlatitu, txtlong, txtalt, txtCount;
        TextView errorText;
        TextView clearPhoneData;
        TextView trigger;
        Button startButton;
        Button exportButton;
        Location currentLocation;
        LocationManager locationManager;
        string locationProvider;
        Database database;
        private Timer timer;
        Timer textTimer;
        private int LocNum = 0;

        public static Semaphore Token { get; set; }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            // Set our view from the "main" layout resource  
            SetContentView(Resource.Layout.Main);
            txtlatitu = FindViewById<TextView>(Resource.Id.txtlatitude);
            txtlong = FindViewById<TextView>(Resource.Id.txtlong);
            txtalt = FindViewById<TextView>(Resource.Id.txtalt);
            txtCount = FindViewById<TextView>(Resource.Id.txtCount);
            startButton = FindViewById<Button>(Resource.Id.startButton);
            exportButton = FindViewById<Button>(Resource.Id.exportData);
            errorText = FindViewById<TextView>(Resource.Id.errorText);
            clearPhoneData = FindViewById<Button>(Resource.Id.clearPhoneData);
            trigger = FindViewById<TextView>(Resource.Id.trigger);
            Token = new Semaphore(1, 1);

           
           
           
            database = new Database();
            database.SetRoute();
            //database.refresh();
            
            txtCount.Text = database.getCount().ToString();
            

            startButton.Click += delegate
            {
                StartStopButton();
            };

            exportButton.Click += delegate
            {
                exportDataButton();
            };

            clearPhoneData.Click += delegate
            {
                clearDatabase();
            };


            InitializeLocationManager();
        }

        private void InsertLocation()
        {
            if (currentLocation != null) { 
            database.SetLocation(currentLocation.Latitude, currentLocation.Longitude, currentLocation.Altitude);
            }
        }

        private void InsertSplits()
        {
            database.SetSplits(currentLocation.Latitude, LocNum);
        }

        protected override void OnResume()
        {
            base.OnResume();
            locationManager.RequestLocationUpdates(locationProvider, 0, 0, this);

        }
        protected override void OnPause()
        {
            base.OnPause();
           // locationManager.RemoveUpdates(this);

        }


        private void InitializeLocationManager()
        {
            locationManager = (LocationManager)GetSystemService(LocationService);
            Criteria criteriaForLocationService = new Criteria
            {
                Accuracy = Accuracy.Fine
            };
            IList<string> acceptableLocationProviders = locationManager.GetProviders(criteriaForLocationService, true);
            if (acceptableLocationProviders.Any())
            {
                locationProvider = acceptableLocationProviders.First();
            }
            else
            {
                locationProvider = string.Empty;
            }
            Log.Debug(TAG, "Using " + locationProvider + ".");
        }

        public void OnLocationChanged(Location location)
        {
            currentLocation = location;
            if (currentLocation == null)
            {
                //Error Message  
            }
            else
            {
                txtlatitu.Text = currentLocation.Latitude.ToString();
                txtlong.Text = currentLocation.Longitude.ToString();
                txtalt.Text = currentLocation.Altitude.ToString();
                txtCount.Text = database.getCount().ToString();

                Token.WaitOne();
                Log.Info(TAG, "Location Trigger Before");

                //LocationTrigger();
                LocTrigger();

                Token.Release(1);
            }
        }

        public void LocTrigger()
        {
            IEnumerator<SplitRoutes> splitRoute = database.GetSpecifiedRoute();

            if ((splitRoute.Current != null) && (currentLocation.Latitude <= splitRoute.Current.LatUpper) && (currentLocation.Latitude >= splitRoute.Current.LatLower))
            {
                if (LocNum != splitRoute.Current.LocNumber)
                {
                    trigger.Text = "Location Trigger " + splitRoute.Current.LocNumber + " Activated";
                    LocNum = splitRoute.Current.LocNumber;
                    InsertSplits();
                    Log.Info(TAG, "Location Trigger " + splitRoute.Current.LocNumber + " Activation");
                    database.MoveNextSplit();
                }
            }

        }

        //method for going to any split checkpoint

        //public void LocTrigger()
        //{
        //    IEnumerator<SplitRoutes> splitRoute = database.GetRoute();

        //    while (splitRoute.MoveNext())
        //    {

        //        if ((currentLocation.Latitude >= splitRoute.Current.LatUpper) && (currentLocation.Latitude <= splitRoute.Current.LatLower))
        //        {
        //            if (LocNum != splitRoute.Current.LocNumber)
        //            {
        //                trigger.Text = "Location Trigger " + splitRoute.Current.LocNumber + " Activated";
        //                LocNum = splitRoute.Current.LocNumber;
        //                InsertSplits();
        //                Log.Info(TAG, "Location Trigger " + splitRoute.Current.LocNumber + " Activation");
        //            }
        //        }
        //        else
        //        {
        //            trigger.Text = "";
        //        }
        //    }
        //}

        public void LocationTrigger()
        {

            if ((currentLocation.Latitude <= -27.481500) && (currentLocation.Latitude >= -27.481700))
            {
                if (LocNum != 1)
                {
                    trigger.Text = "Location Trigger 1 Activated";
                    LocNum = 1;
                    InsertSplits();
                    Log.Info (TAG, "Location Trigger 1 Activation");
                   
                }
            }
            else
            {
                trigger.Text = "";
            }

            if ((currentLocation.Latitude <= -27.481900) && (currentLocation.Latitude >= -27.482100))
            {
                if (LocNum != 2)
                {
                    trigger.Text = "Location Trigger 2 Activated";
                    LocNum = 2;
                    InsertSplits();
                    Log.Info(TAG, "Location Trigger 2 Activation");
                }
            }
            else
            {
                trigger.Text = "";
            }
        }

        public void OnProviderDisabled(string provider)
        {
            //throw new NotImplementedException();
        }

        public void OnProviderEnabled(string provider)
        {
            //throw new NotImplementedException();
        }

        public void OnStatusChanged(string provider, [GeneratedEnum] Availability status, Bundle extras)
        {
            //throw new NotImplementedException();
        }

        public void StartStopButton()
        {
            if (timer == null) {
                database.incrementFlag();
                timer = new Timer(x => InsertLocation(), null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
                startButton.Text = "Stop";
            }

            else { 
                timer.Dispose();
                timer = null;
                startButton.Text = "Start";
            }   
        }

        public void exportDataButton()
        {
            errorText.Text = "";
            var client = new WebsiteRestClient().GetWebsiteData();
            //var client = new WebsiteRestClient().GetTestData();

            var request = new RestSharp.RestRequest();

            request.Method = Method.POST;
            request.AddHeader("Accept", "application/json");
            request.Parameters.Clear();
            request.RequestFormat = DataFormat.Json;

            JSONObject dataStore = database.formatData();
            JSONArray spdataStore = database.formatSplitData();
            JSONObject jobj = new JSONObject();
            jobj.Put("data", dataStore);
            jobj.Put("spdata", spdataStore);
            String data = jobj.ToString();

            request.AddParameter("application/json", data, ParameterType.RequestBody);

            var response = client.Execute(request);
            var content = response.Content;

            if (content == "data_received")
            {
                database.clearData();
                errorText.Text = "Data transferred";
            }
            else
            {
                errorText.Text = "Data not transferred";          
            }

            //textTimer = new Timer(x => ClearMessage(), null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
        }

        public void clearDatabase()
        {
            database.clearData();
        }

        public void ClearMessage()
        {
           // TextView errText = FindViewById<TextView>(Resource.Id.errorText);

            //errText.Text = "";
            //textTimer.Dispose();
        }
    }
}



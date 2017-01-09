using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Data;
using SQLite;
using Org.Json;
using System.Threading;

namespace App2
{
    public class Locations
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public double Lat { get; set; }
        public double Lng { get; set; }
        public double Alt { get; set; }
        public DateTime Timestamp { get; set; }
        public int SeparatorFlag { get; set; }
    }

    public class SepStorage
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int SeparatorFlag { get; set; }
    }

    public class Splits
    {
        [PrimaryKey, AutoIncrement]
        
        public int SplitId { get; set; }
        public int RouteNum { get; set; }
        public double SplitLat { get; set; }
        public double SplitLng { get; set; }
        public int SplitNum { get; set; }
        public DateTime SplitTimestamp { get; set; }
        public int SeparatorFlag { get; set; }
    }

    public class SplitRoutes
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int RNum { get; set; }
        public int LocNumber { get; set; }
        public double SLat { get; set; }
        public double SLng { get; set; }
       // public double LngUpper { get; set; }
       // public double LngLower { get; set; }

    }

    public class Database {

       

        string folder, locationsdb;
        string SplitRouteDb;
        SQLiteConnection SplitRouteDbConnection, LocationsDatabase;
        int sepFlag;
        IEnumerator<SplitRoutes> CurrentSplit;
        
        public  JSONObject formatData()
        {
            JSONObject jobj;
            JSONObject arr = new JSONObject();
            String dataIndex;
           
            var returndata = LocationsDatabase.DeferredQuery<Locations>("select * from locations").GetEnumerator();
            
            while (returndata.MoveNext())
            {
                TimeSpan time = returndata.Current.Timestamp - new DateTime(1970, 1, 1);
                jobj = new JSONObject();
                jobj.Put("lat", returndata.Current.Lat);
                jobj.Put("lng", returndata.Current.Lng);
                jobj.Put("alt", returndata.Current.Alt);
                jobj.Put("timestamp", (int)time.TotalSeconds);
                dataIndex = returndata.Current.SeparatorFlag.ToString();
                if (!arr.Has(dataIndex))
                {
                    arr.Put(dataIndex, new JSONArray());
                }
                arr.GetJSONArray(dataIndex).Put(jobj);
            }
            return arr;
        }

        public JSONArray formatSplitData()
        {
            JSONObject jobj;
            JSONArray sparr = new JSONArray();
           
            var returndata = LocationsDatabase.DeferredQuery<Splits>("select * from splits").GetEnumerator();

            while (returndata.MoveNext())
            {
                TimeSpan time = returndata.Current.SplitTimestamp - new DateTime(1970, 1, 1);
                jobj = new JSONObject();
                jobj.Put("rnum", returndata.Current.RouteNum);
                jobj.Put("slat", returndata.Current.SplitLat);
                jobj.Put("slng", returndata.Current.SplitLng);
                jobj.Put("stimestamp", (int)time.TotalSeconds);
                jobj.Put("snum", returndata.Current.SplitNum);
                jobj.Put("sepflag", returndata.Current.SeparatorFlag );
                sparr.Put(jobj);
            }
            return sparr;
        }

        public Database()
        {

            folder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
            locationsdb = "locations.db";
            LocationsDatabase = new SQLiteConnection(System.IO.Path.Combine(folder, locationsdb));

            LocationsDatabase.CreateTable<Locations>();
            LocationsDatabase.CreateTable<SepStorage>();
            LocationsDatabase.CreateTable<Splits>();
               
            //var maxColumn = db.DeferredQuery<Locations>("select max(SeparatorFlag) as SeparatorFlag from Locations").GetEnumerator();
            var maxColumn = LocationsDatabase.DeferredQuery<SepStorage>("select max(SeparatorFlag) as SeparatorFlag from SepStorage").GetEnumerator();
            sepFlag = 0;

            while (maxColumn.MoveNext())
            {
                sepFlag = maxColumn.Current.SeparatorFlag;
            }

           
            SplitRouteDb = "Splits.db";
            SplitRouteDbConnection = new SQLiteConnection(System.IO.Path.Combine(folder, SplitRouteDb));
            SplitRouteDbConnection.CreateTable<SplitRoutes>();
        }

        //public void refresh()
        //{
        //    db.DropTable<Locations>();
        //    db.CreateTable<Locations>();

        //    db.DropTable<SepStorage>();
        //    db.CreateTable<SepStorage>();
        //}


        //public void GetRouteOne()
        //{
        //    SplitRouteDbConnection.DeferredQuery<SplitRoutes>().GetEnumerator();
        //}


        public void SetRoute()
        {
            if (TableExists<SplitRoutes>(SplitRouteDbConnection) == true)
                SplitRouteDbConnection.DropTable<SplitRoutes>();
                SplitRouteDbConnection.CreateTable<SplitRoutes>();

            insertRouteCoordinate(2, 1, -27.481600, 153.007801);
            insertRouteCoordinate(2, 2, -27.482000, 153.007730);

            //insertRouteCoordinate(1, 1, -27.483520, 153.011580);
            //insertRouteCoordinate(1, 2, -27.489640, 153.010350);
            //insertRouteCoordinate(1, 3, -27.489780, 153.003300);
            //insertRouteCoordinate(1, 4, -27.487320, 152.997090);
            //insertRouteCoordinate(1, 5, -27.481300, 153.001000);
            //insertRouteCoordinate(1, 6, -27.476880, 153.004810);
            //insertRouteCoordinate(1, 7, -27.471100, 153.012500);
            //insertRouteCoordinate(1, 8, -27.472920, 153.019940);
            //insertRouteCoordinate(1, 9, -27.479980, 153.024700);
            //insertRouteCoordinate(1, 10, -27.482220, 153.020700);
            //insertRouteCoordinate(1, 11, -27.480980, 153.012110);

        }

        //sets specified routes with route numbers, split numbers and coordinates to phone database

        private void insertRouteCoordinate(int route, int loc, double lat, double lng)
        {
            SplitRoutes splitRoute = new SplitRoutes {RNum = route, LocNumber = loc, SLat = lat, SLng = lng };
            SplitRouteDbConnection.Insert(splitRoute);
        }

        public bool MoveNextSplit()
        {
            return CurrentSplit.MoveNext();
        }

        public IEnumerator<SplitRoutes> GetSpecifiedRoute(int routeNumber)
        {
            if (CurrentSplit == null)
            {
                CurrentSplit = SplitRouteDbConnection.DeferredQuery<SplitRoutes>("select * from SplitRoutes where RNum = " + routeNumber).GetEnumerator();
                CurrentSplit.MoveNext();
            }

            return CurrentSplit;
        }

        public IEnumerator<SplitRoutes> GetRoute(int routeNumber)
        {
            return SplitRouteDbConnection.DeferredQuery<SplitRoutes>("select * from splitroutes where RNum = " + routeNumber).GetEnumerator();
            //TODO Parameterised Query to prevent SQL injection
        }

        //sets split data while app is in use
        public void SetSplits(int routeNumber, double latitude, double longitude, int LocNum)
        {
            var split = new Splits { RouteNum = routeNumber, SplitLat = latitude, SplitLng = longitude, SplitNum = LocNum, SplitTimestamp = DateTime.UtcNow, SeparatorFlag = sepFlag };
            LocationsDatabase.Insert(split);
        }

        public void SetLocation(double latitude, double longitude, double altitude)
        {
            var location = new Locations { Lat = latitude, Lng = longitude, Alt = altitude, Timestamp = DateTime.UtcNow, SeparatorFlag = sepFlag };
            LocationsDatabase.Insert(location);
        }

        public int getCount()
        {
            int count = 0;
            count = LocationsDatabase.Table<Locations>().Count();
            return count;
        }

        public void incrementFlag()
        {
            sepFlag++;
            LocationsDatabase.Insert(new SepStorage { SeparatorFlag = sepFlag });
        }

        public void clearData()
        {
            
            LocationsDatabase.DropTable<Locations>();
            //}

            LocationsDatabase.CreateTable<Locations>();

            LocationsDatabase.DropTable<Splits>();
            LocationsDatabase.CreateTable<Splits>();

            LocationsDatabase.DropTable<SepStorage>();
            LocationsDatabase.CreateTable<SepStorage>();
            LocationsDatabase.Insert(new SepStorage { SeparatorFlag = sepFlag });


            
        }

        public static bool TableExists<T>(SQLiteConnection connection)
        {
            const string cmdText = "SELECT name FROM sqlite_master WHERE type='table' AND name=?";
            var cmd = connection.CreateCommand(cmdText, typeof(T).Name);
            return cmd.ExecuteScalar<string>() != null;
        }

    }
}
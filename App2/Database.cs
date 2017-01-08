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
        public double SplitLat { get; set; }
        public int SplitNum { get; set; }
        public DateTime SplitTimestamp { get; set; }
    }

    public class SplitRoutes
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
       // public int RouteId { get; set; }
        public int LocNumber { get; set; }
        public double LatUpper { get; set; }
        public double LatLower { get; set; }
       // public double LngUpper { get; set; }
       // public double LngLower { get; set; }

    }

    public class Database {

       // public static Semaphore Token { get; set; }

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
                jobj.Put("slat", returndata.Current.SplitLat);
                //jobj.Put("lng", returndata.Current.Lng);
                jobj.Put("stimestamp", (int)time.TotalSeconds);
                jobj.Put("snum", returndata.Current.SplitNum);
                sparr.Put(jobj);
            }
            return sparr;
        }

        public Database()
        {
            //Token = new Semaphore(1, 1);

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


        public void SetRoute()
        {
            if (TableExists<SplitRoutes>(SplitRouteDbConnection) == true)
                SplitRouteDbConnection.DropTable<SplitRoutes>();
            SplitRouteDbConnection.CreateTable<SplitRoutes>();

            SplitRoutes splitRoute;

            splitRoute = new SplitRoutes { LocNumber = 1, LatLower = -27.481700, LatUpper = -27.481500 };
            SplitRouteDbConnection.Insert(splitRoute);
            splitRoute = new SplitRoutes { LocNumber = 2, LatLower = -27.482100, LatUpper = -27.481900 };
            SplitRouteDbConnection.Insert(splitRoute);
            
        }

        public bool MoveNextSplit()
        {
            return CurrentSplit.MoveNext();
        }

        public IEnumerator<SplitRoutes> GetSpecifiedRoute()
        {
            if (CurrentSplit == null)
            {
                CurrentSplit = SplitRouteDbConnection.DeferredQuery<SplitRoutes>("select * from SplitRoutes").GetEnumerator();
                CurrentSplit.MoveNext();
            }

            return CurrentSplit;
        }

        public IEnumerator<SplitRoutes> GetRoute()
        {
            return SplitRouteDbConnection.DeferredQuery<SplitRoutes>("select * from splitroutes").GetEnumerator();
        }

        public void SetSplits(double latitude, int LocNum)
        {
            var split = new Splits { SplitLat = latitude, SplitNum = LocNum, SplitTimestamp = DateTime.UtcNow };
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
            //Token.WaitOne();
            //using (SQLiteConnection db = new SQLiteConnection(System.IO.Path.Combine(folder, locationsdb)))
            //{

            //if (TableExists<Locations>(db) == true)
            //{ 
            LocationsDatabase.DropTable<Locations>();
            //}

            LocationsDatabase.CreateTable<Locations>();

            LocationsDatabase.DropTable<Splits>();
            LocationsDatabase.CreateTable<Splits>();

            LocationsDatabase.DropTable<SepStorage>();
            LocationsDatabase.CreateTable<SepStorage>();
            LocationsDatabase.Insert(new SepStorage { SeparatorFlag = sepFlag });


            //{
            //Token.Release();
        }

        public static bool TableExists<T>(SQLiteConnection connection)
        {
            const string cmdText = "SELECT name FROM sqlite_master WHERE type='table' AND name=?";
            var cmd = connection.CreateCommand(cmdText, typeof(T).Name);
            return cmd.ExecuteScalar<string>() != null;
        }

    }
}
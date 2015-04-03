using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI;
using VRage.Voxels;
using VRageMath;

namespace Communications//THIS STILL CRASHES DO NOT USE
{
    public static class AsteroidManager//class asteroid manager, static
    {
        public class OreCoord// class ore coord, was just a struct but realized we need to change it later
        {
            public string VMaterial;// # representing material
            public Vector3 OrePos;//Vector representing global position of voxel
        }

        public class Asteroid// Class defining our asteroid information
        {
            public String AssName;//name of storage bin of asteroid
            public List<OreCoord> Ores;//list of Orecoords generated when we scanned the asteroid cache
            public DateTime Timer;//Timer, when this is 10 min less than actual time asteroid is removed from list
        }
        private static DateTime _mostRecentTime = DateTime.Now;//Time
        public static List<OreCoord> OreArray = new List<OreCoord>();  //List of OreCoords found in Asteroid
        public static List<Asteroid> StorageList = new List<Asteroid>();//A list of all the asteroids

        public static void Update100()//Update function, allows us to do aging of asteroids and remove after 10 min
        {
            _mostRecentTime = DateTime.Now;// get the time
            foreach (var asteroid in StorageList.Where(asteroid => _mostRecentTime.Minute-asteroid.Timer.Minute > 10))//if time in minutes now is greater than 10 min from when the asteroid was created
            {
                StorageList.Remove(asteroid);//remove the asteroid from the list
            }
        }

        public static void UpdateOres(IMyVoxelMap asteroid)//check if asteroid is in registry
        {
            var asteroidinit = StorageList.Any(asteroid1 => asteroid1.AssName == asteroid.StorageName);//if asteroid is not in registry
            if (asteroidinit == false)
            {
                GenerateMapAsteroid(asteroid);//put it in registry
            }
        }

        public static List<OreCoord> GetGrid(IMyVoxelMap asteroid)//return list of ore scans in the asteroid
        {
            UpdateOres(asteroid);//make sure asteroid is in registry
            var myAsteroid = StorageList.Find(asteroid1 => asteroid1.AssName == asteroid.StorageName);//find asteroid in registry
            return myAsteroid.Ores;//return list

        }

        public static void GenerateMapAsteroid(IMyVoxelMap asteroid1)//add asteroid to registry
        {
            OreArray.Clear();//empty the ore array
            var testStorage = asteroid1.Storage;//get the storage file

            var cache = new MyStorageDataCache();//create new data cache

            cache.Resize(testStorage.Size);
            testStorage.ReadRange(cache, MyStorageDataTypeFlags.All, 4, new Vector3I(0, 0, 0), testStorage.Size);//set accuracy to 4 in the search

            for (var x = 0; x <= cache.Size3D.X; x += 16)//for every 16 x
            {

                for (var y = 0; y <= cache.Size3D.Y; y += 16)//for every 16 y
                {
                    for (var z = 0; z <= cache.Size3D.Z; z += 16)//for every 16 z
                    {

                        var voxelpos = new Vector3I(x, y, z);//create a voxel
                        var material = cache.Material(ref voxelpos).ToString();//get the material at this voxel point
                        var worldpos = ((Vector3D)voxelpos) + asteroid1.PositionLeftBottomCorner;//convert to metric world position relative to (0,0)(Bottom Left Corner)
                        var ore = new OreCoord {VMaterial = material, OrePos = worldpos};//create new Ore point containing material # and global position
                        OreArray.Add(ore);//add to the ore list for this asteroid
                    }
                }
            }//when we have generated a rough grid of asteroid points
            //create new asteroid containing the storage name, the Ore array, and a timer set to the moment we create the asteroid
            var asteroid = new Asteroid {AssName = asteroid1.StorageName, Ores = OreArray,Timer = DateTime.Now};

            StorageList.Add(asteroid);//add asteroid to the storage list
        }
    }
}
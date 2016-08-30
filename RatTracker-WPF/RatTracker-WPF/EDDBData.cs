﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json;
using RatTracker_WPF.Models.Edsm;
using RatTracker_WPF.Models.Eddb;
using log4net;
using System.IO;

namespace RatTracker_WPF
{
	public class EddbData
	{
		private const string EddbUrl = "http://eddb.io/archive/v4/";
		private static readonly ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public IEnumerable<EddbStation> stations;
		public IEnumerable<EddbSystem> systems;

		public async Task<string> UpdateEddbData()
		{
			string rtPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            if (Thread.CurrentThread.Name == null)
            {
                Thread.CurrentThread.Name = "EDDBWorker";
            }

            DateTime filedate = File.Exists(rtPath + @"\RatTracker\stations.json") ? File.GetLastWriteTime(rtPath + @"\RatTracker\stations.json") : new DateTime(1985,4,1);
            if (filedate.AddDays(7) < DateTime.Now)
            {
                logger.Info("EDDB cache is older than 7 days, updating...");
                try
                {
                    using (
                        HttpClient client =
                            new HttpClient(new HttpClientHandler
                            {
                                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
                            }))
                    {
                        UriBuilder content = new UriBuilder(EddbUrl + "stations.json") { Port = -1 };
                        logger.Info("Downloading " + content);
                        HttpResponseMessage response = await client.GetAsync(content.ToString());
                        response.EnsureSuccessStatusCode();
                        string responseString = await response.Content.ReadAsStringAsync();
                        //AppendStatus("Got response: " + responseString);
                        using (StreamWriter sw = new StreamWriter(rtPath + @"\RatTracker\stations.json"))
                        {
                            await sw.WriteLineAsync(responseString);
                            logger.Info("Saved stations.json");
                        }
                        stations = JsonConvert.DeserializeObject<IEnumerable<EddbStation>>(responseString);
                        logger.Debug("Deserialized stations: " + stations.Count());
                    }
                    
                    using (
                        HttpClient client =
                            new HttpClient(new HttpClientHandler
                            {
                                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
                            }))
                    {
                        UriBuilder content = new UriBuilder(EddbUrl + "systems.json") { Port = -1 };
                        logger.Debug("Downloading " + content);
                        HttpResponseMessage response = await client.GetAsync(content.ToString());
                        response.EnsureSuccessStatusCode();
                        string responseString = await response.Content.ReadAsStringAsync();
                        //AppendStatus("Got response: " + responseString);
                        using (StreamWriter sw = new StreamWriter(rtPath + @"\RatTracker\systems.json"))
                        {
                            await sw.WriteLineAsync(responseString);
                            logger.Info("Saved systems.json");
                        }

                        systems = JsonConvert.DeserializeObject<IEnumerable<EddbSystem>>(responseString);
                        logger.Info("Deserialized systems: " + systems.Count());
                        return "EDDB data downloaded. " + systems.Count() + " systems and " + stations.Count() + " stations added.";
                    }
                }
                catch (Exception ex)
                {
                    logger.Fatal("Exception in UpdateEDDBData: ", ex);
                    return "EDDB data download failed!";
                }
            }
            else
            {
				try {
					string loadedfile;
					using (StreamReader sr = new StreamReader(rtPath + @"\RatTracker\stations.json"))
					{
						loadedfile = sr.ReadLine();
					}
					stations = JsonConvert.DeserializeObject<IEnumerable<EddbStation>>(loadedfile);
					using (StreamReader sr = new StreamReader(rtPath + @"\RatTracker\systems.json"))
					{
						loadedfile = sr.ReadLine();
					}
					systems = JsonConvert.DeserializeObject<IEnumerable<EddbSystem>>(loadedfile);
					return "Loaded cached EDDB data. " + systems.Count() + " systems and " + stations.Count() + " stations added.";
				}
				catch(Exception ex)
				{
					logger.Debug("Exception during load EDDB cached data: " + ex.Message);
					return "Failed to load EDDB cache!";
				}
            }
		}

		public EddbSystem GetSystemById(int id)
		{
			return id < 1 ? new EddbSystem() : systems.FirstOrDefault(sys => sys.id == id);
		}

		/*
		public EDDBSystem GetNearestSystem(string systemname)
		{
			try
			{
				logger.Debug("Searching for system " + systemname);
				var nearestsystem = systems.Where(mysystem => mysystem.name == systemname).Select(
					system => new
					{
						system.id,
						system.population,
						system.name
					}).OrderBy(mysys => mysys.name).First().id;
				return nearestsystem;
			}
			catch (Exception ex)
			{
				logger.Debug("FAIL!");
				return new EDDBSystem();
			}
		}
		*/
		public EddbStation GetClosestStation(EdsmCoords coords)
		{
			try {
				logger.Debug("Calculating closest station to X:" + coords.X + " Y:" + coords.Y + " Z:" + coords.Z);
				var closestSystemId = systems.Where(system => system.population > 0).Select(
					system =>
						new
						{
							system.id,system.population,
							distance =
								Math.Sqrt(Math.Pow(coords.X - system.x, 2) + Math.Pow(coords.Y - system.y, 2) + Math.Pow(coords.Z - system.z, 2))
						}).OrderBy(x => x.distance).First().id;
				logger.Debug("Got system " + closestSystemId);
				EddbStation station =
					stations.Where(st => st.system_id == closestSystemId && st.distance_to_star != null)
						.OrderBy(st => st.distance_to_star)
						.FirstOrDefault();
				logger.Debug("Got station " + station.name);
				return station;
			}
			catch (Exception ex)
			{
				logger.Fatal("Exception in GetClosestStation: " + ex.Message);
				return new EddbStation();
			}
		}

	}
}
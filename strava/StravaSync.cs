using Google.Apis.Auth.OAuth2;
using Google.Apis.Fitness.v1;
using Google.Apis.Fitness.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using NUnit.Framework;
using System;
using System.Configuration;
using System.Collections.Specialized;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WeightSync
{
	public class StravaSync
	{
		public static double weight = 0;

		[STAThread]
		[Test]
		public static void Main()
		{
			try
			{
				new StravaSync().Run().Wait();
			}
			catch (AggregateException ex)
			{
				foreach (var e in ex.InnerExceptions)
				{
					Console.WriteLine("ERROR: " + e.Message);
					Assert.Fail("ERROR: " + e.Message);
				}
			}

			if (weight > 0)
			{
				try
				{
					SyncToStrava(weight.ToString());
					Assert.Pass($"Synced weight to Strava - {weight}");
				}
				catch (AggregateException ex)
				{
					foreach (var e in ex.InnerExceptions)
					{
						Console.WriteLine("ERROR: " + e.Message);
						Assert.Fail("ERROR: " + e.Message);
					}
				}
			}
			else
			{
				Assert.Fail("Вес не был синхронизирован, его значение равно или меньше нуля!");
			}
		}
		private async Task Run()
		{
			UserCredential credential;

			credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
				new ClientSecrets
				{
					ClientId = ConfigurationManager.AppSettings["ClientId"],
					ClientSecret = ConfigurationManager.AppSettings["ClientSecret"]
				},
				new[] { FitnessService.Scope.FitnessBodyRead },
				"user",
				CancellationToken.None,
				new FileDataStore("Fitness"));

			// Create the service.
			var service = new FitnessService(new BaseClientService.Initializer()
			{
				HttpClientInitializer = credential,
				ApplicationName = "Google Fit to Strava weight Sync",
			});

			var start = UnixTime(int.Parse(ConfigurationManager.AppSettings["StartMonth"]));
			var end = UnixTime(int.Parse(ConfigurationManager.AppSettings["EndMonth"]));

			string mainUri = ConfigurationManager.AppSettings["mainUri"];
			string uri = $"{mainUri}{start}-{end}";
			var responseMessage = await service.HttpClient.GetAsync(uri);

			if (responseMessage.IsSuccessStatusCode)
			{
				var ds = responseMessage.Content.ReadAsAsync<Dataset>().Result;
				weight = ds.Point[ds.Point.Count-1].Value[0].FpVal.Value;
			}
		}
		public long UnixTime(int addMonths)
		{
			DateTime epochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			return (DateTime.UtcNow.AddMonths(addMonths) - epochStart).Ticks * 100;
		}
		private static void SyncToStrava(string weight)
		{
			string url = ConfigurationManager.AppSettings["MakerURL"];
			string successMsg = "Congratulations! You've fired the new_weight event";

			using (var wb = new WebClient())
			{

				var data = new NameValueCollection
				{
					["value1"] = weight
				};

				var response = wb.UploadValues(url, "POST", data);
				string responseInString = Encoding.UTF8.GetString(response);
				Assert.AreEqual(responseInString, successMsg);
			}
		}
	}
}
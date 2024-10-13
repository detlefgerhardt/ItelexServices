using HtmlAgilityPack;
using ItelexCommon;
using ItelexCommon.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ItelexWeatherServer
{
	public class DwdServer
	{
		private const int DWD_LINE_LEN = 69;

		private const string DWD_URL_FORECASTS = "https://opendata.dwd.de/weather/text_forecasts/txt/";
		private const string DWD_URL_ALERTS = "https://opendata.dwd.de/weather/alerts/txt/";
		private const string DWD_URL_MARITIME_FORECASTS_DE = "https://opendata.dwd.de/weather/maritime/forecast/german/";
		private const string DWD_URL_MARITIME_FORECASTS_EN = "https://opendata.dwd.de/weather/maritime/forecast/english/";
		private const string DWD_URL_MARITIME_ALERTS = "https://opendata.dwd.de/weather/maritime/forecast/german/";

		public enum DwdTypeEnum
		{
			Vorhersage,
			Warnlage,
			Strassenlage,
			Alpen,
			WarnWoche,
			SynoptKurz,
			SynoptMittel,
			SeewetterNord_DE,
			SeewetterSued_DE,
			SeewarnNord_DE,
			SeewarnSued_DE,
			SeewetterNord_EN,
			SeewarnNord_EN,
		};

		public enum DwdRegionEnum
		{
			Deut = 1,
			SchlesHam = 3,
			NordrWestf = 4,
			NiedersBrem = 5,
			MecklVorp = 6,
			BrandbBerl = 7,
			Hess = 8,
			Thuer = 9,
			Sachs = 10,
			SachsAnh = 11,
			RheinlPfSaarl = 12,
			NordwStutt = 13,
			BadenW = 14,
			RheintSchw = 15,
			SchwAlbBod = 16,
			Bay = 17,
			NordBay = 18,
			SuedBay = 19,
			Default = 90,
			Synopt = 91,
			Marit = 92,
			Alpen = 93
		};

		private List<DwdRegion> DWD_REGIONS = new List<DwdRegion>()
		{
			new DwdRegion(DwdRegionEnum.Deut, "DWOG", "Deutschland", "deutschland"),
			new DwdRegion(DwdRegionEnum.SchlesHam, "DWHH", "Schleswig-Holstein/Hamburg", "schleswig-holstein/hamburg"),
			new DwdRegion(DwdRegionEnum.NordrWestf, "DWEH", "Nordrhein-Westfahlen", "nordrhein-westfahlen"),
			new DwdRegion(DwdRegionEnum.NiedersBrem, "DWHG", "Niedersachen/Bremen", "niedersachen/bremen"),
			new DwdRegion(DwdRegionEnum.MecklVorp, "DWPH", "Mecklenburg-Vorpommern", "mecklenburg-vorpommern"),
			new DwdRegion(DwdRegionEnum.BrandbBerl, "DWPG", "Brandenburg/Berlin", "brandenburg/berlin"),
			new DwdRegion(DwdRegionEnum.Hess, "DWOH", "Hessen", "hessen"),
			new DwdRegion(DwdRegionEnum.Thuer, "DWLI", "Thüringen", "thueringen"),
			new DwdRegion(DwdRegionEnum.Sachs, "DWLG", "Sachsen", "sachsen"),
			new DwdRegion(DwdRegionEnum.SachsAnh, "DWLH", "Sachsen-Anhalt", "sachsen-anhalt"),
			new DwdRegion(DwdRegionEnum.RheinlPfSaarl, "DWOI", "Rheinland-Pfalz/Saarland", "rheinland-pfalz/saarland"),
			new DwdRegion(DwdRegionEnum.NordwStutt, "DWSN", "Nordwürttemberg/Stuttgart", "nordwuerttemberg/stuttgart"),
			new DwdRegion(DwdRegionEnum.BadenW, "DWSG", "Baden-Württemberg", "baden-wuerttemberg"),
			new DwdRegion(DwdRegionEnum.RheintSchw, "DWSO", "Rheintal/Schwarzwald", "rheintal/schwarzwald"),
			new DwdRegion(DwdRegionEnum.SchwAlbBod, "DWSP", "Schwäbische Alb/Oberschwaben/Bodensee",
				"schwaeb.alb/oberschw./bodensee"),
			new DwdRegion(DwdRegionEnum.Bay, "DWMG", "Bayern", "bayern"),
			new DwdRegion(DwdRegionEnum.NordBay, "DWMO", "Nordbayern", "nordbayern"),
			new DwdRegion(DwdRegionEnum.SuedBay, "DWMP", "Südbayern", "suedbayern"),

			//new DwdRegion(80, "DWOG", "Warnmeldungen", ""),
			new DwdRegion(DwdRegionEnum.Default, "DWHA", "Default", ""),
			new DwdRegion(DwdRegionEnum.Marit, "EDZW", "Default Maritime", ""),

			new DwdRegion(DwdRegionEnum.Synopt, "DWAV", "Synopt", "synopt"),
			new DwdRegion(DwdRegionEnum.Alpen, "DWMS", "Alpen", "alpen"),
		};

		private List<DwdType> DWD_TYPES = new List<DwdType>()
		{
			new DwdType(1, DwdTypeEnum.Vorhersage, "VHDL13", "Wettervorhersage", "wetter", DWD_URL_FORECASTS),
			new DwdType(2, DwdTypeEnum.Warnlage, "VHDL30", "Warnlage", "warnlage", DWD_URL_ALERTS),
			//new DwdType(3, DwdTypeEnum.Strassenlage, "VHDL35", "Strassenvorhersage", "strassenlage", DWD_URL_FORECASTS),
			new DwdType(3, DwdTypeEnum.WarnWoche, "VHDL35", "Warnungen 10-Tage", "warn 10-tage", DWD_URL_ALERTS),

			new DwdType(4, DwdTypeEnum.SeewetterNord_DE, "FQEN51", "Seewetterbericht Nord- und Ostseeküste", "seewetter nord/ostsee",
				DWD_URL_MARITIME_FORECASTS_DE),
			new DwdType(5, DwdTypeEnum.SeewetterSued_DE, "FQMM60", "Seewetterbericht Mittelmeer", "seewetter mittelmeer",
				DWD_URL_MARITIME_FORECASTS_DE),
			new DwdType(6, DwdTypeEnum.SeewarnSued_DE, "FXDL40", "Mittelfrist-Seewetterbericht Nord- und Ostseeküste",
				"mittelfristwetter nord/ostsee", DWD_URL_MARITIME_FORECASTS_DE),
			new DwdType(7, DwdTypeEnum.SeewarnNord_DE, "FQEN50", "Seewetter-Warnungen Nord- und Ostseeküste",
				"wetterwarnungen nord/ostsee", DWD_URL_MARITIME_FORECASTS_DE),
			new DwdType(8, DwdTypeEnum.SeewetterNord_EN, "FQEN71", "Weatherreport for German Coast",
				"german coast", DWD_URL_MARITIME_FORECASTS_EN),
			new DwdType(9, DwdTypeEnum.SeewarnNord_EN, "FQEN70", "Weather and Sea Bulletin for Noth- and Baltic Sea",
				"bulletin german coast", DWD_URL_MARITIME_FORECASTS_EN),

			new DwdType(10, DwdTypeEnum.SynoptKurz, "SXDL31", "Synoptische Wettervorhersage kurzfristig",
				"synopt kurzfrist", DWD_URL_FORECASTS),
			new DwdType(11, DwdTypeEnum.SynoptMittel, "SXDL33", "Synoptische Wettervorhersage mittelfristig",
				"synopt mittelfrist", DWD_URL_FORECASTS),

			new DwdType(12, DwdTypeEnum.Alpen, "FWDL39", "Deutscher Alpenraum und Lawinenwarnzentrale",
				"alpen", DWD_URL_FORECASTS)

		};

		private List<FileItem> forecastCache = null;
		private TickTimer forecastCacheAge = new TickTimer(false);

		private List<FileItem> alertsCache = null;
		private TickTimer alertsCacheAge = new TickTimer(false);

		private List<FileItem> maritimeCache = null;
		private TickTimer maritimeCacheAge = new TickTimer(false);

		/// <summary>
		/// singleton pattern
		/// </summary>
		private static DwdServer instance;

		public static DwdServer Instance => instance ?? (instance = new DwdServer());

		private DwdServer()
		{
			GetForecastFiles();
			GetAlertsFiles();
			GetMaritimeFiles();
			Test();
		}

		public void Test()
		{
			//List<FileItem> list = GetFiles(DWD_URL_FORECASTS);
			//string weatherText = GetWeather(DwdTypeEnum.Vorhersage, 8);
			//string weatherText = GetWeather(DwdTypeEnum.SeewetterNord_DE, DwdRegionEnum.Marit);
			//string weatherText1 = GetWeather(DwdTypeEnum.SynoptKurz, DwdRegionEnum.Synopt);
			//string weatherText2 = GetWeather(DwdTypeEnum.SynoptMittel, DwdRegionEnum.Synopt);
			//string weatherText3 = GetWeather(DwdTypeEnum.Alpen, DwdRegionEnum.Alpen);
			string weatherText = GetWeather(DwdTypeEnum.Alpen, DwdRegionEnum.Alpen);

			//FormatText(weatherText, 59);

		}

		public string GetWeather(DwdTypeEnum type, DwdServer.DwdRegionEnum region)
		{
			var dwdType = (from t in DWD_TYPES where t.Type == type select t).FirstOrDefault();
			var dwdRegion = (from r in DWD_REGIONS where r.Region == region select r).FirstOrDefault();

			var files = GetFilesByType(type);
			if (files == null) return null;

			var weatherFiles = (from f in files
								where f.Field1 == dwdType.Field && f.Field2 == dwdRegion.Field
								select f).OrderByDescending(f => f.Index).ToList();
			if (weatherFiles.Count == 0) return null;

			var weatherFile = weatherFiles.First();
			if (weatherFile == null) return null;

			string htmlSource;
			try
			{
				WebClient wClient = new WebClient();
				htmlSource = wClient.DownloadString(weatherFile.Url + weatherFile.RawName);
			}
			catch (WebException)
			{
				htmlSource = null;
			}
			catch (Exception)
			{
				return null;
			}

			//return FormatText(htmlSource, lineLen);
			return htmlSource;
		}

		private List<FileItem> GetFilesByType(DwdTypeEnum type)
		{
			switch (type)
			{
				case DwdTypeEnum.Vorhersage:
				case DwdTypeEnum.Strassenlage:
				case DwdTypeEnum.SynoptKurz:
				case DwdTypeEnum.SynoptMittel:
				case DwdTypeEnum.Alpen:
					return GetForecastFiles();

				case DwdTypeEnum.Warnlage:
				case DwdTypeEnum.WarnWoche:
					return GetAlertsFiles();

				case DwdTypeEnum.SeewetterNord_DE:
				case DwdTypeEnum.SeewetterSued_DE:
				case DwdTypeEnum.SeewarnNord_DE:
				case DwdTypeEnum.SeewarnSued_DE:
				case DwdTypeEnum.SeewetterNord_EN:
				case DwdTypeEnum.SeewarnNord_EN:
					return GetMaritimeFiles();

				default:
					return null;
			}
		}

		private List<FileItem> GetForecastFiles()
		{
			if (forecastCacheAge.IsStarted && !forecastCacheAge.IsElapsedMinutes(30))
			{
				return forecastCache;
			}

			List<FileItem> list = GetFiles(DWD_URL_FORECASTS);
			forecastCache = list;
			forecastCacheAge.Start();
			return list;
		}

		private List<FileItem> GetAlertsFiles()
		{
			string[] regs = new string[] { "EM", "EN", "GER", "HA", "LZ", "MS", "OF", "PD", "SU" };

			if (alertsCacheAge.IsStarted && !alertsCacheAge.IsElapsedMinutes(30)) return alertsCache;

			List<FileItem> alertsList = new List<FileItem>();
			foreach (string reg in regs)
			{
				List<FileItem> list = GetFiles(DWD_URL_ALERTS + reg + "/");
				alertsList.AddRange(list);
			}

			alertsCache = alertsList;
			alertsCacheAge.Start();
			return alertsList;
		}

		private List<FileItem> GetMaritimeFiles()
		{
			if (maritimeCacheAge.IsStarted && !maritimeCacheAge.IsElapsedMinutes(30)) return maritimeCache;

			List<FileItem> list = GetFiles(DWD_URL_MARITIME_FORECASTS_DE);
			List<FileItem> list_EN = GetFiles(DWD_URL_MARITIME_FORECASTS_EN);
			list.AddRange(list_EN);
			maritimeCache = list;
			maritimeCacheAge.Start();
			return list;
		}

		private List<FileItem> GetFiles(string url)
		{
			string htmlSource;
			try
			{
				WebClient wClient = new WebClient();
				htmlSource = wClient.DownloadString(url);
			}
			catch (WebException)
			{
				htmlSource = "Error";
			}
			catch (Exception)
			{
				return null;
			}

			HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
			doc.LoadHtml(htmlSource);

			List<FileItem> list = new List<FileItem>();
			HtmlNode preNode = doc.DocumentNode.SelectNodes("//pre").First();

			foreach (HtmlNode fileNode in preNode.SelectNodes("//a"))
			{
				var href = GetHtmlAttrValue(fileNode, "href");
				if (href == "../") continue;
				if (Path.GetExtension(href) == ".pdf") continue;
				list.Add(new FileItem(href, url));
			}



			return list;
		}

		private string GetHtmlAttrValue(HtmlNode node, string name)
		{
			if (node == null)
				return "";

			HtmlAttributeCollection attrColl = node.Attributes;
			if (attrColl == null)
				return "";

			var attr = attrColl[name];
			if (attr == null)
				return "";

			return attr.Value;
		}
	}

	public class FileItem
	{
		private string[] PREFIXES = new string[] { "ber01", "pid", "swis2", "wst04" };

	
		public string RawName { get; set; }

		public string Url { get; set; }

		public bool IsValid { get; set; }

		public bool Corr { get; set; }

		public string Prefix { get; set; }

		public string Field1 { get; set; }

		public string Field2 { get; set; }

		public int Index { get; set; }

		public long DateInt { get; set; }

		public FileItem(string name, string url)
		{
			Url = url;

			IsValid = false;

			RawName = name;

			Prefix = "";
			foreach (string pre in PREFIXES)
			{
				if (name.Substring(0, pre.Length + 1) == pre + "-")
				{
					name = name.Substring(pre.Length + 1);
					Prefix = pre;
					break;
				}
			}

			int p = name.IndexOf("_COR");
			if (p != -1)
			{
				name = name.Substring(0, p) + name.Substring(p + 4);
				Corr = true;
			}

			if (name[6] != '_' || name[11] != '_' || name.Length > 18 && name[18] != '-') return;

			Field1 = name.Substring(0, 6);
			Field2 = name.Substring(7, 4);

			string s;
			s = name.Substring(12, 5);
			if (s == "LATES")
			{
				Index = 99999;
			}
			else
			{
				if (!int.TryParse(s, out int index)) return;
				Index = index;
				if (Corr) Index++;
			}

			DateInt = 0;
			if (name.Length > 18)
			{
				s = name.Substring(19, 10);
				if (!long.TryParse(s, out long dateInt)) return;
				DateInt = dateInt;
			}

			IsValid = true;
		}

		public override string ToString()
		{
			return $" {RawName} {Field1} {Field2} {Index} {DateInt} {IsValid}";
		}
	}

	public class DwdRegion
	{
		public DwdServer.DwdRegionEnum Region { get; set; }

		public string Field { get; set; }

		public string Name { get; set; }

		public string ShortTelex { get; set; }

		public DwdRegion(DwdServer.DwdRegionEnum region, string field, string name, string shortTelex)
		{
			Region = region;
			Field = field;
			Name = name;
			ShortTelex = shortTelex;
		}

		public override string ToString()
		{
			return $"{Region} {Field} {ShortTelex}";
		}
	}

	public class DwdType
	{
		public int Sort { get; set; }

		public DwdServer.DwdTypeEnum Type { get; set; }

		public string Field { get; set; }

		public string Url { get; set; }

		public string Name { get; set; }

		public string ShortTelex { get; set; }

		public DwdType(int sort, DwdServer.DwdTypeEnum type, string field, string name, string shortTelex, string url)
		{
			Sort = sort;
			Type = type;
			Field = field;
			Url = url;
			Name = name;
			ShortTelex = shortTelex;
		}

		public override string ToString()
		{
			return $"{Type} {Field} {ShortTelex}";
		}
	}
}
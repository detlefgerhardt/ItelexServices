using ItelexCommon;
using ItelexCommon.Logger;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ItelexBaudotArtServer
{
	class BaudotArtFiles
	{
		private const string TAG = nameof(BaudotArtFiles);

		private Logging _logger;

		/// <summary>
		/// singleton pattern
		/// </summary>
		private static BaudotArtFiles instance;

		public static BaudotArtFiles Instance => instance ?? (instance = new BaudotArtFiles());

		private BaudotArtFiles()
		{
			_logger = LogManager.Instance.Logger;
		}

		public List<BaudotArtItem> BaudotArtList { get; set; }

		//public List<BaudotArtItem> BaudotArtList = new List<BaudotArtItem>
		//{
		//	new BaudotArtItem("Adenauer", "adenauer.ls"),
		//	new BaudotArtItem("Kennedy", "kennedy.ls"),
		//	new BaudotArtItem("Walter von Lorenz", "walter von lorenz.ls"),
		//};

		public bool LoadFileList()
		{
			string fullConfigName = Path.Combine(Helper.GetExePath(), Constants.FILE_PATH, Constants.FILES_NAME);

			XmlDocument xml = new XmlDocument();
			XmlNodeList entries;

			try
			{
				xml.Load(fullConfigName);
				entries = xml.SelectNodes("/files/file");
			}
			catch (Exception ex)
			{
				_logger.Error(TAG, nameof(LoadFileList), $"error loading {fullConfigName}", ex);
				return false;
			}

			BaudotArtList = new List<BaudotArtItem>();
			foreach (XmlNode node in entries)
			{
				string numberStr = node.Attributes?["number"]?.Value;
				if (!int.TryParse(numberStr, out int number)) continue;
				string name = node.Attributes?["name"]?.Value;
				string filename = node.Attributes?["filename"]?.Value;
				string fullName = Path.Combine(Helper.GetExePath(), Constants.FILE_PATH, filename);
				int size = GetFileSize(fullName);
				if (size > 0)
				{
					BaudotArtItem item = new BaudotArtItem(number, name, fullName, size);
					BaudotArtList.Add(item);
				}
			}

			return true;
		}

		private int GetFileSize(string fullName)
		{
			try
			{
				FileInfo fileInfo = new FileInfo(fullName);
				if (fileInfo == null) return 0;
				return (int)fileInfo.Length;
			}
			catch(Exception ex)
			{
				_logger.Error(TAG, nameof(GetFileSize), $"error reading {fullName}", ex);
				return 0;
			}
		}
	}

	class BaudotArtItem
	{
		public int Number { get; set; }

		public string Name { get; set; }

		public string Filename { get; set; }

		public int Size { get; set; }

		public int DownloadMinutes
		{
			get
			{
				int seconds = (int)(Size / 6.667);
				TimeSpan span = new TimeSpan(seconds * 10000000L);
				return (int)Math.Ceiling(span.TotalMinutes);
			}
		}

		public BaudotArtItem(int number, string name, string filename, int size)
		{
			Number = number;
			Name = name;
			Filename = filename;
			Size = size;
		}

		public override string ToString()
		{
			return $"{Name} {Filename} {Size} {DownloadMinutes}";
		}
	}
}

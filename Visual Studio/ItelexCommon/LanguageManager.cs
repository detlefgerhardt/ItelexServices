using ItelexCommon.Logger;
using ItelexCommon.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ItelexCommon
{
	/// <summary>
	/// This class manages the language definition and change of language at runtime.
	/// On startup it looks for all *.lng files in the exe directory and loads them.
	/// The languages "en" and "de" are hardcoded and do not need a language file, but the hardcoded definition
	/// can be overwritten by a language file.
	/// </summary>

	public class LanguageManager
	{
		private const string TAG = nameof(LanguageManager);

		/// <summary>
		/// singleton pattern
		/// </summary>
		private static LanguageManager instance;

		public static LanguageManager Instance => instance ?? (instance = new LanguageManager());

		public delegate void LanguageChangedEventHandler();
		/// <summary>
		/// Language changed event
		/// </summary>
		//public event LanguageChangedEventHandler LanguageChanged;

		public List<Language> LanguageList { get; private set; }

		private int _defaultLanguageId;

		private LanguageManager()
		{
			LanguageList = new List<Language>();
			/*
			LanguageList.Add(LanguageEnglish.GetLng());
			LanguageList.Add(LanguageDeutsch.GetLng());
			*/

#if DEBUG
			//SaveLanguage(LanguageEnglish.GetLng());
			//SaveLanguage(LanguageDeutsch.GetLng());
#endif
		}

		public void Init(int defaultLngId)
		{
			_defaultLanguageId = defaultLngId;
		}

		public void AddLanguages(Language lng)
		{
			LanguageList.Add(lng);
			//SaveLanguage(lng.GetLng());
		}

		//public Language CurrentLanguage { get; private set; }

		//public const int DefaultLanguageId = LanguageIds.de;

		/// <summary>
		/// Get a list of all language keys
		/// </summary>
		/// <returns></returns>
		//public List<string> GetLanguageKeys()
		//{
		//	return (from l in LanguageList orderby l.Key select $"{l.Key} {l.DisplayName}").ToList();
		//}

		//public string GetText(LngKeys lngKey, Language lng, string[] parameters = null)
		//{
		//	return GetText()

		//}

		public string GetText(int textKey, int lngId, string[] parameters = null)
		{
			Language lng = GetLanguageById(lngId);

			string keyStr = textKey.ToString();

			// get text from current language
			string text = null;
			if (lng.Items.ContainsKey(textKey))
			{
				text = (from l in lng.Items where l.Key == textKey select l.Value).FirstOrDefault();
			}

			if (string.IsNullOrEmpty(text))
			{
				// lng key not found: get text from default language
				lng = GetLanguageById(_defaultLanguageId);
				if (lng.Items.ContainsKey(textKey))
				{
					text = (from l in lng.Items where l.Key == textKey select l.Value).FirstOrDefault();
				}
			}
			if (string.IsNullOrEmpty(text))
			{
				// lng key not found
				return "";
			}

			// replace parameters
			if (parameters != null)
			{
				for (int i = 0; i< parameters.Length; i++)
				{
					text = text.Replace($"@{i + 1}", parameters[i]);
				}
			}

			// replace unresolved parameters
			text = text.Replace("@", "?");

			return text;
		}

		/*
		public void ChangeLanguage(string lngKey, bool force=false)
		{
			Language newLng = GetLanguage(lngKey);
			if (newLng != null)
			{
				CurrentLanguage = newLng;
			}
			else
			{
				SetDefaultLanguage();
			}
			Logging.Instance.Info(TAG, nameof(ChangeLanguage), $"language changed to {CurrentLanguage.Key} {CurrentLanguage.DisplayName}");

			LanguageChanged?.Invoke();
		}

		private void SetDefaultLanguage()
		{
			CurrentLanguage = GetLanguage("DE");
		}
		*/

		public Language GetLanguageById(int id)
		{
			if (id==0)
			{
				id = _defaultLanguageId;
			}

			return (from l in LanguageList where l.Id==id select l).FirstOrDefault();
		}

		public Language GetLanguageByShortname(string shortName)
		{
			return (from l in LanguageList where string.Compare(shortName, l.ShortName, true)==0 select l).FirstOrDefault();
		}

		public Language GetLanguageOrDefaultByShortname(string shortName)
		{
			Language lng = GetLanguageByShortname(shortName);
			if (lng == null)
			{
				lng = (from l in LanguageList where l.IsDefault select l).FirstOrDefault();
			}
			return lng;
		}

		/// <summary>
		/// Load all language files (*.lng) found in the exe directory, replace default language definitions (de/en)
		/// </summary>
		public void LoadAllLanguageFiles()
		{
			string path = FormsHelper.GetExePath();
			DirectoryInfo dirInfo = new DirectoryInfo(path);
			FileInfo[] files = dirInfo.GetFiles("*.lng");
			foreach(FileInfo file in files)
			{
				Language newLng = LoadLanguage(file.FullName);
				if (newLng==null)
				{
					// not a valid language file
					continue;
				}

				// replace or add
				Language oldLng = GetLanguageById(newLng.Id);
				if (oldLng==null)
				{	// add
					LanguageList.Add(newLng);
				}
				else
				{
					// replace existing
					LanguageList.Remove(oldLng);
					LanguageList.Add(newLng);
				}
			}
		}

		public Language LoadLanguage(string filename)
		{
			const char REPLACE_CHAR = '\x01';

			try
			{
				Language language = new Language();
				string[] lines;
				try
				{
					lines = File.ReadAllLines(filename);
				}
				catch(Exception ex)
				{
					Log(LogTypes.Error, nameof(LoadLanguage), $"Error loading {filename}", ex);
					return null;
				}
				for (int i=0; i<lines.Length; i++)
				{
					string line = lines[i];
					if (string.IsNullOrWhiteSpace(line))
					{
						// empty line
						continue;
					}
					line = line.Trim();
					if (line[0]==';')
					{
						// comment line
						continue;
					}

					line = ReplaceQuotedBlanks(line, REPLACE_CHAR);
					string[] words = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
					if (words.Length < 2)
					{
						Log(LogTypes.Error, nameof(LoadLanguage), $"File {filename}: error in line #{line} ({line})");
						return null;
					}
					string cmd = words[0].ToLower();
					string prm = words[1].Replace(REPLACE_CHAR, ' ');
					switch (words[0].ToLower())
					{
						case "id":
							int id;
							if (Enum.TryParse(prm, true, out id))
							{
								language.Id = id;
							}
							else
							{
								Log(LogTypes.Error, nameof(LoadLanguage), $"File {filename}: Invalid language id {prm}");
								return null;
							}
							break;
						case "version":
							if (prm != "1")
							{
								Log(LogTypes.Error, nameof(LoadLanguage), $"File {filename}: Version {prm} not supported");
								return null;
							}
							language.Version = prm;
							break;
						case "name":
							language.DisplayName = prm;
							break;
						case "shortname":
							language.ShortName = prm.ToLower();
							break;
						case "text":
							if (words.Length<3)
							{
								Log(LogTypes.Error, nameof(LoadLanguage), $"File {filename}: error in line #{line} ({line})");
							}
							int lngKey = Language.StringToLngKey(prm);
							if (lngKey==0)
							{
								Log(LogTypes.Error, nameof(LoadLanguage), $"File {filename}: invalid key {prm} in line #{line}");
							}
							string text = words[2].Replace(REPLACE_CHAR, ' ');
							language.Items.Add(lngKey, text);
							break;
					}
				}

				if (string.IsNullOrWhiteSpace(language.ShortName))
				{
					Log(LogTypes.Error, nameof(LoadLanguage), $"File {filename}: missing shortname");
					return null;
				}
				if (string.IsNullOrWhiteSpace(language.DisplayName))
				{
					Log(LogTypes.Error, nameof(LoadLanguage), $"File {filename}: missing name");
					return null;
				}
				if (language.Items.Count==0)
				{
					Log(LogTypes.Error, nameof(LoadLanguage), $"File {filename}: not text items");
					return null;
				}
				return language;
			}
			catch (Exception ex)
			{
				Log(LogTypes.Error, "LoadLanguage", $"language file {filename} not found", ex);
				return null;
			}
		}

		/// <summary>
		/// replace all spaces inside quotes with replaceChar
		/// </summary>
		/// <param name="line"></param>
		/// <param name="replaceChr"></param>
		/// <returns></returns>
		private string ReplaceQuotedBlanks(string line, char replaceChr)
		{
			string newLine = "";
			bool quote = false;
			for (int i=0; i<line.Length; i++)
			{
				char chr = line[i];
				if (!quote)
				{
					if (chr=='\"')
					{
						quote = true;
						continue;
					}
				}
				else
				{
					if (chr == '\"')
					{
						quote = false;
						continue;
					}
					if (chr==' ')
					{
						chr = replaceChr;
					}
				}
				newLine += chr;
			}
			return newLine;
		}

		private static void Log(LogTypes logType, string methode, string msg, Exception ex = null)
		{
			if (logType == LogTypes.Error)
			{
				LogManager.Instance.Logger.Error(TAG, methode, msg, ex);
			}
			else
			{
				LogManager.Instance.Logger.Log(logType, TAG, methode, msg);
			}
		}

		/*
#if DEBUG
		public bool SaveLanguage(Language lng)
		{
			List<string> lines = new List<string>();
			lines.Add($"; {Constants.PROGRAM_NAME} language file");
			lines.Add($"version 1");
			lines.Add($"key \"{lng.Id}\"");
			lines.Add($"shortname \"{lng.ShortName}\"");
			lines.Add($"name \"{lng.DisplayName}\"");
			lines.Add($";");
			foreach(var item in lng.Items)
			{
				lines.Add($"text {item.Key} \"{item.Value}\"");
			}
			lines.Add($";");
			lines.Add($"; end of file");

			try
			{
				File.WriteAllLines($"{lng.Id}_{lng.DisplayName}.lng", lines);
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}
#endif
*/
	}

}

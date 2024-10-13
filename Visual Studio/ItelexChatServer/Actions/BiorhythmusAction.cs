using ItelexChatServer.Languages;
using ItelexCommon;
using ItelexCommon.Connection;
using System;
using System.Diagnostics;

namespace ItelexChatServer.Actions
{
	class BiorhythmusAction : ActionBase
	{
		private const string TAG = nameof(BiorhythmusAction);

		// phsysisch, emotional, intellektuell
		private readonly char[] curveChars = new char[] { 'p', 'e', 'i' };
		private readonly int[] curvePoriods = new int[] { 23, 28, 33 };

		private string debugName => _chatConnection != null ? _chatConnection.ConnectionName : "null";

		public BiorhythmusAction(ActionBase.ActionCallTypes actionCallType, ItelexLogger itelexLogger) : 
				base(Actions.Biorhythmus, LanguageDefinition.GetLanguageById(LanguageIds.en), actionCallType, itelexLogger)
		{
		}

		public override void Run(IncomingChatConnection chatConnection, bool debug)
		{
			base.Run(chatConnection, debug);
			Start();

			if (_actionCallType == ActionCallTypes.FromCmd)
			{
				// return to command mode
				_chatConnection.StartCommandMode();
			}
		}

		private void Start()
		{
			/*
			DateTime bd = new DateTime(2016, 2, 29);
			DateTime sd = new DateTime(2020, 2, 27);
			DateTime ed = new DateTime(2020, 3, 22);
			Print(bd, sd, ed);
			return;
			*/


			//                             12345678901234567890123456789012345678901234567890123456789
			_chatConnection.SendAscii("\r\n\r\n");
			_chatConnection.SendAscii("\r\n                   b i o r h y t h m u s");
			_chatConnection.SendAscii("\r\n                   ---------------------\r\n");

			DateTime? birthDate = InputDatum("geburtsdatum (tt.mm.jjjj)", 3);
			if (birthDate == null)
			{
				return;
			}

			InputResult inputResult = _chatConnection.InputSelection("fuer ein jahr oder einen monat (j/m) ?", ShiftStates.Ltrs,
				"", new string[] { "j", "m" }, 1, 2);
			if (string.IsNullOrEmpty(inputResult.InputString))
			{
				return;
			}
			string monthOrYear = inputResult.InputString.ToLower();

			DateTime? startDate;
			DateTime endDate;
			startDate = InputDatum($"startdatum (mm.jjjj)", monthOrYear == "m" ? 2 : 1);
			if (startDate == null)
			{
				return;
			}

			if (monthOrYear == "m")
			{
				startDate = new DateTime(startDate.Value.Year, startDate.Value.Month, 1);
				endDate = startDate.Value.AddMonths(1).AddDays(-1);
			}
			else
			{
				startDate = new DateTime(startDate.Value.Year, 1, 1);
				endDate = startDate.Value.AddYears(1).AddDays(-1);
			}

			Print(birthDate.Value, startDate.Value, endDate);
		}

		private void Print(DateTime birthDate, DateTime startDate, DateTime endDate)
		{
			DateTime date = startDate;
			int days = (startDate - birthDate).Days;
			int curveWidth = 57;
			int curveWidth2 = curveWidth / 2;
			_chatConnection.SendAscii("\r\n         p = physisch       e = emotional        i = intellektuell");
			_chatConnection.SendAscii($"\r\n         {new string('-', curveWidth)}");
			_chatConnection.SendAscii($"\r\n         tief {new string(' ', curveWidth - 10)} hoch");
			_chatConnection.SendAscii($"\r\n         {new string('-', curveWidth)}");
			while (date <= endDate)
			{
				bool isBirthday = IsBirthDay(birthDate, date);
				char[] line = new char[curveWidth];
				for (int i = 0; i < curveWidth; i++)
				{
					if (isBirthday)
					{
						// birthday
						line[i] = '=';
					}
					else if (date.AddDays(1).Day == 1)
					{
						// last day of month
						line[i] = '-';
					}
					else
					{
						// normal
						line[i] = ' ';
					}
				}
				if (line[curveWidth2]==' ') line[curveWidth2] = ':';
				int[] v = new int[3];
				for (int c = 0; c < 3; c++)
				{
					int val = (int)Math.Round(Math.Sin(2 * Math.PI * days / curvePoriods[c]) * curveWidth2 + curveWidth2);
					v[c] = val;
					line[val] = curveChars[c];
				}
				string lineStr = new string(line).TrimEnd();
				_chatConnection.SendAscii($"\r\n{date:dd.MM.yy} {lineStr}");

				date = date.AddDays(1);
				days++;
			}
			_chatConnection.SendAscii("\r\n\r\n\r\n\r\n");
		}

		private DateTime? InputDatum(string text, int partCount)
		{
			for (int i = 0; i < 3; i++)
			{
				if (!_chatConnection.IsConnected)
				{
					return null;
				}

				InputResult inputResult = _chatConnection.InputString(text, ShiftStates.Figs, "", 11, 1);
				if (string.IsNullOrEmpty(inputResult.InputString))
				{
					continue;
				}

				int day = 1;
				int month = 1;
				int year;
				string[] parts = inputResult.InputString.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length<partCount)
				{
					continue;
				}
				int idx = 0;
				if (partCount == 3)
				{
					if (!int.TryParse(parts[idx++], out day))
					{
						continue;
					}
				}
				if (partCount >= 2)
				{
					if (!int.TryParse(parts[idx++], out month))
					{
						continue;
					}
				}
				if (!int.TryParse(parts[idx], out year))
				{
					continue;
				}
				DateTime gebDat = new DateTime(year, month, day);
				if (gebDat >= DateTime.Now)
				{
					continue;
				}

				try
				{
					return gebDat;
				}
				catch
				{
				}
			}
			return null;
		}

		/// <summary>
		/// Geburtstag im Bereich des Biorhythmus mit Sonderbehandlung für Geburtstag am 29.2.
		/// </summary>
		/// <param name="birthDate"></param>
		/// <param name="date"></param>
		private bool IsBirthDay(DateTime birthDate, DateTime date)
		{
			DateTime birthDay;
			if (!DateTime.IsLeapYear(date.Year) && birthDate.Month == 2 && birthDate.Day == 29)
			{
				// birthday on 29th Feb and no leapyear: use 1.3.
				birthDay = new DateTime(date.Year, 3, 1);
			}
			else
			{
				// use birthday
				birthDay = new DateTime(date.Year, birthDate.Month, birthDate.Day);
			}
			return birthDay.Month == date.Month && birthDay.Day == date.Day;
		}
	}
}

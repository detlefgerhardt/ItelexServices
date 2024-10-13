using ItelexCommon.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ItelexCommon
{
	public static class CodeManager
	{
		private const string TAG = nameof(CodeManager);

		// special ASCII codes
		public const char ASC_INV = '~'; // replace invalid baudot character
		public const char ASC_NUL = '\x00';
		public const char ASC_WRU = '\x05'; // = enquire
		public const char ASC_BEL = '\x07';
		public const char ASC_HEREIS = '\x09';
		public const char ASC_LF = '\x0A';
		public const char ASC_CR = '\x0D';
		public const char ASC_SHIFTF = '\x10';
		public const char ASC_SHIFTG = '\x11';
		public const char ASC_SHIFTH = '\x12';
		public const char ASC_LTRS = '\x1E';
		public const char ASC_FIGS = '\x1F';
		public const char ASC_COND_NL = '\x1D'; // conditional new line
		//public const char ASC_MANLTRS = '\x1C'; // mandatory letters
		//public const char ASC_MANFIGS = '\x1D'; // mandatory figures

		public static readonly string ASC_CR_LTRS = ASC_CR.ToString() + ASC_LTRS.ToString();

		// special ITA2 codes
		public const byte BAU_NUL = 0x00;
		public const byte BAU_CR = 0x02;
		public const byte BAU_LF = 0x08;
		public const byte BAU_WRU = 0x12;
		public const byte BAU_BEL = 0x1A;
		public const byte BAU_FIGS = 0x1B;
		public const byte BAU_LTRS = 0x1F;

		// F=Quadrat ohne Inhalt, G=Quadrat mit Querstrich, H=Quadrat mit Schrägstrich
		public const byte BAU_SHIFTF = 0x16;
		public const byte BAU_SHIFTG = 0x0B;
		public const byte BAU_SHIFTH = 0x05;

		public enum SendRecv { Send, Recv };

		public static string BaudotDataToAsciiDebug(byte[] baudotData, ref ShiftStates shiftState, CodeSets codeSet = CodeSets.ITA2)
		{
			string asciiStr = BaudotDataToAscii(baudotData, ref shiftState, codeSet, true);
			string debugStr = "";
			foreach(char chr in asciiStr)
			{
				string debugSym;
				switch(chr)
				{
					case ASC_CR:
						debugSym = "<CR>";
						break;
					case ASC_LF:
						debugSym = "<LF>";
						break;
					case ASC_LTRS:
						debugSym = "<BU>";
						break;
					case ASC_FIGS:
						debugSym = "<ZI>";
						break;
					case ASC_BEL:
						debugSym = "<BEL>";
						break;
					case ASC_NUL:
						debugSym = "<NULL>";
						break;
					case ASC_WRU:
						debugSym = "<WRU>";
						break;
					default:
						if ((byte)chr >= 0x20 && (byte)chr <= 0x7F)
						{
							debugSym = chr.ToString();
						}
						else
						{
							debugSym = $"<{(byte)chr:X02}>";
						}
						break;
				}
				debugStr += debugSym;
			}
			return debugStr;
		}

		public static string BaudotDataToAscii(byte[] baudotData, ref ShiftStates shiftState, CodeSets codeSet = CodeSets.ITA2, 
			bool keepLtrsFigs = false)
		{
			if (baudotData == null || baudotData.Length == 0) return "";

			string asciiStr = "";
			for (int i = 0; i < baudotData.Length; i++)
			{
				byte baudotChr = baudotData[i];
				if (baudotChr == BAU_LTRS)
				{
					shiftState = ShiftStates.Ltrs;
					if (keepLtrsFigs) asciiStr += (char)ASC_LTRS;
				}
				else if (baudotChr == BAU_FIGS)
				{
					shiftState = ShiftStates.Figs;
					if (keepLtrsFigs) asciiStr += (char)ASC_FIGS;
				}
				else
				{
					char asciiChr;
					if (baudotData[i] > 0x1F)
					{
						asciiChr = ASC_INV;
					}
					else
					{
						asciiChr = _codeTab[baudotData[i]].GetCode(shiftState, codeSet);
					}
					asciiStr += asciiChr;
				}
			}
			return asciiStr;
		}

		public static string BaudotCodeToPuncherText(byte baudotCode, ShiftStates shiftState, CodeSets codeSet)
		{
			if (baudotCode > 0x1F)
			{
				return ASC_INV.ToString();
			}
			return _codeTab[baudotCode].GetName(shiftState, codeSet);
		}

		/// <summary>
		/// Only used for local event
		/// </summary>
		/// <param name="asciiStr"></param>
		/// <param name="shiftState"></param>
		/// <param name="codeSet"></param>
		/// <returns></returns>
		public static byte[] AsciiStringToBaudotLocal(string asciiStr, ref ShiftStates shiftState, CodeSets codeSet, bool asciiTexting)
		{
			byte[] baudotData = new byte[0];
			for (int i = 0; i < asciiStr.Length; i++)
			{
				string telexData = AsciiCharReplacements(asciiStr[i], codeSet, asciiTexting, false);
				byte[] data = AsciiStringToBaudot(telexData, ref shiftState, codeSet);
				baudotData = baudotData.Concat(data).ToArray();
			}
			return baudotData;
		}

		/// <summary>
		/// convert ASCII string to printable characters and replacement characters
		/// </summary>
		/// <param name="asciiStr"></param>
		/// <returns></returns>
		public static string AsciiStringReplacements(string asciiStr, CodeSets codeSet, bool asciiTexting, bool keep128)
		{
			string telexStr = "";
			for (int i = 0; i < asciiStr.Length; i++)
			{
				telexStr += AsciiCharReplacements(asciiStr[i], codeSet, asciiTexting, keep128);
			}
			return telexStr;
		}

		/// <summary>
		/// convert ASCII character to baudot printable character and replacement character
		/// </summary>
		/// <param name="asciiChr"></param>
		/// <returns></returns>
		private static string AsciiCharReplacements(char asciiChr, CodeSets codeSet, bool asciiTexting, bool keep128)
		{
			// when bit 7 is set, do not convert (baudot data)
			if (keep128 && asciiChr >= 128) return char.ToString(asciiChr);

			if (asciiTexting)
			{
				switch(asciiChr)
				{
					case ASC_WRU:
						return "@";
					case ASC_BEL:
						return "%";
					/*
					case ASC_MANFIGS:
					case ASC_MANLTRS:
						return "";
					*/
				}
			}

			AsciiConvItem[] asciiToTelexTab;
			switch (codeSet)
			{
				default:
				case CodeSets.ITA2:
					asciiToTelexTab = _asciiIta2Tab;
					break;
				case CodeSets.USTTY:
					asciiToTelexTab = _asciiUsTtyTab;
					break;
			}

			string asciiData = CodePage437ToPlainAscii(asciiChr);
			string telexData = "";
			for (int i = 0; i < asciiData.Length; i++)
			{
				foreach (AsciiConvItem convItem in asciiToTelexTab)
				{
					string ascii = convItem.GetCodeInRange(asciiData[i]);
					if (!string.IsNullOrEmpty(ascii))
					{
						telexData += ascii;
					}
				}
			}
			return telexData;
		}

		public static string CodePage437ToPlainAscii(string asciiStr)
		{
			string newStr = "";
			foreach(char chr in asciiStr)
			{
				newStr += CodePage437ToPlainAscii(chr);
			}
			return newStr;
		}


		public const string CLEAN_CHARACTERS = "abcdefghijklmnopqrstuvwxyz0123456789+-?()/'.,:= ";

		public static string CleanAscii(string str)
		{
			string newStr = "";
			for (int i = 0; i < str.Length; i++)
			{
				if (CLEAN_CHARACTERS.Contains(str[i].ToString()))
				{
					newStr += str[i];
				}
			}
			return newStr;
		}

		public const string CLEAN_CHARACTERS_CRLF = "abcdefghijklmnopqrstuvwxyz0123456789+-?()/'.,:= \r\n";

		public static string CleanAsciiKeepCrLf(string str)
		{
			string newStr = "";
			for (int i = 0; i < str.Length; i++)
			{
				if (CLEAN_CHARACTERS_CRLF.Contains(str[i].ToString()))
				{
					newStr += str[i];
				}
			}
			return newStr;
		}

		private const string CLEAN_CHARACTERS_EX = "abcdefghijklmnopqrstuvwxyz0123456789+-?()/'.,:=%@ \r\n";

		public static string CleanExAscii(string str)
		{
			string newStr = "";
			for (int i = 0; i < str.Length; i++)
			{
				// if str[i] >= 128 string contains baudot data
				if (/*str[i] >= 128 ||*/ CLEAN_CHARACTERS_EX.Contains(str[i].ToString()))
				{
					newStr += str[i];
				}
			}
			return newStr;
		}

		public static byte[] AsciiStringToBaudot(string telexStr, ref ShiftStates shiftState, CodeSets codeSet)
		{
			byte[] buffer = new byte[0];
			for (int i = 0; i < telexStr.Length; i++)
			{
				byte[] baudotData = AsciiCharToBaudotWithShift(telexStr[i], ref shiftState, codeSet);
				buffer = buffer.Concat(baudotData).ToArray();
			}
			return buffer;
		}

		public static byte[] AsciiCharToBaudotWithShift(char telexChr, ref ShiftStates shiftState, CodeSets codeSet)
		{
			byte? ltrCode = FindBaudot(telexChr, ShiftStates.Ltrs, codeSet);
			byte? figCode = FindBaudot(telexChr, ShiftStates.Figs, codeSet);
			byte baudCode;

			ShiftStates newShiftState;
			if (ltrCode != null && figCode != null)
			{
				baudCode = ltrCode.Value;
				newShiftState = ShiftStates.Both;
			}
			else if (ltrCode != null)
			{
				baudCode = ltrCode.Value;
				newShiftState = ShiftStates.Ltrs;
			}
			else if (figCode != null)
			{
				baudCode = figCode.Value;
				newShiftState = ShiftStates.Figs;
			}
			else
			{
				return new byte[0];
			}

			return BaudotCodeToBaudotWithShift(baudCode, newShiftState, ref shiftState);
		}

		public static byte[] BaudotCodeToBaudotWithShift(byte baudCode, ShiftStates newShiftState, ref ShiftStates shiftState)
		{
			//Logging.Instance.Debug(TAG, nameof(BaudotCodeToBaudotWithShift), $"baudCode={baudCode:X02} ");

			byte[] buffer = new byte[0];

			if (baudCode == BAU_LTRS)
			{
				buffer = CommonHelper.AddByte(buffer, BAU_LTRS);
				shiftState = ShiftStates.Ltrs;
				return buffer;
			}

			if (baudCode == BAU_FIGS)
			{
				buffer = CommonHelper.AddByte(buffer, BAU_FIGS);
				shiftState = ShiftStates.Figs;
				return buffer;
			}

			if (shiftState == ShiftStates.Unknown && newShiftState == ShiftStates.Unknown)
			{
				buffer = CommonHelper.AddByte(buffer, BAU_LTRS);
				newShiftState = ShiftStates.Ltrs;
			}

			if (shiftState == ShiftStates.Unknown && newShiftState == ShiftStates.Ltrs ||
				shiftState == ShiftStates.Figs && newShiftState == ShiftStates.Ltrs)
			{
				buffer = CommonHelper.AddByte(buffer, BAU_LTRS);
				buffer = CommonHelper.AddByte(buffer, baudCode);
				shiftState = ShiftStates.Ltrs;
				return buffer;
			}

			if (shiftState == ShiftStates.Unknown && newShiftState == ShiftStates.Figs ||
				shiftState == ShiftStates.Ltrs && newShiftState == ShiftStates.Figs)
			{
				buffer = CommonHelper.AddByte(buffer, BAU_FIGS);
				buffer = CommonHelper.AddByte(buffer, baudCode);
				shiftState = ShiftStates.Figs;
				return buffer;
			}

			if (shiftState == newShiftState || newShiftState == ShiftStates.Both)
			{
				buffer = CommonHelper.AddByte(buffer, baudCode);
				return buffer;
			}

			// should not happen
			return new byte[0];
		}

		public static string AsciiToDebugStr(string ascStr)
		{
			string newStr = "";
			for (int i=0; i<ascStr.Length; i++)
			{
				char ascChr = ascStr[i];
				int ascCode = (int)ascChr;
				string newChr;
				if (ascCode<32)
				{
					switch(ascCode)
					{
						case ASC_NUL:
							newChr = "<NUL>";
							break;
						case ASC_WRU:
							newChr = "<WRU>";
							break;
						case ASC_BEL:
							newChr = "<KL>";
							break;
						case ASC_LF:
							newChr = "<LF>";
							break;
						case ASC_CR:
							newChr = "<CR>";
							break;
						case ASC_LTRS:
							newChr = "<BU>";
							break;
						case ASC_FIGS:
							newChr = "<ZI>";
							break;
						default:
							newChr = $"<{ascCode:X02}>";
							break;
					}
				}
				else
				{
					newChr = ascChr.ToString();
				}
				newStr += newChr;
			}
			return newStr;
		}

		public static string ChrToStr(char chr, int count = 1)
		{
			char[] data = new char[count];
			for (int i = 0; i < count; i++)
			{
				data[i] = chr;
			}
			return new string(data);
		}

		private static byte? FindBaudot(char asciiChar, ShiftStates shiftState, CodeSets codeSet)
		{
			for (int c = 0; c < 32; c++)
			{
				char chr = _codeTab[c].GetCode(shiftState, codeSet);
				if (chr == asciiChar)
				{
					return (byte)c;
				}
			}
			return null;
		}

		/// <summary>
		/// Code page 437 (0x00-0xFF) to plain ASCII conversion (0x00-0x7F).
		/// This conversion is valid for all code pages
		/// </summary>
		/// <param name="asciiChar"></param>
		/// <returns></returns>
		private static string CodePage437ToPlainAscii(char asciiChar)
		{
			switch (asciiChar)
			{
				case 'ä':
				case 'Ä':
					return "ae";
				case 'ö':
				case 'Ö':
					return "oe";
				case 'ü':
				case 'Ü':
					return "ue";
				case 'ß':
					return "ss";
				case 'á':
				case 'à':
				case 'â':
				case 'Á':
				case 'À':
				case 'Â':
					return "a";
				case 'ç':
					return "c";
				case 'é':
				case 'è':
				case 'ê':
				case 'É':
				case 'È':
				case 'Ê':
					return "e";
				case 'ñ':
					return "n";
				case 'ó':
				case 'ò':
				case 'ô':
				case 'Ó':
				case 'Ò':
				case 'Ô':
					return "o";
				case 'ú':
				case 'ù':
				case 'û':
				case 'Ú':
				case 'Ù':
				case 'Û':
					return "u";
				case 'í':
				case 'ì':
				case 'î':
				case 'Í':
				case 'Ì':
				case 'Î':
					return "i";
				case '°':
					return "o";
				default:
					if (asciiChar < 128)
					{
						return asciiChar.ToString();
					}
					else
					{
						return "";
					}
			}
		}

		public static string AsciiToDump(string asciiStr)
		{
			string newAsciiStr = "";
			for (int i = 0; i < asciiStr.Length; i++)
			{
				int asc = asciiStr[i];
				if (asc < 32)
				{
					newAsciiStr += $"<{asc:X02}>";
				}
				else
				{
					newAsciiStr += asciiStr[i];
				}
			}

			return newAsciiStr;
		}

		public static byte[] MirrorByteArray(byte[] buffer)
		{
			byte[] newBuffer = new byte[buffer.Length];
			for (int i = 0; i < buffer.Length; i++)
			{
				newBuffer[i] = MirrorCode(buffer[i]);
			}
			return newBuffer;
		}

		public static byte MirrorCode(byte code)
		{
			byte inv = 0;
			for (int i = 0; i < 5; i++)
			{
				if ((code & (1 << i)) != 0)
				{
					inv = (byte)(inv | (1 << (4 - i)));
				}
			}
			return inv;
		}

		#region ASCII -> ASCII Telex character set

		private static readonly AsciiConvItem[] _asciiIta2Tab = new AsciiConvItem[]
		{
			new AsciiConvItem(0x00, ASC_NUL),
			new AsciiConvItem(0x05, ASC_WRU),
			new AsciiConvItem(0x07, ASC_BEL),
			new AsciiConvItem(0x0A, ASC_LF),
			new AsciiConvItem(0x0D, ASC_CR),
			new AsciiConvItem(0x10, ASC_SHIFTF),
			new AsciiConvItem(0x11, ASC_SHIFTG),
			new AsciiConvItem(0x12, ASC_SHIFTH),
			new AsciiConvItem(0x1D, ASC_COND_NL),
			new AsciiConvItem(0x1E, ASC_LTRS),
			new AsciiConvItem(0x1F, ASC_FIGS),
			new AsciiConvItem(0x20, ' '),
			new AsciiConvItem(0x22, "''"), // "
			//new AsciiConvItem(0x25, ASC_BEL), // % -> BEL
			new AsciiConvItem(0x27, '\''),
			new AsciiConvItem(0x28, '('),
			new AsciiConvItem(0x29, ')'),
			new AsciiConvItem(0x2B, '+'),
			new AsciiConvItem(0x2C, ','),
			new AsciiConvItem(0x2D, '-'),
			new AsciiConvItem(0x2E, '.'),
			new AsciiConvItem(0x2F, '/'),
			new AsciiConvItem(0x30, 0x39, '0'), // 0..9
			new AsciiConvItem(0x3A, ':'),
			new AsciiConvItem(0x3C, "(."), // <
			new AsciiConvItem(0x3D, '='),
			new AsciiConvItem(0x3E, ".)"), // >
			new AsciiConvItem(0x3F, '?'),
			//new AsciiConvItem(0x40, ASC_WRU), // @ -> WRU
			new AsciiConvItem(0x41, 0x5A, 'a'), // A..Z
			new AsciiConvItem(0x5B, "(:"), // [
			//new AsciiConvItem(0x5C, ""), // \
			new AsciiConvItem(0x5D, ":)"), // ]
			//new AsciiConvItem(0x5E, ""), // ^
			new AsciiConvItem(0x5F, ' '), // _
			new AsciiConvItem(0x60, '\''), // `
			new AsciiConvItem(0x61, 0x7A, 'a'), // a..z
			new AsciiConvItem(0x7B, "(,"), // {
			new AsciiConvItem(0x7C, '/'),
			new AsciiConvItem(0x7D, ",)"), // }
			new AsciiConvItem(0x7E, '-'), // ~
		};

		/*
		private static AsciiConvItem[] _asciiIta2ExtTab = new AsciiConvItem[]
		{
			new AsciiConvItem(0x00, ASC_NUL),
			new AsciiConvItem(0x05, ASC_WRU),
			new AsciiConvItem(0x07, ASC_BEL),
			new AsciiConvItem(0x0A, ASC_LF),
			new AsciiConvItem(0x0D, ASC_CR),
			new AsciiConvItem(0x1E, ASC_LTRS),
			new AsciiConvItem(0x1F, ASC_FIGS),
			new AsciiConvItem(0x20, ' '),
			new AsciiConvItem(0x21, '!'),
			new AsciiConvItem(0x22, "''"), // "
			new AsciiConvItem(0x23, '#'),
			new AsciiConvItem(0x26, '&'),
			new AsciiConvItem(0x27, '\''),
			new AsciiConvItem(0x28, '('),
			new AsciiConvItem(0x29, ')'),
			new AsciiConvItem(0x2B, '+'),
			new AsciiConvItem(0x2C, ','),
			new AsciiConvItem(0x2D, '-'),
			new AsciiConvItem(0x2E, '.'),
			new AsciiConvItem(0x2F, '/'),
			new AsciiConvItem(0x30, 0x39, '0'), // 0..9
			new AsciiConvItem(0x3A, ':'),
			new AsciiConvItem(0x3C, "(."), // <
			new AsciiConvItem(0x3D, '='),
			new AsciiConvItem(0x3E, ".)"), // >
			new AsciiConvItem(0x3F, '?'),
			new AsciiConvItem(0x41, 0x5A, 'a'), // A..Z
			new AsciiConvItem(0x5B, "(:"), // [
			//new AsciiConvItem(0x5C, ""), // \
			new AsciiConvItem(0x5D, ":)"), // ]
			//new AsciiConvItem(0x5E, ""), // ^
			new AsciiConvItem(0x5F, ' '), // _
			new AsciiConvItem(0x60, '\''), // `
			new AsciiConvItem(0x61, 0x7A, 'a'), // a..z
			new AsciiConvItem(0x7B, "(,"), // {
			new AsciiConvItem(0x7C, '/'),
			new AsciiConvItem(0x7D, ",)"), // }
			new AsciiConvItem(0x7E, '-'), // ~
		};
		*/

		private static readonly AsciiConvItem[] _asciiUsTtyTab = new AsciiConvItem[]
		{
			new AsciiConvItem(0x00, ASC_NUL),
			new AsciiConvItem(0x05, ASC_WRU),
			new AsciiConvItem(0x07, ASC_BEL),
			new AsciiConvItem(0x0A, ASC_LF),
			new AsciiConvItem(0x0D, ASC_CR),
			new AsciiConvItem(0x1D, ASC_COND_NL),
			new AsciiConvItem(0x1E, ASC_LTRS),
			new AsciiConvItem(0x1F, ASC_FIGS),
			new AsciiConvItem(0x20, ' '),
			new AsciiConvItem(0x21, '!'),
			new AsciiConvItem(0x22, '"'), // "
			new AsciiConvItem(0x23, '#'),
			new AsciiConvItem(0x24, '$'),
			new AsciiConvItem(0x26, '&'),
			new AsciiConvItem(0x27, '\''),
			new AsciiConvItem(0x28, '('),
			new AsciiConvItem(0x29, ')'),
			new AsciiConvItem(0x2B, '+'),
			new AsciiConvItem(0x2C, ','),
			new AsciiConvItem(0x2D, '-'),
			new AsciiConvItem(0x2E, '.'),
			new AsciiConvItem(0x2F, '/'),
			new AsciiConvItem(0x30, 0x39, '0'), // 0..9
			new AsciiConvItem(0x3A, ':'),
			new AsciiConvItem(0x3C, "(."), // <
			new AsciiConvItem(0x3D, '='),
			new AsciiConvItem(0x3E, ".)"), // >
			new AsciiConvItem(0x3F, '?'),
			new AsciiConvItem(0x40, '@'),
			new AsciiConvItem(0x41, 0x5A, 'a'), // A..Z
			new AsciiConvItem(0x5B, "(:"), // [
			//new AsciiConvItem(0x5C, ""), // \
			new AsciiConvItem(0x5D, ":)"), // ]
			//new AsciiConvItem(0x5E, ""), // ^
			new AsciiConvItem(0x5F, ' '), // _
			new AsciiConvItem(0x60, '\''), // `
			new AsciiConvItem(0x61, 0x7A, 'a'), // a..z
			new AsciiConvItem(0x7B, "(,"), // {
			new AsciiConvItem(0x7C, '/'),
			new AsciiConvItem(0x7D, ",)"), // }
			new AsciiConvItem(0x7E, '-'), // ~
		};


#endregion

		private static readonly CodeItem[] _codeTab = new CodeItem[32]
		{
			new CodeItem(
				0x00,
				ASC_NUL,
				new string[] { "", "" }
				),
			new CodeItem(
				0x01,
				new char?[] { 't' },
				new char?[] { '5' },
				new string[] { "t" },
				new string[] { "5" }
				),
			new CodeItem(
				0x02,
				'\r',
				new string[] { "WR", "CR" }
				),
			new CodeItem(
				0x03,
				new char?[] { 'o' },
				new char?[] { '9' },
				new string[] { "o" },
				new string[] { "9" }
				),
			new CodeItem(
				0x04,
				' ',
				new string[] { "ZWR", "SP" }
				),
			new CodeItem(
				0x05,
				new char?[] { 'h' },
				new char?[] { ASC_SHIFTH, '#' },
				new string[] { "h" },
				new string[] { ASC_SHIFTH.ToString(), "#" },
				'#',
				"#"
				),
			new CodeItem(
				0x06,
				new char?[] { 'n' },
				new char?[] { ',' },
				new string[] { "n" },
				new string[] { "," }
				),
			new CodeItem(
				0x07,
				new char?[] { 'm' },
				new char?[] { '.' },
				new string[] { "m" },
				new string[] { "." }
				),
			new CodeItem(
				0x08,
				'\n',
				new string[] { "ZL", "NL" }
				),
			new CodeItem(
				0x09,
				new char?[] { 'l' },
				new char?[] { ')' },
				new string[] { "l" },
				new string[] { ")" }
				),
			new CodeItem(
				0x0A,
				new char?[] { 'r' },
				new char?[] { '4' },
				new string[] { "r" },
				new string[] { "4" }
				),
			new CodeItem(
				0x0B,
				new char?[] { 'g' },
				new char?[] { ASC_SHIFTG, '&' }, // or @
				new string[] { "g" },
				new string[] { ASC_SHIFTG.ToString(), "&" }, // or @
				'&',
				"&"
				),
			new CodeItem(
				0x0C,
				new char?[] { 'i' },
				new char?[] { '8' },
				new string[] { "i" },
				new string[] { "8" }
				),
			new CodeItem(
				0x0D,
				new char?[] { 'p' },
				new char?[] { '0' },
				new string[] { "p" },
				new string[] { "0" }
				),
			new CodeItem(
				0x0E,
				new char?[] { 'c' },
				new char?[] { ':' },
				new string[] { "c" },
				new string[] { ":" }
				),
			new CodeItem(
				0x0F,
				new char?[] { 'v' },
				new char?[] { '=' },
				new string[] { "v" },
				new string[] { "=" }
				),
			new CodeItem(
				0x10,
				new char?[] { 'e' },
				new char?[] { '3' },
				new string[] { "e" },
				new string[] { "3" }
				),
			new CodeItem(
				0x11,
				new char?[] { 'z' },
				new char?[] { '+', '"' },
				new string[] { "z" },
				new string[] { "+", "\"" }
				),
			new CodeItem(
				0x12,
				new char?[] { 'd' },
				new char?[] { ASC_WRU, '$' }, // or Pound
				new string[] { "d" },
				new string[] { "WRU", "$" } // or Pound
				),
			new CodeItem(
				0x13,
				new char?[] { 'b' },
				new char?[] { '?' },
				new string[] { "b" },
				new string[] { "?" }
				),
			new CodeItem(
				0x14,
				new char?[] { 's' },
				new char?[] { '\'', ASC_BEL },
				new string[] { "s" },
				new string[] { "'", "BEL" }
				),
			new CodeItem(
				0x15,
				new char?[] { 'y' },
				new char?[] { '6' },
				new string[] { "y" },
				new string[] { "6" }
				),
			new CodeItem(
				0x16,
				new char?[] { 'f' },
				new char?[] { ASC_SHIFTF, '!' }, // or %
				new string[] { "f" },
				new string[] { ASC_SHIFTF.ToString(), "!" }, // or %
				'!',
				"!"
				),
			new CodeItem(
				0x17,
				new char?[] { 'x' },
				new char?[] { '/' },
				new string[] { "x" },
				new string[] { "/" }
				),
			new CodeItem(
				0x18,
				new char?[] { 'a' },
				new char?[] { '-' },
				new string[] { "a" },
				new string[] { "-" }
				),
			new CodeItem(
				0x19,
				new char?[] { 'w' },
				new char?[] { '2' },
				new string[] { "w" },
				new string[] { "2" }
				),
			new CodeItem(
				0x1A,
				new char?[] { 'j' },
				new char?[] { ASC_BEL, '\'' },
				new string[] { "j" },
				new string[] { "BEL", "'" }
				),
			new CodeItem(
				0x1B,
				ASC_FIGS,
				new string[] { "Zi", "Fig" }
				),
			new CodeItem(
				0x1C,
				new char?[] { 'u' },
				new char?[] { '7' },
				new string[] { "u" },
				new string[] { "7" }
				),
			new CodeItem(
				0x1D,
				new char?[] { 'q' },
				new char?[] { '1' },
				new string[] { "q" },
				new string[] { "1" }
				),
			new CodeItem(
				0x1E,
				new char?[] { 'k' },
				new char?[] { '(' },
				new string[] { "k" },
				new string[] { "(" }
				),
			new CodeItem(
				0x1F,
				ASC_LTRS,
				new string[] { "Bu", "Ltr" }
				)
		};

#region Keyboard handling

		/// <summary>
		/// all characters that are to be recognized as input in the terminal windows must be explicitly
		/// defined here.
		/// </summary>
		/// <param name="keyChar"></param>
		/// <returns></returns>
		public static char? KeyboardCharacters(char keyChar)
		{
			int code = (int)keyChar;
			char? newChar = null;

			// all characters that are to be recognized as input in the terminal windows must be explicitly defined here.
			switch (char.ToLower(keyChar))
			{
				default:
					// letters and numbers
					if (code >= 0x30 && code <= 0x39 || code >= 0x41 && code <= 0x5A || code >= 0x61 && code <= 0x7A)
					{
						newChar = keyChar;
					}
					break;
				case ' ':
				case '+':
				case '-':
				case ',':
				case '.':
				case ':':
				case '\'':
				case '/':
				case '(':
				case ')':
				case '=':
				case '?':
				// tty-us characters
				case '@':
				case '!':
				case '$':
				case '%':
				case '&':
				case '#':
				case ';':
				// characters that will be replaced
				case 'ä':
				case 'ö':
				case 'ü':
				case 'ß':
				case '[':
				case ']':
				case '{':
				case '}':
				case '<':
				case '>':
				case '´':
				case '`':
				// control characters
				case ASC_WRU: // ctrl-e ENQ
				case ASC_BEL: // ctrl-g BEL
				case ASC_HEREIS: // ctrl-i here is
				case '\x17': // ctrl-w WRU
					newChar = keyChar;
					break;
			}
			return newChar;
		}

#endregion
	}
}

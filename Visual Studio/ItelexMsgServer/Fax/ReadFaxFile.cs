using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Diagnostics;
using ItelexMsgServer.Fax;

namespace ItelexMsgServer.Serial
{
	public class ReadFaxFile
	{
		// This sample shows how to convert a System.Drawing.Bitmap to the black&white CCITT compressed TIFF image.
		public void Read()
		{
			//string filename = "ccitt_hex.g3";
			//string filename = "AD_P.FAX";
			//string filename = "fax_image_inv.g3";
			string filename = "telex.g3";

			byte[] data = File.ReadAllBytes(filename);
			data = MirrorBits(data);
			Bitmap bmp = DecodeFax(data, 1728);
		}

		public void Decode(byte[] data)
		{
			DecodeFax(data, 1728);
		}

		private Bitmap DecodeFax(byte[] data, int lineWidth)
		{
			List<byte[]> lines = new List<byte[]>();
			int pos = 0;

			bool white = true;
			bool foundEol = false;
			int eolCnt = 0;
			byte[] line = new byte[lineWidth];
			int width = 0;
			string pattern = "";
			while (true)
			{
				if (lines.Count == 256)
				{
					Debug.Write("");
				}

				string wbStr = white ? "w" : "b";

				int bytePos = pos / 8;
				if (bytePos >= data.Length) break;

				int bitPos = pos % 8;
				pos++;
				if (bytePos >= data.Length) break; // end of file
				string bit = (data[bytePos] & (1 << bitPos)) != 0 ? "1" : "0";
				pattern += bit;

				//string pattern = GetNextDataAsPattern(data, pos);
				(int RunLength, int PatternLength, Code Code) code = FindCode(white, pattern);
				//Debug.WriteLine($"{wbStr} {pattern} {code.RunLength} {code.PatternLength}");

				if (code.RunLength == CODE_NOT_FOUND)
				{
					if (IsFill(pattern))
					{
						//Debug.WriteLine($"{pos}/{lines.Count}: {wbStr} fill");
					}
					else
					{
						//Debug.WriteLine($"{pos}/{lines.Count}: {wbStr} invalid pattern {pattern}");
						//Debug.WriteLine($"{pattern}");
					}
					//pos++;
					continue;
				}
				if (code.RunLength == EOL_CODE)
				{
					//Debug.WriteLine($"{pos}/{lines.Count}: EOL {pattern} {width}");
					//pos += pattern.Length;
					if (foundEol)
					{
						lines.Add(line);
						line = new byte[lineWidth];
					}
					width = 0;
					white = true;
					pattern = "";
					foundEol = true;
					eolCnt++;
					if (eolCnt >= 6)
					{
						break;
					}
					continue;
				}

				if (!foundEol) continue;

				//Debug.WriteLine($"{pos}/{lines.Count}: {wbStr} {code.RunLength} {width}");

				byte wb = white ? (byte)0 : (byte)1;
				for (int i = 0; i < code.RunLength; i++)
				{
					if (width < lineWidth)
					{
						line[width] = wb;
					}
					width++;
				}

				if (code.RunLength < 64)
				{   // terminating code
					white = !white;
				}
				//pos += pattern.Length;
				pattern = "";
				eolCnt = 0;

				if (width > lineWidth)
				{
					Debug.WriteLine($"{pos}/{lines.Count}: error, linewidth = {width}");
				}

				/*
				if (width >= lineWidth)
				{
					if (width > lineWidth)
					{
						Debug.WriteLine($"{pos}/{lines.Count}: error, linewidth = {width}");
					}
					lines.Add(line);
					line = new byte[lineWidth];
					width = 0;
					white = true;
					continue;
				}
				*/
			}

			Bitmap bmp = new Bitmap(lineWidth, lines.Count);
			for (int y = 0; y < lines.Count; y++)
			{
				byte[] lin = lines[y];
				for (int x = 0; x < lineWidth; x++)
				{
					Color col = lin[x] == 0 ? Color.White : Color.Black;
					bmp.SetPixel(x, y, col);
				}
			}

			bmp.Save("xxxxx.png", ImageFormat.Png);

			return null;
		}

		private string GetNextDataAsPattern(byte[] data, int pos)
		{
			string code = "";
			for (int i = 0; i < MAX_CODE_LEN; i++)
			{
				int bytePos = pos / 8;
				int bitPos = pos % 8;
				if (bytePos >= data.Length) return code; // end of file

				string b = (data[bytePos] & (1 << bitPos)) != 0 ? "1" : "0";
				code += b;
				pos++;
			}
			return code;
		}

		private (int, int, Code) FindCode(bool white, string searchPattern)
		{
			if (searchPattern.EndsWith(EOL)) return (EOL_CODE, EOL.Length, null);
			//if (CompPattern(EOL, searchPattern)) return (EOL_CODE, EOL.Length, null);

			Code[] codes = white ? WhiteCodes : BlackCodes;
			Code code = (from c in codes where CompPattern(c.Pattern, searchPattern) select c).FirstOrDefault();
			if (code == null)
			{
				code = (from c in MakeUpCodes where CompPattern(c.Pattern, searchPattern) select c).FirstOrDefault();
			}
			if (code == null) return (CODE_NOT_FOUND, 0, null);
			return (code.RunLength, code.Pattern.Length, code);
		}

		private bool CompPattern(string pattern, string searchPattern)
		{
			if (searchPattern.Length < pattern.Length) return false;
			return searchPattern.Substring(0, pattern.Length) == pattern;
		}

		private bool IsFill(string pattern)
		{
			int len = Math.Min(pattern.Length, 12);
			for (int i = 0; i < len; i++)
			{
				if (pattern[i] != '0') return false;
			}
			return true;
		}

		private static byte[] MirrorBits(byte[] bytes)
		{
			byte[] mirror = new byte[bytes.Length];
			for (int i = 0; i < bytes.Length; i++)
			{
				int by = bytes[i];
				int mir = 0;
				for (int b = 0; b < 8; b++)
				{
					if ((by & (1 << b)) != 0)
					{
						mir |= (1 << 7 - b);
					}
				}
				mirror[i] = (byte)mir;
			}

			return mirror;
		}

		private const int MAX_CODE_LEN = 13;

		private Code[] WhiteCodes = new Code[]
		{
			// terminating code white
			new Code(0, "00110101"),
			new Code(1, "000111"),
			new Code(2, "0111"),
			new Code(3, "1000"),
			new Code(4, "1011"),
			new Code(5, "1100"),
			new Code(6, "1110"),
			new Code(7, "1111"),
			new Code(8, "10011"),
			new Code(9, "10100"),
			new Code(10, "00111"),
			new Code(11, "01000"),
			new Code(12, "001000"),
			new Code(13, "000011"),
			new Code(14, "110100"),
			new Code(15, "110101"),
			new Code(16, "101010"),
			new Code(17, "101011"),
			new Code(18, "0100111"),
			new Code(19, "0001100"),
			new Code(20, "0001000"),
			new Code(21, "0010111"),
			new Code(22, "0000011"),
			new Code(23, "0000100"),
			new Code(24, "0101000"),
			new Code(25, "0101011"),
			new Code(26, "0010011"),
			new Code(27, "0100100"),
			new Code(28, "0011000"),
			new Code(29, "00000010"),
			new Code(30, "00000011"),
			new Code(31, "00011010"),
			new Code(32, "00011011"),
			new Code(33, "00010010"),
			new Code(34, "00010011"),
			new Code(35, "00010100"),
			new Code(36, "00010101"),
			new Code(37, "00010110"),
			new Code(38, "00010111"),
			new Code(39, "00101000"),
			new Code(40, "00101001"),
			new Code(41, "00101010"),
			new Code(42, "00101011"),
			new Code(43, "00101100"),
			new Code(44, "00101101"),
			new Code(45, "00000100"),
			new Code(46, "00000101"),
			new Code(47, "00001010"),
			new Code(48, "00001011"),
			new Code(49, "01010010"),
			new Code(50, "01010011"),
			new Code(51, "01010100"),
			new Code(52, "01010101"),
			new Code(53, "00100100"),
			new Code(54, "00100101"),
			new Code(55, "01011000"),
			new Code(56, "01011001"),
			new Code(57, "01011010"),
			new Code(58, "01011011"),
			new Code(59, "01001010"),
			new Code(60, "01001011"),
			new Code(61, "00110010"),
			new Code(62, "00110011"),
			new Code(63, "00110100"),
			// extended make up codes white
			new Code(64, "11011"),
			new Code(128, "10010"),
			new Code(192, "010111"),
			new Code(256, "0110111"),
			new Code(320, "00110110"),
			new Code(384, "00110111"),
			new Code(448, "01100100"),
			new Code(512, "01100101"),
			new Code(576, "01101000"),
			new Code(640, "01100111"),
			new Code(704, "011001100"),
			new Code(768, "011001101"),
			new Code(832, "011010010"),
			new Code(896, "011010011"),
			new Code(960, "011010100"),
			new Code(1024, "011010101"),
			new Code(1088, "011010110"),
			new Code(1152, "011010111"),
			new Code(1216, "011011000"),
			new Code(1280, "011011001"),
			new Code(1344, "011011010"),
			new Code(1408, "011011011"),
			new Code(1472, "010011000"),
			new Code(1536, "010011001"),
			new Code(1600, "010011010"),
			new Code(1664, "011000"),
			new Code(1728, "010011011")
		};

		private Code[] BlackCodes = new Code[]
		{
			// terminating code black
			new Code(0, "0000110111"),
			new Code(1, "010"),
			new Code(2, "11"),
			new Code(3, "10"),
			new Code(4, "011"),
			new Code(5, "0011"),
			new Code(6, "0010"),
			new Code(7, "00011"),
			new Code(8, "000101"),
			new Code(9, "000100"),
			new Code(10, "0000100"),
			new Code(11, "0000101"),
			new Code(12, "0000111"),
			new Code(13, "00000100"),
			new Code(14, "00000111"),
			new Code(15, "000011000"),
			new Code(16, "0000010111"),
			new Code(17, "0000011000"),
			new Code(18, "0000001000"),
			new Code(19, "00001100111"),
			new Code(20, "00001101000"),
			new Code(21, "00001101100"),
			new Code(22, "00000110111"),
			new Code(23, "00000101000"),
			new Code(24, "00000010111"),
			new Code(25, "00000011000"),
			new Code(26, "000011001010"),
			new Code(27, "000011001011"),
			new Code(28, "000011001100"),
			new Code(29, "000011001101"),
			new Code(30, "000001101000"),
			new Code(31, "000001101001"),
			new Code(32, "000001101010"),
			new Code(33, "000001101011"),
			new Code(34, "000011010010"),
			new Code(35, "000011010011"),
			new Code(36, "000011010100"),
			new Code(37, "000011010101"),
			new Code(38, "000011010110"),
			new Code(39, "000011010111"),
			new Code(40, "000001101100"),
			new Code(41, "000001101101"),
			new Code(42, "000011011010"),
			new Code(43, "000011011011"),
			new Code(44, "000001010100"),
			new Code(45, "000001010101"),
			new Code(46, "000001010110"),
			new Code(47, "000001010111"),
			new Code(48, "000001100100"),
			new Code(49, "000001100101"),
			new Code(50, "000001010010"),
			new Code(51, "000001010011"),
			new Code(52, "000000100100"),
			new Code(53, "000000110111"),
			new Code(54, "000000111000"),
			new Code(55, "000000100111"),
			new Code(56, "000000101000"),
			new Code(57, "000001011000"),
			new Code(58, "000001011001"),
			new Code(59, "000000101011"),
			new Code(60, "000000101100"),
			new Code(61, "000001011010"),
			new Code(62, "000001100110"),
			new Code(63, "000001100111"),
			// extended make up codes black
			new Code(064, "0000001111"),
			new Code(128, "000011001000"),
			new Code(192, "000011001001"),
			new Code(256, "000001011011"),
			new Code(320, "000000110011"),
			new Code(384, "000000110100"),
			new Code(448, "000000110101"),
			new Code(512, "0000001101100"),
			new Code(576, "0000001101101"),
			new Code(640, "0000001001010"),
			new Code(704, "0000001001011"),
			new Code(768, "0000001001100"),
			new Code(832, "0000001001101"),
			new Code(896, "0000001110010"),
			new Code(960, "0000001110011"),
			new Code(1024, "0000001110100"),
			new Code(1088, "0000001110101"),
			new Code(1152, "0000001110110"),
			new Code(1216, "0000001110111"),
			new Code(1280, "0000001010010"),
			new Code(1344, "0000001010011"),
			new Code(1408, "0000001010100"),
			new Code(1472, "0000001010101"),
			new Code(1536, "0000001011010"),
			new Code(1600, "0000001011011"),
			new Code(1664, "0000001100100"),
			new Code(1728, "0000001100101"),
		};

		private Code[] MakeUpCodes = new Code[]
		{
			// extended make up codes white and black
			new Code(1792,"00000001000"),
			new Code(1856,"00000001100"),
			new Code(1920,"00000001101"),
			new Code(1984,"000000010010"),
			new Code(2048,"000000010011"),
			new Code(2112,"000000010100"),
			new Code(2176,"000000010101"),
			new Code(2240,"000000010110"),
			new Code(2304,"000000010111"),
			new Code(2368,"000000011100"),
			new Code(2432,"000000011101"),
			new Code(2496,"000000011110"),
			new Code(2560,"000000011111")
		};

		private const string EOL = "000000000001";
		//private const string EOL = "000000001";
		private const int EOL_CODE = 10000;

		private const int CODE_NOT_FOUND = -1;

		private byte[] RTC = new byte[] { 0x00, 0x08, 0x80, 0x00, 0x08, 0x80, 0x00, 0x08, 0x80 };

	}

}

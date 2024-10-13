using ItelexCommon;
using ItelexCommon.Logger;
using ItelexCommon.Utility;
using ItelexMsgServer.Data;
using ItelexMsgServer.Serial;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Threading.Tasks;

namespace ItelexMsgServer.Fax
{
	public enum FaxColor { White = 0, Black = 1 };

	public enum FaxFormat { Endless = 1, A4 = 2 };

	public enum FaxModemResponse { SendOk = 1, Busy = 2, NoDialtone = 3, Class2Error = 4, CommError = 5, Timeout = 6,
			Error = 9 };

	public class FaxManager
	{
		private const string TAG = nameof(FaxManager);

		public const int FAX_A4_WIDTH = 1728;
		public const int FAX_A4_HIGHT = 2394;

		private const int FAX_MAX_RETRIES = 3;
		//private readonly int[] FaxWaitRetrySec = new int[] { 0, 60, 120, 600, 3600 }; // seconds
		private readonly int[] FaxWaitRetrySec = new int[] { 0, 5*60, 10*60 }; // seconds

		private const int CHARS_PER_LINE = 68;

		protected Logging _logger;

		private MsgServerDatabase _database;

		private MessageDispatcher _messageDispatcher;

		private volatile bool _sendTimerActive;
		private System.Timers.Timer _sendTimer;

		private object _modemLock = new object();

		/// <summary>
		/// singleton pattern
		/// </summary>
		private static FaxManager instance;

		public static FaxManager Instance => instance ?? (instance = new FaxManager());

		private FaxManager()
		{
			_logger = LogManager.Instance.Logger;
			_database = MsgServerDatabase.Instance;
			_messageDispatcher = MessageDispatcher.Instance;
		}

		public void Start()
		{
			_sendTimer = new System.Timers.Timer(1000 * 10);
			_sendTimerActive = false;
			_sendTimer.Elapsed += SendTimer_Elapsed;
			_sendTimer.Start();
		}

#if FALSE
		public void Test()
		{
			/*
			Bitmap bmp = new Bitmap(FAX_A4_WIDTH, FAX_A4_HIGHT);
			Graphics gr = Graphics.FromImage(bmp);
			gr.Clear(Color.White);
			bmp.SetPixel(0, 0, Color.Black);
			Pen pen = new Pen(Color.Black, 1);
			//gr.DrawLine(pen, 0, 0, 1, 1);
			gr.DrawRectangle(pen, 10, 10, FAX_A4_WIDTH - 20, FAX_A4_HIGHT - 20);
			*/

			List<byte[]> allPages = new List<byte[]>();
			int pageCnt = 3;
			for (int page = 1; page <= pageCnt; page++)
			{
				Bitmap bmp = FaxConversion.CreateFaxImageText(page, pageCnt);
				byte[] data = ConvertToG3FaxFormat(bmp);
				allPages.Add(data);
			}

			ReadFaxFile rff = new ReadFaxFile();
			rff.Decode(allPages[0]);

			//FaxModem fm = new FaxModem();
			//fm.SendFax(allPages, FaxFormat.A4);
	}

		public void Test2()
		{
			string msg = File.ReadAllText("fernschreiber t100.txt");
			//TransmitFax(msg, FaxFormat.Endless, true, "7822222", "06426 921125", "de");
		}

		public void Test3()
		{
			string msg = "ein wirklich sehr kurzer text\r\nviele gruesse, detlef\r\n" +
				"abc" + CodeManager.ASC_BEL + "abc" + CodeManager.ASC_WRU + "abc" + CodeManager.ASC_SHIFTF +
				"abc" + CodeManager.ASC_SHIFTG + "abc" + CodeManager.ASC_SHIFTH + "abc";
			//TransmitFax(msg, FaxFormat.Endless, true, "7822222", "06426 921125", "en");
		}
#endif

		public void SendFax(string msg, FaxFormat faxFormat, bool edgePrint, Int64 userId, bool isMiniTelex, string iTelexNumber,
				string faxNumber, string lng)
		{
			if (string.IsNullOrWhiteSpace(msg)) return;

			int faxId = _database.FaxQueueGetFaxId();

			_logger.Info(TAG, nameof(SendFax),
				$"start isMiniTelex={isMiniTelex} faxId={faxId} userId={userId} number={iTelexNumber} {faxNumber} {faxFormat} {edgePrint} {lng}");

			FaxQueueItem item = new FaxQueueItem()
			{
				FaxId = faxId,
				UserId = userId,
				IsMiniTelex = isMiniTelex,
				Sender = iTelexNumber,
				Receiver = faxNumber,
				Message = msg,
				FaxFormat = (int)faxFormat,
				EdgePrint = edgePrint,
				Language = lng,
				CreateTimeUtc = DateTime.UtcNow,
				SendRetries = 0,
				Status = (int)FaxStatis.Pending,
				LineCount = 0,
			};
			_database.FaxQueueInsert(item);
			_messageDispatcher.Dispatch($"new fax from {iTelexNumber} to {faxNumber} len={msg.Length}");
		}

		private void SendTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			if (_sendTimerActive) return;
			Task.Run(() =>
			{
				_sendTimerActive = true;
				TaskManager.Instance.AddTask(Task.CurrentId, TAG, nameof(SendTimer_Elapsed));
				try
				{
					List<FaxQueueItem> items = _database.FaxQueueLoadAllPending(FAX_MAX_RETRIES);
					if (items == null) return;
					foreach(FaxQueueItem item in items)
					{
						if (item.LastRetryTimeUtc != null)
						{
							DateTime nextRetry = item.LastRetryTimeUtc.Value.AddSeconds(FaxWaitRetrySec[item.SendRetries]);
							if (DateTime.UtcNow < nextRetry) continue;
						}
						TransmitFax(item);
					}
				}
				finally
				{
					_sendTimerActive = false;
					TaskManager.Instance.RemoveTask(Task.CurrentId);
				}
			});
		}

		public void TransmitFax(FaxQueueItem faxQueueItem)
		{
			_logger.Info(TAG, nameof(TransmitFax), $"start {faxQueueItem}");

			if (faxQueueItem.Message == null) return;

			string msg = faxQueueItem.Message.Trim(new char[] { '\r', '\n', ' ' });
			if (msg.Length == 0) return;

			_messageDispatcher.Dispatch($"transmit fax from {faxQueueItem.Sender} to {faxQueueItem.Receiver}");

			DateTime now = DateTime.Now;
			DateTime utcNow = DateTime.UtcNow;
			List<Bitmap> bmpPages = null;
			int lineCnt = 0;
			switch ((FaxFormat)faxQueueItem.FaxFormat)
			{
				case FaxFormat.Endless:
					bmpPages = FaxEndless(msg, faxQueueItem.FaxId, faxQueueItem.IsMiniTelex, faxQueueItem.EdgePrint,
							faxQueueItem.Sender, faxQueueItem.Receiver, faxQueueItem.Language, now, out lineCnt);
					break;
				case FaxFormat.A4:
					bmpPages = FaxA4(msg, faxQueueItem.FaxId, faxQueueItem.IsMiniTelex, faxQueueItem.EdgePrint,
							faxQueueItem.Sender, faxQueueItem.Receiver, faxQueueItem.Language, now, out lineCnt);
					break;
			}

			for (int p = 0; p < 10; p++)
			{
				try
				{
					File.Delete($"faxpage{p + 1}.png");
				}
				catch
				{
				}

				if (p < bmpPages.Count)
				{
					try
					{
						bmpPages[p].Save($"faxpage{p + 1}.png", ImageFormat.Png);
					}
					catch
					{
					}
				}
			}


			List<byte[]> g3Pages = new List<byte[]>();
			foreach (Bitmap bmpPage in bmpPages)
			{
				byte[] data = ConvertToG3FaxFormat(bmpPage);
				g3Pages.Add(data);
			}

#if DEBUG
			return;
#endif

			FaxModemResponse response;
			lock (_modemLock)
			{
				FaxModem fm = new FaxModem();
				response = fm.SendClass2Fax(g3Pages, Constants.OWN_FAX_NUMBER, faxQueueItem.Receiver,
						(FaxFormat)faxQueueItem.FaxFormat);
			}

			_messageDispatcher.Dispatch($"transmit fax from {faxQueueItem.Sender} to {faxQueueItem.Receiver} {response}");

			faxQueueItem.LastRetryTimeUtc = utcNow;
			faxQueueItem.Response = (int)response;
			faxQueueItem.SendRetries++;
			//if (response != FaxModemResponse.SendOk && response != FaxModemResponse.Class2Error)
			if (response == FaxModemResponse.SendOk)
			{
				_logger.Info(TAG, nameof(TransmitFax),
						$"transmit fax from {faxQueueItem.Sender} to {faxQueueItem.Receiver} FaxId={faxQueueItem.FaxId} resp={response}");
				faxQueueItem.Status = (int)FaxStatis.Ok;
				faxQueueItem.SendTimeUtc = now;
				faxQueueItem.LineCount = lineCnt;
				_database.FaxQueueUpdate(faxQueueItem);
				return;
			}

			if (response == FaxModemResponse.Class2Error)
			{
				// fewer retries for Class2 errors
				faxQueueItem.SendRetries++;
			}
			if (faxQueueItem.SendRetries >= FAX_MAX_RETRIES)
			{
				faxQueueItem.Status = (int)FaxStatis.Failed;
			}
			_database.FaxQueueUpdate(faxQueueItem);
			_logger.Notice(TAG, nameof(TransmitFax),
					$"transmit fax from {faxQueueItem.Sender} to {faxQueueItem.Receiver} FaxId={faxQueueItem.FaxId} " +
					$"retries={faxQueueItem.SendRetries} resp={response}");
		}

		private List<Bitmap> FaxEndless(string msg, int faxId, bool isMiniTelex, bool edgePrint, string iTelexNumber,
				string faxNumber, string lng, DateTime now, out int lineCnt)
		{
			const int START_X = 210;
			const int START_Y = 130;
			const int DX = 19;
			const int DY = 42;

			int lineNr = 0;
			int colNr = 0;
			bool wrap = false;

			msg += "\r\n\n\n";

			// pass 1, get image height by number of lines
			foreach (char c in msg)
			{
				switch (c)
				{
					case '\r':
						colNr = 0;
						break;
					case '\n':
						if (!wrap)
						{
							lineNr++;
						}
						wrap = false;
						break;
					default:
						colNr++;
						wrap = false;
						if (colNr >= CHARS_PER_LINE)
						{
							colNr = 0;
							lineNr++;
							wrap = true;
						}
						break;
				}
			}

			// pass 2, create image
			lineCnt = 0;

			int faxHeight = START_Y + lineNr * DY;
			Bitmap bmpPage = new Bitmap(FAX_A4_WIDTH, faxHeight);
			Graphics gr = Graphics.FromImage(bmpPage);
			gr.SmoothingMode = SmoothingMode.None;
			gr.TextRenderingHint = TextRenderingHint.SingleBitPerPixel;
			gr.Clear(Color.White);
			lineNr = 0;
			colNr = 0;

			_logger.Debug(TAG, nameof(FaxEndless), $"lines={lineNr} faxHeight={faxHeight}");

			Font textFont = new Font("Consolas", 26);
			SolidBrush brush = new SolidBrush(Color.Black);

			foreach (char c in msg)
			{
				switch (c)
				{
					case '\r':
						colNr = 0;
						break;
					case '\n':
						if (!wrap)
						{
							lineNr++;
							lineCnt++;
						}
						wrap = false;
						break;
					default:
						if (c < 32)
						{
							DrawSpecialChar(bmpPage, c, START_X + colNr * DX + 4, START_Y + lineNr * DY, FAX_A4_WIDTH, faxHeight);
						}
						else
						{
							gr.DrawString(c.ToString(), textFont, brush, START_X + colNr * DX, START_Y + lineNr * DY);
						}
						colNr++;
						wrap = false;
						if (colNr >= CHARS_PER_LINE)
						{
							colNr = 0;
							lineNr++;
							wrap = true;
						}
						break;
				}
			}

			List<Bitmap> bmpPages = new List<Bitmap>();
			bmpPages.Add(bmpPage);

			for (int p = 0; p < bmpPages.Count; p++)
			{
				FaxA4AddHeaderAndEdgePrint(bmpPages[p], p + 1, bmpPages.Count, faxId, isMiniTelex, bmpPages[p].Height,
						edgePrint, iTelexNumber, faxNumber, lng, now);
			}

			return bmpPages;
		}

		private List<Bitmap> FaxA4(string msg, int faxId, bool isMiniTelex, bool edgePrint, string iTelexNumber,
				string faxNumber, string lng, DateTime now, out int lineCnt)
		{
			const int LINES_PER_PAGE = 52;
			const int START_X = 210;
			const int START_Y = 130;
			const int DX = 19;
			const int DY = 42;

			List<Bitmap> bmpPages = new List<Bitmap>();
			Bitmap bmpPage = null;
			Graphics gr = null;
			int lineNr = LINES_PER_PAGE + 1;
			int colNr = 0;
			bool wrap = false;

			Font textFont = new Font("Consolas", 28);
			SolidBrush brush = new SolidBrush(Color.Black);

			lineCnt = 0;
			foreach (char c in msg)
			{
				if (lineNr >= LINES_PER_PAGE)
				{
					if (bmpPage != null) bmpPages.Add(bmpPage);
					bmpPage = new Bitmap(FAX_A4_WIDTH, FAX_A4_HIGHT);
					gr = Graphics.FromImage(bmpPage);
					gr.SmoothingMode = SmoothingMode.None;
					gr.TextRenderingHint = TextRenderingHint.SingleBitPerPixel;
					gr.Clear(Color.White);
					lineNr = 0;
					colNr = 0;
				}

				switch (c)
				{
					case '\r':
						colNr = 0;
						break;
					case '\n':
						if (!wrap)
						{
							lineNr++;
							lineCnt++;
						}
						wrap = false;
						break;
					default:
						if (c < 32)
						{
							DrawSpecialChar(bmpPage, c, START_X + colNr * DX, START_Y + lineNr * DY, FAX_A4_WIDTH, FAX_A4_HIGHT);
						}
						else
						{
							gr.DrawString(c.ToString(), textFont, brush, START_X + colNr * DX, START_Y + lineNr * DY);
						}
						colNr++;
						wrap = false;
						if (colNr >= CHARS_PER_LINE)
						{
							colNr = 0;
							lineNr++;
							wrap = true;
						}
						break;
				}

			}
			if (lineNr > 0) bmpPages.Add(bmpPage);

			for (int p = 0; p < bmpPages.Count; p++)
			{
				FaxA4AddHeaderAndEdgePrint(bmpPages[p], p + 1, bmpPages.Count, faxId, isMiniTelex, bmpPage.Height, edgePrint,
						iTelexNumber, faxNumber, lng, now);
				bmpPages[p].Save($"faxpage{p + 1}.png", ImageFormat.Png);
			}

			return bmpPages;
		}

		private void FaxA4AddHeaderAndEdgePrint(Bitmap bmpPage, int pageNr, int pageCnt, int faxId, bool isMiniTelex,
				int faxHeight, bool edgePrint, string iTelexNumber, string faxNumber, string lng, DateTime dt)
		{
			Graphics gr = Graphics.FromImage(bmpPage);
			gr.SmoothingMode = SmoothingMode.None;
			gr.TextRenderingHint = TextRenderingHint.SingleBitPerPixel;

			Font headerFont = new Font("Consolas", 24);
			SolidBrush headerBrush = new SolidBrush(Color.Black);
			string typeStr = isMiniTelex ? "Minitelex" : "Telex";
			string header;
			if (lng == "de")
			{
				header = $"{dt:dd.MM.yyyy} {dt:HH:mm} #{faxId:D05} {typeStr} von {iTelexNumber} an {faxNumber} Seite {pageNr} / {pageCnt}";
			}
			else
			{
				header = $"{dt:yyyy-MM-dd} {dt:HH:mm} #{faxId:D05} {typeStr} from {iTelexNumber} to {faxNumber} page {pageNr} / {pageCnt}";
			}

			string leftPad = new string(' ', (84 - header.Length) / 2);
			gr.DrawString(leftPad + header, headerFont, headerBrush, 100, 30);

			if (edgePrint)
			{
				int partLen = 475;
				int partCnt = faxHeight / partLen + 3;
				for (int i = 0; i < partCnt; i++)
				{
					//float x = -400F + 430F + 100 - (430F + 45F) * i;
					float x = 130F - 475F * i;
					DrawString(gr, "TELEX", x, -5F+30F, -90);
				}
				for (int i = 0; i < partCnt; i++)
				{
					float x = -360F + 475F * i;
					DrawString(gr, "TELEX", x, -1735F+30F, 90);
				}
			}

			/*
			if (edgePrint)
			{
				for (float x = -400F + 430F + 100; x > -3000F; x -= 430F + 45F)
				{
					DrawString(gr, "TELEX", x, -5F, -90);
				}

				for (float x = 50F - 430F + 20F; x < 3000F; x += 430F + 45F)
				{
					DrawString(gr, "TELEX", x, -1735F, 90);
				}
			}
			*/
		}

		public static void DrawString(Graphics gr, string text, float x, float y, int rotation)
		{
			gr.RotateTransform(rotation);
			Font borderFont = new Font("Arial Black", 40, FontStyle.Italic);
			SolidBrush drawBrush = new SolidBrush(Color.Black);
			gr.DrawString(text, borderFont, drawBrush, x, y);

			Pen pen = new Pen(Color.Black, 2);
			gr.FillPolygon(drawBrush, new PointF[] {
				new PointF(x+223,y+21),
				new PointF(x+446+45,y+21),
				new PointF(x+446-10+45,y+59),
				new PointF(x+223-10,y+59),
				new PointF(x+223,y+21) });

			gr.RotateTransform(-rotation);
		}

		private void DrawSpecialChar(Bitmap bmpPage, char asciiCode, int posX, int posY, int maxX, int maxY)
		{
			string[] data;
			switch(asciiCode)
			{
				case CodeManager.ASC_BEL:
					data = BELL;
					break;
				case CodeManager.ASC_WRU:
					data = WRU;
					break;
				case CodeManager.ASC_SHIFTF:
					data = SHIFT_F;
					break;
				case CodeManager.ASC_SHIFTG:
					data = SHIFT_G;
					break;
				case CodeManager.ASC_SHIFTH:
					data = SHIFT_H;
					break;
				default:
					return;
			}

			for (int y=0; y< data.Length; y++)
			{
				for (int x=0; x < data[y].Length; x++)
				{
					if (data[y][x] != ' ')
					{
						if (posX + x < maxX && posY + y < maxY)
						{
							bmpPage.SetPixel(posX + x, posY + y, Color.Black);
						}
					}
				}
			}
		}

		private string[] BELL = new string[]
		{
		// 22 x 42
		//	"1234567890123456789012"
			"",
			"",
			"",
			"",
			"",
			"",
			"",
			"",
			"",
			"",
			"",
			"",
			"",
			"",
			"",
			"",
			"        ****** ",
			"      **********",
			"     ***      ***",
			"    **          **",
			"    **          **",
			"   **            **",
			"   **            **",
			"   **            **",
			"  **              **",
			"  **              **",
			"  ******************",
			"  ******************",
			"       **    **",
			"       **    **",
			"       **    **",
			"       **    **",
			"   ******    ******",
			"   ******    ******",
		};

		private string[] WRU =
		{
		//	"1234567890123456789012"
			"",
			"",
			"",
			"",
			"",
			"",
			"",
			"",
			"",
			"",
			"",
			"",
			"      **********",
			"        ******",
			"         ****",
			"          ** ",
			"          **",
			"  *       **       *",
			"  *       **       *",
			"  **      **      **",
			"  ****    **    ****",
			"  ******************",
			"  ******************",
			"  ****    **    ****",
			"  **      **      **",
			"  *       **       *",
			"  *       **       *",
			"          **",
			"          ** ",
			"         ****",
			"        ******",
			"      **********",
		};

		private string[] SHIFT_F =
		{
			"",
			"",
			"",
			"",
			"",
			"",
			"",
			"",
			"",
			"",
			"",
			"",
			"",
			"",
			"",
			"  *****************",
			"  *****************",
			"  **             **",
			"  **             **",
			"  **             **",
			"  **             **",
			"  **             **",
			"  **             **",
			"  **             **",
			"  **             **",
			"  **             **",
			"  **             **",
			"  **             **",
			"  **             **",
			"  **             **",
			"  **             **",
			"  **             **",
			"  **             **",
			"  *****************",
			"  *****************",
		};

		private string[] SHIFT_G =
		{
			"",
			"",
			"",
			"",
			"",
			"",
			"",
			"",
			"",
			"",
			"",
			"",
			"",
			"",
			"",
			"  *****************",
			"  *****************",
			"  **             **",
			"  **             **",
			"  **             **",
			"  **             **",
			"  **             **",
			"  **             **",
			"  **             **",
			"  *****************",
			"  *****************",
			"  **             **",
			"  **             **",
			"  **             **",
			"  **             **",
			"  **             **",
			"  **             **",
			"  **             **",
			"  *****************",
			"  *****************",
		};

		private string[] SHIFT_H =
		{
			"",
			"",
			"",
			"",
			"",
			"",
			"",
			"",
			"",
			"",
			"",
			"",
			"",
			"",
			"",
			"  *****************",
			"  *****************",
			"  **            ***",
			"  **           ****",
			"  **          ** **",
			"  **         **  **",
			"  **        **   **",
			"  **       **    **",
			"  **       **    **",
			"  **      **     **",
			"  **     **      **",
			"  **    **       **",
			"  **    **       **",
			"  **   **        **",
			"  **  **         **",
			"  ** **          **",
			"  ****           **",
			"  ***            **",
			"  *****************",
			"  *****************",
		};



		private byte[] ConvertToG3FaxFormat(Bitmap bmpPage)
		{
			int width = bmpPage.Width;
			int height = bmpPage.Height;

			if (width != FAX_A4_WIDTH) return null;

			List<byte> data = new List<byte>();

			// start with EOL
			int pos = AddPatternToByteArray(data, 0, EOL, true);
			int lineCnt = 0;

			for (int y = 0; y < height; y++)
			{
				FaxColor faxCol = FaxColor.White;
				int x = 0;
				int runLength = 0;
				while (x < bmpPage.Width)
				{
					Color c = bmpPage.GetPixel(x, y);
					x++;
					if (!FaxColorEquals(faxCol, c))
					{
						// pixel color changed
						string pattern = GetCodePattern(faxCol, runLength);
						pos = AddPatternToByteArray(data, pos, pattern, false);
						runLength = 1;
						faxCol = faxCol == FaxColor.White ? FaxColor.Black : FaxColor.White;
						continue;
					}
					runLength++;
				}
				if (runLength > 0)
				{
					string pattern = GetCodePattern(faxCol, runLength);
					pos = AddPatternToByteArray(data, pos, pattern, false);
				}
				// EOL (end of line)
				lineCnt++;
				pos = AddPatternToByteArray(data, pos, EOL, true);
			}

			// RTC (return to command) = 6 x EOL, first is aligned
			for (int i = 0; i < 6; i++)
			{
				pos = AddPatternToByteArray(data, pos, EOL, i == 0);
			}

			return data.ToArray();
		}

		private string GetCodePattern(FaxColor faxCol, int runLength)
		{
			Code[] codes = faxCol == FaxColor.White ? WhiteCodes : BlackCodes;

			if (runLength >= MAX_RUNLENGTH) runLength = MAX_RUNLENGTH; // correct invalid runlength

			string pattern = "";
			if (runLength >= 64)
			{
				int idx = runLength / 64 + 63;
				pattern += codes[idx].Pattern;
				runLength &= 0x3F;
			}
			pattern += codes[runLength].Pattern;
			return pattern;
		}

		private int AddPatternToByteArray(List<byte> data, int pos, string pattern, bool rightAlign)
		{
			if (string.IsNullOrEmpty(pattern)) return pos;

			// left fill pattern for whole bytes
			if (rightAlign)
			{
				int algn = (pos + pattern.Length) % 8;
				if (algn != 0)
				{
					pattern = new string('0', 8 - algn) + pattern;
				}
				else
				{
					pattern = new string('0', 8) + pattern;
				}
			}

			for (int i=0; i<pattern.Length; i++)
			{
				int bitPos = pos % 8;

				if (bitPos == 0)
				{
					data.Add((byte)0);
				}
				if (pattern[i] == '1')
				{
					data[data.Count - 1] |= (byte)(1 << (bitPos)); // LSB first
				}
				pos++;
			}
			return pos;
		}

		public static bool FaxColorEquals(FaxColor faxCol, Color col)
		{
			return GetFaxColor(faxCol).ToArgb().Equals(col.ToArgb());
		}

		public static bool ColorEquals(Color col1, Color col2)
		{
			return col1.ToArgb().Equals(col2.ToArgb());
		}

		private static Color GetFaxColor(FaxColor faxCol)
		{
			return faxCol == FaxColor.White ? Color.White : Color.Black;
		}

		private static string GetFaxColorStr(FaxColor col)
		{
			return col == FaxColor.White ? "W" : "B";
		}

		private const int MAX_CODE_LEN = 13;

		private const int MAX_RUNLENGTH = 2560;

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
			new Code(1728, "010011011"),
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
			new Code(64, "0000001111"),
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
		//private const int EOL_CODE = 10000;
		//private const int CODE_NOT_FOUND = -1;
		//private byte[] RTC = new byte[] { 0x00, 0x08, 0x80, 0x00, 0x08, 0x80, 0x00, 0x08, 0x80 };
	}

	public class Code
	{
		public int RunLength { get; set; }

		public string Pattern { get; set; }

		public bool IsTerminating => RunLength < 64;

		public Code(int runLength, string pattern)
		{
			RunLength = runLength;
			Pattern = pattern;
		}

		public override string ToString()
		{
			return $"{RunLength} {Pattern}";
		}
	}
}

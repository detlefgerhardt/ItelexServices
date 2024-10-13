using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO.Ports;
using iText.Commons.Bouncycastle;
using System.Drawing.Drawing2D;
using Org.BouncyCastle.Utilities;
using System.Drawing.Text;

namespace ItelexMsgServer.Serial
{
	public class FaxConversion
	{
#if FALSE
		public static void CreateFaxImage()
		{
			//CreateFaxImageText();
		}

		public static Bitmap CreateFaxImage1()
		{
			const int FAX_WIDTH = 1728;
			const int FAX_HIGHT = 2394;

			Bitmap bmp = new Bitmap(FAX_WIDTH, FAX_HIGHT);
			Graphics gr = Graphics.FromImage(bmp);
			gr.Clear(Color.White);

			Pen pen = new Pen(Color.Black, 2);
			//gr.DrawRectangle(pen, 10, 10, FAX_WIDTH - 20, FAX_HIGHT - 20);
			//gr.DrawLine(pen, 0, 0, FAX_WIDTH - 1, FAX_WIDTH - 1);

			for (int i=0; i<FAX_WIDTH-1; i++)
			{
				bmp.SetPixel(i, i, Color.Black);
			}

			bmp.Save("CreateFaxImage1.png", ImageFormat.Png);
			return bmp;

		}


		public static Bitmap CreateFaxImageText(int pageNr, int pageCnt)
		{
			const int FAX_WIDTH = 1728;
			const int FAX_HIGHT = 2394;

			Bitmap bmp = new Bitmap(FAX_WIDTH, FAX_HIGHT);
			Graphics gr = Graphics.FromImage(bmp);
			gr.SmoothingMode = SmoothingMode.None;
			gr.TextRenderingHint = TextRenderingHint.SingleBitPerPixel;
			//gr.Graphics.TextRenderingHint = TextRenderingHint.AntiAlias;



			gr.Clear(Color.White);

			//DrawString(gr, "TELEX", 1000F, 1000F, FAX_WIDTH, FAX_HIGHT, 0);

			for (float x = -400F + 430F + 100; x > -3000F; x -= 430F + 45F)
			{
				DrawString(gr, "TELEX", x, -5F, FAX_WIDTH, FAX_HIGHT, -90);
			}

			for (float x = 50F - 430F + 20F; x < 3000F; x += 430F + 45F)
			{
				DrawString(gr, "TELEX", x, -1735F, FAX_WIDTH, FAX_HIGHT, 90);
			}
			//DrawString(gr, "TELEX", 50F, -1735F, FAX_WIDTH, FAX_HIGHT, 90);

			Font textFont1 = new Font("Consolas", 24);
			Font textFont2 = new Font("Consolas", 28);

			SolidBrush drawBrush = new SolidBrush(Color.Black);
			gr.DrawString($"Minitelex von 7822222 an 06426 921125  Seite {pageNr} / {pageCnt}", textFont1, drawBrush, 360, 20);


			int ln = 42;
			gr.DrawString("         1         2         3         4         5         6",
					textFont2, drawBrush, 150, 100);
			gr.DrawString("12345678901234567890123456789012345678901234567890123456789012345678",
					textFont2, drawBrush, 150, 100 + ln);
			for (int y = 0; y < 50; y++)
			{
				gr.DrawString($"{y + 3:D02}", textFont2, drawBrush, 150, 100 + (y + 2) * ln);
			}

			//bmp.Save("CreateFaxImageText.png", ImageFormat.Png);
			return bmp;
		}

		private static void DrawString(Graphics gr, string text, float x, float y, float width, float height, int rotation)
		{
			//gr.TranslateTransform(width / 2, height / 2); 
			gr.RotateTransform(rotation);
			Font borderFont = new Font("Arial Black", 40, FontStyle.Italic);
			SolidBrush drawBrush = new SolidBrush(Color.Black);
			StringFormat drawFormat = new StringFormat();
			//SdrawFormat.FormatFlags = StringFormatFlags.DirectionVertical;
			gr.DrawString(text, borderFont, drawBrush, x, y);

			Pen pen = new Pen(Color.Black, 2);
			gr.FillPolygon(drawBrush, new PointF[] {
				new PointF(x+223,y+21),
				new PointF(x+446+45,y+21),
				new PointF(x+446-10+45,y+59),
				new PointF(x+223-10,y+59),
				new PointF(x+223,y+21) });

			gr.RotateTransform(-rotation);
			//gr.TranslateTransform(width, height);
		}

		private static void ConvHexToBin()
		{
			string[] lines = File.ReadAllLines("ccitt_hex.txt");
			string hexStr = String.Join(" ", lines);
			string[] hexParts = hexStr.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			List<byte> bytes = new List<byte>();
			foreach (string h in hexParts)
			{
				bytes.Add((byte)Convert.ToInt32(h, 16));
			}
			File.WriteAllBytes("ccitt_hex.bin", bytes.ToArray());
		}
#endif


	}
}

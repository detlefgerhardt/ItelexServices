using ItelexCommon;
using ItelexCommon.Logger;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ItelexMailGate
{
	class PdfCreator
	{
		private const string TAG = nameof(PdfCreator);

		private Logging _logger;

		public PdfCreator()
		{
			_logger = LogManager.Instance.Logger;
		}

		public bool Create(string fileName, string msg)
		{
			try
			{
				string fullName = Path.Combine(Constants.FAX_PATH, fileName);
				FileStream fs = new FileStream(fullName, FileMode.Create, FileAccess.Write, FileShare.None);

				Document pdfDoc = new Document(PageSize.A4, 30, 30, 25, 25);
				pdfDoc.AddTitle(fileName);
				pdfDoc.AddCreator(Constants.PROGRAM_NAME);
				pdfDoc.AddHeader("Date", DateTime.Now.ToString());

				PdfWriter writer = PdfWriter.GetInstance(pdfDoc, fs);
				pdfDoc.Open();

				BaseFont baseFont = BaseFont.CreateFont("c:/windows/fonts/consola.ttf", BaseFont.WINANSI, true);
				//BaseFont baseFont = BaseFont.CreateFont("c:/windows/fonts/courbd.ttf", BaseFont.WINANSI, true);
				Font stdFont = new Font(baseFont, 12, Font.NORMAL);

				Paragraph para = new Paragraph();
				para.SetLeading(1, 1); // Zeilenabstand
				para.Font = stdFont;
				para.Add(msg);
				pdfDoc.Add(para);

				Image faxLinks = Image.GetInstance(@".\fax_links.jpg");
				faxLinks.ScalePercent(10);
				faxLinks.SetAbsolutePosition(0, 0);
				pdfDoc.Add(faxLinks);

				Image faxRechts = Image.GetInstance(@".\fax_rechts.jpg");
				faxRechts.ScalePercent(10);
				faxRechts.SetAbsolutePosition(580, 0);
				pdfDoc.Add(faxRechts);

				pdfDoc.Close();
				writer.Close();
				fs.Close();
				return true;
			}
			catch(Exception ex)
			{
				_logger.Error(TAG, nameof(Create), "error", ex);
				return false;
			}
		}
	}
}

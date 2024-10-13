using ItelexCommon;
using ItelexCommon.Logger;
using iText.IO.Font;
using iText.IO.Image;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Image = iText.Layout.Element.Image;

namespace ItelexMsgServer.Fax
{
	class PdfCreator2
	{
		private const string TAG = nameof(PdfCreator2);

		private Logging _logger;

		public PdfCreator2()
		{
			_logger = LogManager.Instance.Logger;
		}

		public bool Create(string fileName, string msg)
		{
			try
			{
				string fullName = Path.Combine(Constants.FAX_PATH, fileName);

				MemoryStream baos = new MemoryStream();
				//WriterProperties wp = new WriterProperties();
				PdfWriter writer = new PdfWriter(baos);
				writer.SetCloseStream(false); // important

				PdfDocument pdfDoc = new PdfDocument(writer.SetSmartMode(true));
				Document document = new Document(pdfDoc, iText.Kernel.Geom.PageSize.A4);
				document.SetMargins(30, 30, 20, 60);

				FontProgram fontProgram = FontProgramFactory.CreateFont(@"c:/windows/fonts/consola.ttf");
				PdfFont consola = PdfFontFactory.CreateFont(fontProgram, PdfEncodings.UTF8);

				Paragraph para = new Paragraph();
				para.SetFixedLeading(13);  // Zeilenabstand
				para.SetFont(consola);

				para.Add(msg).SetFont(consola);
				document.Add(para);

				/*
				ImageData imageData = ImageDataFactory.Create(@".\fax_links.jpg");
				Image image = new Image(imageData).Scale(0.10F, 0.10F).SetFixedPosition(1, 0, 0);
				document.Add(image);

				ImageData imageData2 = ImageDataFactory.Create(@".\fax_links.jpg");
				Image image2 = new Image(imageData2).Scale(0.10F, 0.10F).SetFixedPosition(1, 580, 0);
				document.Add(image2);
				*/

				document.Close();

				FileStream file = new FileStream(fullName, FileMode.Create, FileAccess.Write);
				baos.WriteTo(file);
				file.Close();
				_logger.Notice(TAG, nameof(Create), $"{fullName} created");
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

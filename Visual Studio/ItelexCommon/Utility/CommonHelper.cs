using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.Net;
using System.Collections.Specialized;
using ItelexCommon.Logger;
using MimeKit.Text;
using MimeKit;
using System.Text.RegularExpressions;
using MailKit.Security;
using MailKit.Net.Smtp;

namespace ItelexCommon.Utility
{
	public static class CommonHelper
	{
		private const string TAG = nameof(CommonHelper);

		private static ShiftStates _debugShiftState = ShiftStates.Unknown;

		public static string FormatVersionRelease(string prgmName, string prgmVersion, string buildTime)
		{
			// show only date in release version
			buildTime = buildTime.Trim(new char[] { '\n', '\r' });
			//buildTime = buildTime.Substring(0, 10);
			return $"{prgmName}  V{prgmVersion}  (Build={buildTime})";
		}

		public static string FormatVersionDebug(string prgmName, string prgmVersion, string buildTime, bool verbose = true)
		{
			buildTime = buildTime.Trim(new char[] { '\n', '\r' }) + " Debug";
			if (verbose)
			{
				// show date and time in debug version
				return $"{prgmName}  V{prgmVersion}  (Build={buildTime})";
			}
			else
			{
				//buildTime = buildTime.Substring(0, 10);
				return $"{prgmName}  V{prgmVersion} ({buildTime} Debug)";
			}
		}



		public static string GetDebugBaudotSend(byte[] data)
		{
			string text = CodeManager.BaudotDataToAscii(data, ref _debugShiftState);
			return CodeManager.AsciiToDebugStr(text);
		}

		public static string GetDebugBaudotRecv(byte[] data)
		{
			string text = CodeManager.BaudotDataToAscii(data, ref _debugShiftState);
			return CodeManager.AsciiToDebugStr(text);
		}

		public static void DumpByteArray(byte[] buffer, int pos, int len = -1)
		{
			List<string> lines = DumpByteArrayStr(buffer, pos, len);
			for (int i = 0; i < lines.Count; i++)
			{
				Debug.WriteLine($"{i + 1:D02} {lines[i]}");
			}
		}

		public static List<string> DumpByteArrayStr(byte[] buffer, int pos, int len = -1)
		{
			if (len == -1)
			{
				len = buffer.Length;
			}
			else if (pos + len > buffer.Length)
			{
				len = buffer.Length - pos;
			}

			List<string> list = new List<string>();
			int p = 0;
			while (p < len)
			{
				string l1 = "";
				string l2 = "";
				string line = $"{p:X3}: ";
				for (int x = 0; x < 8; x++)
				{
					if (!string.IsNullOrEmpty(l1))
						l1 += " ";
					if (p >= len)
					{
						l1 += "  ";
						continue;
					}
					else
					{
						byte b = buffer[pos + p];
						l1 += b.ToString("X2");
						l2 += b >= 32 && b < 127 ? (char)b : '.';
					}
					p++;
				}
				line += l1 + " " + l2;
				list.Add(line);
			}
			return list;
		}

		public static byte[] AddByte(byte[] arr, byte addByte)
		{
			if (arr == null)
			{
				return null;
			}
			byte[] newArr = new byte[arr.Length + 1];
			Buffer.BlockCopy(arr, 0, newArr, 0, arr.Length);
			newArr[arr.Length] = addByte;
			return newArr;
		}

		public static byte[] AddBytes(byte[] arr, byte[] addArr)
		{
			if (arr==null)
			{
				return null;
			}
			if (addArr==null)
			{
				return arr;
			}
			byte[] newArr = new byte[arr.Length + addArr.Length];
			Buffer.BlockCopy(arr, 0, newArr, 0, arr.Length);
			Buffer.BlockCopy(addArr, 0, newArr, arr.Length, addArr.Length);
			return newArr;
		}

		public static byte[] StringToByteArr(string str)
		{
			if (string.IsNullOrEmpty(str))
				return new byte[0];
			return Encoding.ASCII.GetBytes(str);
		}

		public static int? ToInt(string dataStr)
		{
			int value;
			if (int.TryParse(dataStr, out value))
				return value;
			else
				return null;
		}

		public static int? GetIntValueFromString(string str, int pos)
		{
			string valStr = "";
			while (true)
			{
				if (pos >= str.Length) break;
				if (!char.IsDigit(str[pos])) break;
				valStr += str[pos];
				pos++;
			}
			if (valStr.Length == 0) return null;

			if (int.TryParse(valStr, out int val))
			{
				return val;
			}
			return null;
		}

		/// <summary>
		/// Serialize as formatted xml with line breaks
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="objectToSerialize"></param>
		/// <returns></returns>
		public static string SerializeObject<T>(T objectToSerialize)
		{
			XmlSerializer xmlserializer = new XmlSerializer(typeof(T));
			XmlWriterSettings writerSettings = new XmlWriterSettings
			{
				// Formatierung des XML -- Zeilenumbrüche
				Indent = true,
				Encoding = Encoding.UTF8
			};

			string xml;
			using (StringWriterUtf8 stringWriter = new StringWriterUtf8())
			{
				using (var xmlWriter = XmlWriter.Create(stringWriter, writerSettings))
				{
					xmlserializer.Serialize(xmlWriter, objectToSerialize);
					xml = stringWriter.ToString();
				}
			}
			return xml;
		}

		/// <summary>
		/// Deserialize formatted xml with line breaks
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="xmlString"></param>
		/// <returns></returns>
		public static T DeserializeObject<T>(string xmlString)
		{
			XmlSerializer serializer = new XmlSerializer(typeof(T));

			using (Stream stream = new MemoryStream())
			{
				using (var reader = new StreamReader(stream))
				{
					byte[] data = Encoding.UTF8.GetBytes(xmlString);
					stream.Write(data, 0, data.Length);
					stream.Position = 0;
					return (T)serializer.Deserialize(reader);
				}
			}
		}

		public static string SerializeObject_DataContract<T>(T objectToSerialize)
		{
			using (var memoryStream = new MemoryStream())
			{
				using (var reader = new StreamReader(memoryStream))
				{
					DataContractSerializer serializer = new DataContractSerializer(typeof(T));
					serializer.WriteObject(memoryStream, objectToSerialize);
					memoryStream.Position = 0;
					string readToEnd = reader.ReadToEnd();
					return readToEnd;
				}
			}
		}

		public static T DeserializeObject_DataContract<T>(string xml)
		{
			using (Stream stream = new MemoryStream())
			{
				byte[] data = System.Text.Encoding.UTF8.GetBytes(xml);
				stream.Write(data, 0, data.Length);
				stream.Position = 0;
				DataContractSerializer deserializer = new DataContractSerializer(typeof(T));
				return (T)deserializer.ReadObject(stream);
			}
		}

		public static string StripCrLf(string text)
		{
			if (string.IsNullOrEmpty(text)) return text;
			return text.Trim(new char[] { CodeManager.ASC_CR, CodeManager.ASC_LF });
		}

		private static Random _random = new Random();

		public static string CreatePin()
		{
			while (true)
			{
				string pinStr = _random.Next(1001, 9988).ToString();
				int[] digitCnt = new int[10];
				bool max2 = true;
				for (int i = 0; i < 4; i++)
				{
					int digit = ((int)pinStr[i]) - 48;
					digitCnt[digit]++;
					if (digitCnt[digit] > 2) max2 = false;
				}
				if (max2) return pinStr;
			}
		}

		public static bool IsValidPin(string pin)
		{
			if (!int.TryParse(pin, out int value)) return false; // no valid number
			return value >= 1000 && value <= 9999;
		}

#if false

		// Not working any more

		public static bool SendMailUnsecure(string to, string subj, string body)
		{
			if (string.IsNullOrWhiteSpace(to)) return false;

			const string hostname = "smtp.1und1.de";
			const int port = 587;
			const string username = "telex@telexgate.de";
			const string password = PrivateConstants.MSGSRV_MAILPASSWORD;

			MailMessage message = new MailMessage(username, to);
			message.Subject = subj;
			message.Body = body;
			System.Net.Mail.SmtpClient client = new System.Net.Mail.SmtpClient(hostname);
			client.Credentials = new System.Net.NetworkCredential(username, password);

			try
			{
				client.Send(message);
				Logging.Instance.Info(TAG, nameof(SendMailUnsecure), $"sent mail to {to} at {hostname}");
				return true;
			}
			catch (Exception ex)
			{
				Logging.Instance.Error(TAG, nameof(SendMailUnsecure), $"error sending mail to {to} at {hostname}", ex);
				return false;
			}
		}
#endif

		public static bool SendMail(string subj, string msg)
		{
			const string SMTP_SERVER = "smtp.1und1.de";
			const int SMTP_PORT = 587;
			const string SMTP_USERNAME = "telex@telexgate.de";
			const string SMTP_PASSWORD = PrivateConstants.TELEX_TELEXGATE_PASSWORD;
			const string FROM = "telex@telexgate.de";
			const string TO = PrivateConstants.DEBUG_EMAIL_ADDRESS;

			string body = string.IsNullOrWhiteSpace(msg) ? "" : msg;
			try
			{
				MimeMessage message = new MimeMessage();
				message.From.Add(MailboxAddress.Parse(FROM));
				message.To.Add(MailboxAddress.Parse(TO));
				message.Subject = subj;
				message.Body = new TextPart(TextFormat.Plain) { Text = body != null ? body : "" };

				SmtpClient client = null;
				try
				{
					client = new SmtpClient();
					client.Connect(SMTP_SERVER, SMTP_PORT, SecureSocketOptions.StartTls);
					client.Authenticate(SMTP_USERNAME, SMTP_PASSWORD);
					client.Send(message);
					client.Disconnect(true);
				}
				catch (Exception ex)
				{
					LogManager.Instance.Logger.Error(TAG, nameof(SendMail), "error connecting to SMTP server", ex);
					if (client != null) client.Disconnect(false);
					return false;
				}
				return true;
			}
			catch (Exception ex)
			{
				LogManager.Instance.Logger.Error(TAG, nameof(SendMail), $"error sending mail to {TO}", ex);
				return false;
			}
		}

		public static bool SendWebMail(string subject, string message, string to=null)
		{
			try
			{
				string from = "telex@telexgate.de";
				string uri = "http://www.telexgate.de/send.php";

				if (to == null) to = PrivateConstants.DEBUG_EMAIL_ADDRESS;
				if (message == null) message = subject;

				WebClient client = new WebClient();
				System.Collections.Specialized.NameValueCollection reqparm =
						new System.Collections.Specialized.NameValueCollection();
				reqparm.Add("xpw", "Ajszu%437.68D_vsv");
				reqparm.Add("xto", to);
				reqparm.Add("xfrom", from);
				reqparm.Add("xsubject", subject);
				reqparm.Add("xmessage", message);
				//client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
				byte[] responsebytes = client.UploadValues(uri, "POST", reqparm);

				LogManager.Instance.Logger.Notice(TAG, nameof(SendWebMail), $"send mail from {from} at {uri}");
				string response = Encoding.UTF8.GetString(responsebytes);
				if (response == "0")
				{
					LogManager.Instance.Logger.Notice(TAG, nameof(SendWebMail), $"mail sent, response='{response}'");
				}
				else
				{
					LogManager.Instance.Logger.Warn(TAG, nameof(SendWebMail), $"error send mail, response='{response}'");
				}
				return true;
			}
			catch (Exception ex)
			{
				LogManager.Instance.Logger.Error(TAG, nameof(SendWebMail), "error", ex);
				return false;
			}
		}

		public static string CleanupFilename(string filename)
		{
			filename = filename.Replace("\r", "");
			filename = filename.Replace("\n", "");
			filename = filename.Replace("/", "");
			filename = filename.Replace(":", "");
			filename = filename.Trim();
			return filename;
		}
	}

	public class StringWriterUtf8 : StringWriter
	{
		public override Encoding Encoding
		{
			get
			{
				return Encoding.UTF8;
			}
		}
	}
}

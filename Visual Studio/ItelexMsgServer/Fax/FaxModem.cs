using ItelexCommon;
using ItelexCommon.Logger;
using ItelexCommon.Utility;
using ItelexMsgServer.Fax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Text;
using System.Threading;

namespace ItelexMsgServer.Serial
{
	public class FaxModem
	{
		//private readonly byte[] RTC = new byte[] { 0x00, 0x08, 0x80, 0x00, 0x08, 0x80, 0x00, 0x08, 0x80 };

		const string TagList = nameof(FaxModem);

		private const byte DLE = 0x10;
		private const byte ETX = 0x03;

		protected Logging _logger;

		private SerialPort _serialPort;

		/*
		public void SendFax2(byte[] data)
		{
			//string filename = "image.g3";
			//byte[] data = File.ReadAllBytes(filename);
			//data = FaxConversion.MirrorBits(data); ;
			//File.WriteAllBytes("image_inv.g3", data);
			//return;

			//ReadFaxFile rff = new ReadFaxFile();
			//rff.Read();
			//return;

			//FaxConversion.CreateFaxImage();
			//return;


			//SerialPortFixer.Execute("COM3");

			//SendFax();
		}
		*/

		public FaxModem()
		{
			_logger = LogManager.Instance.Logger;
		}

		public FaxModemResponse SendClass2Fax(List<byte[]> data, string ownNumber, string receiver, FaxFormat faxFormat)
		{
			_logger.Info(TagList, nameof(SendClass2Fax), $"receiver={receiver}");


			try
			{
				_serialPort = new SerialPort("COM3");
				_serialPort.BaudRate = 19200;
				_serialPort.DataBits = 8;
				_serialPort.Parity = Parity.None;
				_serialPort.StopBits = StopBits.One;
				_serialPort.Handshake = Handshake.RequestToSend;
				//_serialPort.RtsEnable = false;
				//_serialPort.DtrEnable = true;

				// Set the read/write timeouts
				//_serialPort.ReadTimeout = 2000;
				//_serialPort.WriteTimeout = 2000;
				_serialPort.Open();
			}
			catch (Exception ex)
			{
				_logger.Error(TagList, nameof(SendClass2Fax), "", ex);
				return FaxModemResponse.CommError;
			}

			try
			{
				_serialPort.DiscardInBuffer();

				Debug.WriteLine("--------------------------------------");
				bool success = SendCmd("at\r");
				//success = SendCmd("ate0v1\r");
				success = SendCmd("at&fs0=0e0v1q0\r");
				success = SendCmd("ats7=60&d3&k4\r");
				success = SendCmd("atx4m2l4\r");
				success = SendCmd("at+fclass=2\r");
				//success = SendCmd("at+fmfr?\r"); // hersteller
				//success = SendCmd("at+fmdl?\r"); // modell
				success = SendCmd($"at+flid=\"{ownNumber}\"\r", null, 10000);
				success = SendCmd("at+fbug=0\r");
				success = SendCmd("at+fdis?\r");
				success = SendCmd("at+fdcc=1\r");
				success = SendCmd("at+fbor=0\r"); // bit order
												  //success = SendCmd($"atdt{receiver}\r", null, 30000);
				FaxModemResponse modemResp = SendCmdDial(receiver);
				_logger.Notice(TagList, nameof(SendClass2Fax), $"dial modemResp={modemResp}");
				if (modemResp != FaxModemResponse.SendOk)
				{
					Hangup();
					return modemResp;
				}

				int formatInt = faxFormat == FaxFormat.A4 ? 0 : 2;
				success = SendCmd($"at+fdis=1,5,0,{formatInt},0,0,0,0\r");
				if (!success)
				success = SendCmd("at+fdcs?\r");

				for (int i = 0; i < data.Count; i++)
				{
					success = SendCmd("at+fdt\r", "connect\r\n", 20000);
					SendFaxImage(data[i]);
					success = SendCmd("", null, 30000); // wait for ok
					success = SendCmdFet(i < data.Count - 1 ? 0 : 2, out int? fpts, out int? fhng);
					if (fpts != 1 || fhng != 0)
					{
						Hangup();
						_logger.Notice(TagList, nameof(SendClass2Fax), "class2 error");
						return FaxModemResponse.Class2Error;
					}
				}

				success = SendCmd("ath0\r", "no carrier\r\n", 20000);
				success = SendCmd("at+fclass=0\r", null, 5000);
				//success = SendCmd("at\r");

				_logger.Info(TagList, nameof(SendClass2Fax), "ok");
				return FaxModemResponse.SendOk;
			}
			catch(Exception ex)
			{
				_logger.Error(TagList, nameof(SendClass2Fax), "", ex);
				return FaxModemResponse.Error;
			}
			finally
			{
				_serialPort.Close();
			}
		}

		private void Hangup()
		{
			Thread.Sleep(1500);
			SendCmd("+++");
			Thread.Sleep(1500);
			SendCmd("ath0\r", null, 10000);
		}

		private void SendFaxImage(byte[] data)
		{
			for (int i = 0; i < data.Length; i++)
			{
				if (data[i] == DLE)
				{
					// double DLE
					_serialPort.Write(data, i, 1);
				}
				_serialPort.Write(data, i, 1);
			}

			data = new byte[] { DLE, ETX };
			_serialPort.Write(data, 0, data.Length);

			Thread.Sleep(1000);
		}

		private FaxModemResponse SendCmdDial(string number)
		{
			//number = "06426242320";

			string cmd = $"atdt{number}\r";
			bool success = SendCmd(cmd, new string[] { "ok", "no carrier", "busy", "no dialtone" }, 30000, out string respStr);
			if (respStr.ToLower().Contains("no carrier")) return FaxModemResponse.Error;
			if (respStr.ToLower().Contains("no dialtone")) return FaxModemResponse.NoDialtone;
			if (respStr.ToLower().Contains("busy")) return FaxModemResponse.Busy;
			if (respStr.ToLower().Contains("ok")) return FaxModemResponse.SendOk;

			return FaxModemResponse.SendOk;
		}

		private bool SendCmdFet(int fetPrm, out int? fpts, out int? fhng)
		{
			//success = SendCmd($"at+fet={fetPrm}\r", null, 20000); // keine weiteren seiten
			//rsp: '\r\n+FPTS:1\r\n\r\n+FHNG:00\r\n\r\nOK\r\n'(5367)

			fpts = null;
			fhng = null;

			string cmd = $"at+fet={fetPrm}\r";
			bool success = SendCmd(cmd, new string[] { "ok" }, 20000, out string respStr);
			respStr = respStr.ToLower();
			if (!respStr.Contains("ok"))
			{
				_logger.Warn(TagList, nameof(SendCmdFet), $"no ok after {cmd}");
				return false;
			}

			int pos = respStr.IndexOf("+fpts:");
			if (pos >= 0)
			{
				fpts = CommonHelper.GetIntValueFromString(respStr, pos+6);
			}
			pos = respStr.IndexOf("+fhng:");
			if (pos >= 0)
			{
				fhng = CommonHelper.GetIntValueFromString(respStr, pos + 6);
			}
			_logger.Debug(TagList, nameof(SendCmdFet), $"fpts={fpts} fhng={fhng}");
			if (fpts == null || fhng == null)
			{
				_logger.Warn(TagList, nameof(SendCmdFet), $"fpts={fpts} fhng={fhng}");
			}
			return (fpts == 1 && fhng == 0);
		}

		private bool SendCmd(string cmd, string resp = null, int waitMs = 1000)
		{
			if (resp == null) resp = "ok\r";
			return SendCmd(cmd, new string[] { resp }, waitMs, out _);
		}

		private bool SendCmd(string cmd, string[] resp, int waitMs, out string respStr)
		{
			byte[] inBuf = new byte[512];

			if (resp == null) resp = new string[] { "ok\r" };
			if (!string.IsNullOrEmpty(cmd))
			{
				_serialPort.Write(cmd);
			}

			TickTimer timer = new TickTimer();
			respStr = "";
			bool timeout = false;
			bool foundResp = false;
			while(!foundResp)
			{
				Thread.Sleep(100);
				if (timer.IsElapsedMilliseconds(waitMs))
				{
					timeout = true;
					break;
				}
				int inCnt = _serialPort.BytesToRead;
				inCnt = _serialPort.Read(inBuf, 0, inCnt);
				if (inCnt > 0)
				{
					string inStr = Encoding.UTF8.GetString(inBuf, 0, inCnt);
					respStr += inStr;
					foreach (string rs in resp)
					{
						if (respStr.ToLower().Contains(rs))
						{
							foundResp = true;
							break;
						}
					}
				}
			}
			string timeoutStr = timeout ? " timout" : "";
			_logger.Warn(TagList, nameof(SendCmd), $"cmd={DispStr(cmd)} resp: '{DispStr(respStr)}' ({timer.ElapsedMilliseconds}{timeoutStr})");
			return true;
		}

		private static string DispStr(string str)
		{
			str = str.Replace("\r", "\\r");
			str = str.Replace("\n", "\\n");
			str = str.Replace("\x11", "\\x11");
			return str;
		}
	}
}

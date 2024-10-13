using ItelexCommon.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ItelexCommon
{
	public class SubscriberServer
	{
		public static readonly string[] TLN_SERVER_ADDRS = new string[]
		{
			"tlnserv.teleprinter.net",
			"tlnserv2.teleprinter.net",
			"tlnserv3.teleprinter.net",
		};
		public const int TLN_SERVER_PORT = 11811;

		public const int TIMEOUT = 2000;

		public delegate void MessageEventHandler(string message);
		public event MessageEventHandler Message;

		private const string TAG = nameof(SubscriberServer);

		private TcpClientWithTimeout _client = null;
		private TcpClient _tcpClient = null;
		private NetworkStream _stream = null;

		public SubscriberServer()
		{
		}

		public bool Connect()
		{
			return Connect(null, null);
		}

		public bool Connect(string[] address, int? port)
		{
			if (address == null) address = TLN_SERVER_ADDRS;
			if (port == null) port = TLN_SERVER_PORT;

			for (int i = 0; i < address.Length; i++)
			{
				try
				{
					_client = new TcpClientWithTimeout(address[i], port.Value, TIMEOUT);
					_tcpClient = _client.Connect();
					_tcpClient.ReceiveTimeout = 2000;
					_stream = _tcpClient.GetStream();

					// check connection (work-around)
					PeerSearchReply reply = SendPeerSearch("check");
					if (!reply.Valid)
					{
						string errStr = $"error in subscribe server communication {address[i]}:{port}";
						Log(LogTypes.Error, nameof(Connect), errStr);
						_stream?.Close();
						_tcpClient?.Close();
						continue; // try next subscribe server
					}

					return true;
				}
				catch (Exception ex)
				{
					string errStr = $"error connecting to subscribe server {address[i]}:{port}";
					Log(LogTypes.Error, nameof(Connect), errStr, ex);
					Message?.Invoke(errStr);
				}
			}

			_stream?.Close();
			_tcpClient?.Close();
			_client = null;
			return false;
		}

		public bool Disconnect()
		{
			_stream?.Close();
			_tcpClient?.Close();
			return true;
		}

		public bool CheckNumberIsValid(int number)
		{
			return CheckNumberIsValid(null, null, number);
		}

		public bool CheckNumberIsValid(string[] addresses, int? port, int number)
		{
			SubscriberServer server = new SubscriberServer();
			server.Connect(addresses, port);
			PeerQueryReply reply = server.SendPeerQuery(number);
			server.Disconnect();
			return reply != null && reply.Valid;
		}

		public bool CheckNumberIsService(int number)
		{
			SubscriberServer server = new SubscriberServer();
			server.Connect(null, null);
			PeerQueryReply reply = server.SendPeerQuery(number);
			server.Disconnect();
			if (reply == null) return false;
			string longName = reply.Data.LongName.ToLower();
			return longName.Contains("=a");
		}


#if false
		public AsciiQueryResult PeerQueryAscii(string number)
		{
			Logging.Instance.Log(LogTypes.Debug, TAG, nameof(PeerQueryAscii), $"number='{number}'");

			if (client == null)
			{
				Logging.Instance.Error(TAG, nameof(PeerQueryAscii), "no server connection");
				return null;
			}

			if (string.IsNullOrEmpty(number))
			{
				return null;
			}
			number = number.Replace(" ", "");

			try
			{

				// query number
				byte[] data = Encoding.ASCII.GetBytes($"q{number}\r\n");
				stream.Write(data, 0, data.Length);

				data = new byte[4096];
				string responseData = string.Empty;

				Int32 bytes = stream.Read(data, 0, data.Length);
				responseData = Encoding.ASCII.GetString(data, 0, bytes);

				string[] responseList = responseData.Split(new string[] { "\r\n" }, StringSplitOptions.None);
				if (responseList.Length < 8)
					return null;
				if (responseList[0] != "ok")
					return null;
				if (responseList[7] != "+++")
					return null;

				QueryResult queryData = new QueryResult()
				{
					Number = responseList[1],
					Name = responseList[2],
					Type = GetValue(responseList[3]),
					HostName = responseList[4],
					Port = GetValue(responseList[5]),
					ExtensionNumber = GetValue(responseList[6]),
				};

				return queryData;
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
				return null;
			}
			finally
			{
				stream?.Close();
				client?.Close();
			}
		}
		private int? GetValue(string valStr)
		{
			int value;
			if (int.TryParse(valStr, out value))
			{
				return value;
			}
			return null;
		}
#endif

		/// <summary>
		/// Query for number
		/// </summary>
		/// <param name="number"></param>
		/// <returns>peer or null</returns>
		public PeerQueryReply SendPeerQuery(int number)
		{
			int[] invalidNumbers = { }; // test

			Log(LogTypes.Debug, nameof(SendPeerQuery), $"number='{number}'");

			if (invalidNumbers.Contains(number))
			{
				Log(LogTypes.Notice, nameof(SendPeerQuery), $"peer not found*");
				return new PeerQueryReply()
				{
					Error = $"peer not found {number}",
					Valid = false,
				};
			}

			PeerQueryReply reply = new PeerQueryReply();

			if (_client == null)
			{
				Log(LogTypes.Error, nameof(SendPeerQuery), "no server connection");
				reply.Error = "no server connection";
				reply.Valid = false;
				return reply;
			}

			if (number == 0)
			{
				reply.Error = "no query number";
				reply.Valid = false;
				return reply;
			}

			byte[] sendData = new byte[2 + 5];
			sendData[0] = 0x03; // Peer_query
			sendData[1] = 0x05; // length
			byte[] numData = BitConverter.GetBytes(number);
			Buffer.BlockCopy(numData, 0, sendData, 2, 4);
			sendData[6] = 0x01; ; // version 1

			try
			{
				_stream.Write(sendData, 0, sendData.Length);
			}
			catch (Exception ex)
			{
				Log(LogTypes.Error, nameof(SendPeerQuery), $"error sending data to subscribe server", ex);
				reply.Error = "reply server error";
				return reply;
			}

			byte[] recvData = new byte[102];
			int recvLen;
			try
			{
				recvLen = _stream.Read(recvData, 0, recvData.Length);
			}
			catch (Exception ex)
			{
				Log(LogTypes.Error, nameof(SendPeerQuery), $"error receiving data from subscribe server", ex);
				reply.Error = "reply server error";
				return reply;
			}

			if (recvLen == 0)
			{
				reply.Error = $"no data received";
				return reply;
			}

			if (recvData[0] == 0x04)
			{
				// peer not found
				Log(LogTypes.Notice, nameof(SendPeerQuery), $"peer not found");
				reply.Error = $"peer not found {number}";
				reply.Valid = false;
				return reply;
			}

			if (recvData[0] != 0x05)
			{
				// invalid packet
				Log(LogTypes.Error, nameof(SendPeerQuery), $"invalid packet id ({recvData[0]:X02})");
				reply.Error = $"invalid packet id ({recvData[0]:X02})";
				reply.Valid = false;
				return reply;
			}

			if (recvLen < 2 + 0x64)
			{
				Log(LogTypes.Error, nameof(SendPeerQuery), $"received data to short ({recvLen} bytes)");
				reply.Error = $"received data to short ({recvLen} bytes)";
				reply.Valid = false;
				return reply;
			}

			if (recvData[1] != 0x64)
			{
				Log(LogTypes.Error, nameof(SendPeerQuery), $"invalid length value ({recvData[1]})");
				reply.Error = $"invalid length value ({recvData[1]})";
				reply.Valid = false;
				return reply;
			}

			reply.Data = ByteArrayToPeerData(recvData, 2);

			if (!string.IsNullOrEmpty(reply.Data.HostName) && string.IsNullOrEmpty(reply.Data.IpAddress))
			{
				// get ip address from host name
				reply.Data.IpAddress = Dns.GetHostAddresses(reply.Data.HostName)?.First(addr => addr.AddressFamily == AddressFamily.InterNetwork)?.ToString();
				Log(LogTypes.Debug, nameof(SendPeerQuery), $"host {reply.Data.IpAddress} -> ipaddress {reply.Data.IpAddress}");
				// Dns.GetHostEntry(reply.Data.HostName).AddressList.First(addr => addr.AddressFamily == AddressFamily.InterNetwork);
			}

			reply.Valid = true;
			reply.Error = "ok";

			return reply;
		}

		/// <summary>
		/// Query for search string
		/// </summary>
		/// <param name="name"></param>
		/// <returns>search reply with list of peers</returns>
		public PeerSearchReply SendPeerSearch(string name)
		{
			Log(LogTypes.Debug, nameof(SendPeerSearch), $"name='{name}'");
			PeerSearchReply reply = new PeerSearchReply();

			if (_client == null)
			{
				Log(LogTypes.Error, nameof(SendPeerSearch), "no server connection");
				reply.Error = "no server connection";
				reply.Valid = false;
				return reply;
			}

			if (string.IsNullOrEmpty(name))
			{
				reply.Error = "no search name";
				reply.Valid = false;
				return reply;
			}

			byte[] sendData = new byte[43];
			sendData[0] = 0x0A; // Peer_search
			sendData[1] = 0x29; // length
			sendData[2] = 0x01; ; // version 1
			byte[] txt = Encoding.ASCII.GetBytes(name);
			Buffer.BlockCopy(txt, 0, sendData, 3, txt.Length);
			try
			{
				_stream.Write(sendData, 0, sendData.Length);
			}
			catch(Exception ex)
			{
				//Message?.Invoke(LngText(LngKeys.Message_SubscribeServerError));
				Log(LogTypes.Error, nameof(SendPeerSearch), $"error sending data to subscribe server", ex);
				reply.Valid = false;
				reply.Error = "reply server error";
				return reply;
			}

			byte[] ack = new byte[] { 0x08, 0x00 };
			List<PeerQueryData> list = new List<PeerQueryData>();
			while (true)
			{
				byte[] recvData = new byte[102];
				int recvLen = 0;
				try
				{
					recvLen = _stream.Read(recvData, 0, recvData.Length);
				}
				catch(Exception ex)
				{
					Log(LogTypes.Error, nameof(SendPeerSearch), $"error receiving data from subscribe server", ex);
					reply.Valid = false;
					reply.Error = "reply server error";
					return reply;
				}
				//Logging.Instance.Log(LogTypes.Debug, TAG, nameof(SendPeerSearch), $"recvLen={recvLen}");

				if (recvLen == 0)
				{
					Log(LogTypes.Error, nameof(SendPeerSearch), $"recvLen=0");
					reply.Error = $"no data received";
					return reply;
				}

				if (recvData[0] == 0x09)
				{
					// end of list
					break;
				}

				if (recvLen < 2 + 0x64)
				{
					Log(LogTypes.Warn, nameof(SendPeerSearch), $"received data to short ({recvLen} bytes)");
					reply.Error = $"received data to short ({recvLen} bytes)";
					continue;
				}

				if (recvData[1] != 0x64)
				{
					Log(LogTypes.Warn, nameof(SendPeerSearch), $"invalid length value ({recvData[1]})");
					reply.Error = $"invalid length value ({recvData[1]})";
					continue;
				}

				PeerQueryData data = ByteArrayToPeerData(recvData, 2);
				Log(LogTypes.Debug, nameof(SendPeerSearch), $"found {data}");

				list.Add(data);

				// send ack
				try
				{
					_stream.Write(ack, 0, ack.Length);
				}
				catch (Exception ex)
				{
					Log(LogTypes.Error, nameof(SendPeerSearch), $"error sending data to subscribe server", ex);
					return null;
				}
			}

			reply.List = list.ToArray();
			reply.Valid = true;
			reply.Error = "ok";

			return reply;
		}

		private PeerQueryData ByteArrayToPeerData(byte[] bytes, int offset)
		{
			if (offset + 100 > bytes.Length)
			{
				return null;
			}

			PeerQueryData data = new PeerQueryData
			{
				Number = BitConverter.ToInt32(bytes, offset),
				LongName = Encoding.ASCII.GetString(bytes, offset + 4, 40).Trim(new char[] { '\x00' }),
				SpecialAttribute = BitConverter.ToUInt16(bytes, offset + 44),
				PeerType = bytes[offset + 46],
				HostName = Encoding.ASCII.GetString(bytes, offset + 47, 40).Trim(new char[] { '\x00' }),
				IpAddress = $"{bytes[offset + 87]}.{bytes[offset + 88]}.{bytes[offset + 89]}.{bytes[offset + 90]}",
				PortNumber = BitConverter.ToUInt16(bytes, offset + 91),
				ExtensionNumber = bytes[offset + 93],
				Pin = BitConverter.ToUInt16(bytes, offset + 94)
			};

			UInt32 timestamp = BitConverter.ToUInt32(bytes, offset + 96);
			DateTime dt = new DateTime(1900, 1, 1, 0, 0, 0, 0);
			data.LastChange = dt.AddSeconds(timestamp);

			return data;
		}

		/// <summary>
		/// Update own ip-number
		/// </summary>
		/// <param name="number"></param>
		/// <returns>peer or null</returns>
		public ClientUpdateReply SendClientUpdate(int number, int pin, int port)
		{
			Log(LogTypes.Debug, nameof(SendClientUpdate), $"number='{number}'");
			ClientUpdateReply reply = new ClientUpdateReply();

			if (_client == null)
			{
				Log(LogTypes.Error, nameof(SendPeerQuery), "no server connection");
				reply.Error = "no server connection";
				return reply;
			}

			byte[] sendData = new byte[2 + 8];
			sendData[0] = 0x01; // packet type: client update
			sendData[1] = 0x08; // length
			byte[] data = BitConverter.GetBytes((UInt32)number);
			Buffer.BlockCopy(data, 0, sendData, 2, 4);
			data = BitConverter.GetBytes((UInt16)pin);
			Buffer.BlockCopy(data, 0, sendData, 6, 2);
			data = BitConverter.GetBytes((UInt16)port);
			Buffer.BlockCopy(data, 0, sendData, 8, 2);

			try
			{
				_stream.Write(sendData, 0, sendData.Length);
			}
			catch (Exception ex)
			{
				Log(LogTypes.Error, nameof(SendClientUpdate), $"error sending data to subscribe server", ex);
				reply.Error = "send server data server error";
				return reply;
			}

			byte[] recvData = new byte[6];
			int recvLen = 0;
			try
			{
				recvLen = _stream.Read(recvData, 0, recvData.Length);
			}
			catch (Exception ex)
			{
				Log(LogTypes.Error, nameof(SendClientUpdate), $"error receiving data from subscribe server", ex);
				reply.Error = "receive server data error";
				return reply;
			}

			if (recvLen != 6)
			{
				Log(LogTypes.Warn, nameof(SendClientUpdate), $"wrong reply packet size ({recvLen})");
				reply.Error = "error";
				return reply;
			}

			if (recvData[0] != 0x02)
			{
				// peer not found
				Log(LogTypes.Error, nameof(SendClientUpdate), $"wrong reply packet, type={recvData[0]:X2}");
				reply.Error = "error";
				return reply;
			}

			reply.Success = true;
			reply.IpAddress = $"{recvData[2]}.{recvData[3]}.{recvData[4]}.{recvData[5]}";
			reply.Error = "ok";

			return reply;
		}

		public static string SelectIp4Addr(string host)
		{
			try
			{
				IPHostEntry hostEntry = Dns.GetHostEntry(host);
				if (hostEntry.AddressList == null || hostEntry.AddressList.Length == 0)
				{
					Log(LogTypes.Warn, nameof(SelectIp4Addr), $"ip address error {host}, dns request failed");
					return null;
				}

				string ipv4Str = null;
				for (int i = 0; i < hostEntry.AddressList.Length; i++)
				{
					IPAddress ipAddr = hostEntry.AddressList[i];
					if (ipv4Str == null && ipAddr.AddressFamily.ToString() == ProtocolFamily.InterNetwork.ToString())
					{
						// ipv4 address
						ipv4Str = ipAddr.ToString();
					}
					Log(LogTypes.Debug, nameof(SelectIp4Addr),
						$"{i + 1}: ipAddr={ipAddr} mapToIPv4={ipAddr.MapToIPv4()} addressFamily={ipAddr.AddressFamily}");
				}
				Log(LogTypes.Debug, nameof(SelectIp4Addr), $"ipv4addr = {ipv4Str}");
				return ipv4Str;
			}
			catch(Exception)
			{
				return null;
			}
		}

		private static void Log(LogTypes logType, string methode, string msg, Exception ex = null)
		{
			if (logType == LogTypes.Error && ex != null)
			{
				LogManager.Instance.Logger.Error(TAG, methode, msg, ex);
			}
			else
			{
				LogManager.Instance.Logger.Log(logType, TAG, methode, msg);
			}
		}
	}

	/*
	class AsciiQueryResult
	{
		public string Number { get; set; }
		public string Name { get; set; }
		public int? Type { get; set; }
		public string HostName { get; set; }
		public int? Port { get; set; }
		public int? ExtensionNumber { get; set; }

		public override string ToString()
		{
			return $"{Number} '{Name}' {Type} {HostName} {Port} {ExtensionNumber}";
		}
	}
	*/

	public class ClientUpdateReply
	{
		public bool Success { get; set; } = false;
		public string Error { get; set; }
		public string IpAddress { get; set; }
	}

	public class PeerQueryReply
	{
		public bool Valid { get; set; } = false;
		public string Error { get; set; }
		public PeerQueryData Data { get; set; }
	}

	public class PeerSearchReply
	{
		public bool Valid { get; set; } = false;
		public string Error { get; set; }
		public PeerQueryData[] List { get; set; }
	}

	public class PeerQueryData
	{
		public int Number { get; set; }
		public string LongName { get; set; }
		public UInt16 SpecialAttribute { get; set; }
		public int PeerType { get; set; }
		public string IpAddress { get; set; }
		public string HostName { get; set; }
		public int PortNumber { get; set; }
		public int ExtensionNumber { get; set; }
		public int Pin { get; set; }
		public DateTime LastChange { get; set; }

		public string Address => !string.IsNullOrEmpty(HostName) ? HostName : IpAddress;

		public string Display => $"{Number} {LongName} {PeerType}";

		public override string ToString()
		{
			return $"{Number} {LongName} {PeerType} {Address} {PortNumber} {ExtensionNumber}";
		}
	}
}

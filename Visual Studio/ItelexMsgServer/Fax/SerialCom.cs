/*
 * Copyright 2011 hecke
 * 
 * This file is part of (G)VA18(B) multimeter protcol.
 * 
 * (G)VA18(B) multimeter protcol decoder is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * (G)VA18(B) multimeter protcol decoder is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with (G)VA18(B) multimeter protcol decoder. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SysSerPorts = System.IO.Ports;
using System.Threading;

/**
 * the code below is... ugly.
 */

namespace ItelexMsgServer.Serial
{
    class SerialCom
    {

        private System.IO.Ports.SerialPort SerPort
        {
            set;
            get;
        }

        private Thread readThread = null;

        public delegate int RecByteHandler(byte b);
        List<RecByteHandler> _receivers = new List<RecByteHandler>();

        /*******************************************************************/
        public void AttachReceiver(ref RecByteHandler receiver)
        {
            _receivers.Add(receiver);
        }

        public void DetachReceiver(ref RecByteHandler receiver)
        {
            _receivers.Remove(receiver);
        }

        /*******************************************************************/

        public SerialCom(string port, 
                         int baudrate, 
                         SysSerPorts.Parity parity , 
                         SysSerPorts.StopBits stopbits, 
                         int databits)
        {
            try
            {
                SerPort = new SysSerPorts.SerialPort(port,
                                                          baudrate,
                                                          parity,
                                                          databits,
                                                          stopbits);

                SerPort.ReadTimeout = 500;

                SerPort.Open();

                if (!SerPort.IsOpen)
                {
                    SerPort = null;
                }
            }
            catch {
                SerPort = null;
            }
        }

        ~SerialCom()
        {
            if (isValid())
            {
                SerPort.Close();
            }
        }

        /*******************************************************************/

        private struct ThreadData {
            public SysSerPorts.SerialPort ser_port;
            public List<RecByteHandler> receivers;
        }

        public bool startReader()
        {
            if (!isValid())
                return false;

            if (readThread != null)
                return true;

            readThread = new Thread(ReadHandler);

            ThreadData td;
            td.ser_port = SerPort;
            td.receivers = _receivers;

            readThread.Start(td);
            return true;
        }

                
        private static void ReadHandler(object thread_data)
        {

            SysSerPorts.SerialPort ser_port = (((ThreadData)thread_data).ser_port);
            List<RecByteHandler> _receivers = (((ThreadData)thread_data).receivers);
            byte[] inb = new byte[1];

            while ( ser_port.IsOpen )
            {
                
                try
                {
                    if (1 == ser_port.Read(inb, 0, 1))
                    {
                        foreach (RecByteHandler receiver in _receivers)
                        {
                            receiver(inb[0]);
                        }                        
                    }

                    
                }//__try
                    
                catch { }
                
            }
            
        }

        public bool stopReader()
        {
            if (!isValid())
                return false;

            SerPort.Close();
            SerPort = null;

            return true;
        }

        public bool isValid() 
        {
            return SerPort != null;
        }

    }
}

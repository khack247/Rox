﻿using EIP;
using System;

namespace Rox
{
  internal class KeyenceEip
  {
    public bool Enabled { get; set; }
    public ConnectionSettings Settings { get; set; } = new ConnectionSettings();
    private EIPClient eipClient;
    //System.Threading.Timer updateEeipTmr;
    private byte[] InputState;
    private byte[] OutputState;
    public bool IsConnected { get; private set; }
    public bool IsPaused { get; private set; }

    public KeyenceEip()
    {
      Settings = new ConnectionSettings();
    }
    public class ConnectionSettings
    {
      public string IpAddress { get; set; } = "127.0.0.1";
      public int Port { get; set; } = 44818;
      public byte AssemblyIn { get; set; } = 100;
      public byte AssemblyOut { get; set; } = 101;
      private uint _rpi = 50000;
      public uint RPI { get { return _rpi; } set { _rpi = value < 50000 ? 50000 : value; } } // min = 50000;
    }
    public void Connect()
    {
      try
      {
        eipClient = new EIPClient() { IPAddress = Settings.IpAddress, TCPPort = (ushort)Settings.Port }; // omron NX1P2 = "192.168.1.3"
        eipClient.RegisterSession();

        //Parameters from Originator -> Target
        eipClient.O_T_InstanceID = Settings.AssemblyOut;
        eipClient.O_T_Length = eipClient.Detect_O_T_Length();
        eipClient.O_T_RealTimeFormat = EIP.RealTimeFormat.Header32Bit;   //Header Format
        eipClient.O_T_OwnerRedundant = false;
        eipClient.O_T_Priority = EIP.Priority.Scheduled;
        eipClient.O_T_VariableLength = false;
        eipClient.O_T_ConnectionType = EIP.ConnectionType.Point_to_Point;
        eipClient.RequestedPacketRate_O_T = Settings.RPI; //RPI (microseconds)

        //Parameters from Target -> Originator
        eipClient.T_O_InstanceID = Settings.AssemblyIn;
        eipClient.T_O_Length = eipClient.Detect_T_O_Length();
        eipClient.T_O_RealTimeFormat = EIP.RealTimeFormat.Modeless;
        eipClient.T_O_OwnerRedundant = false;
        eipClient.T_O_Priority = EIP.Priority.Scheduled;
        eipClient.T_O_VariableLength = false;
        eipClient.T_O_ConnectionType = EIP.ConnectionType.Point_to_Point;
        eipClient.RequestedPacketRate_T_O = Settings.RPI; //RPI (microseconds)

        //Forward open initiates the Implicit Messaging
        eipClient.ForwardOpen();
        //System.Threading.Thread.Sleep(500); 
        //updateEeipTmr = new System.Threading.Timer(new System.Threading.TimerCallback(updateEeipStats), null, Settings.RPI / 1000, Settings.RPI / 1000);

        IsConnected = true;
      }
      catch (Exception)
      {
        IsConnected = false;
      }
    }
    public void Disconnect()
    {
      if (eipClient != null)
      {
        try
        {
          if (!IsPaused) { eipClient.ForwardClose(); }
          eipClient.UnRegisterSession();
          eipClient = null;
        }
        catch (Exception)
        {
          return;
        }
      }
      IsConnected = false;
    }
    private void updateEeipStats(object state)
    {
      try
      {
        InputState = eipClient.AssemblyObject.getInstance(Settings.AssemblyIn);
        //lbl1.Content = "input (assembly 100) as unsigned short: " + string.Format("{0}", EIPClient.ToUshort(result));
        //Console.WriteLine(string.Join(", ", InputState));
        OutputState = eipClient.AssemblyObject.getInstance(Settings.AssemblyOut);
        //Console.WriteLine(string.Join(", ", OutputState));
        //lbl3.Content = "output (assembly 101) as unsigned short: " + string.Format("{0}", EIPClient.ToUshort(result));
        ////txt1.Text = string.Join(", ", eeipClient.T_O_IOData.Take(eeipClient.T_O_Length));
        //txt2.Text = string.Join(", ", eipClient.O_T_IOData.Take(eipClient.O_T_Length));
      }
      catch (Exception ex)
      {
        Console.WriteLine("updateEeipStats: " + ex.Message);
      }
    }
    public byte[] GetInputs()
    {
      try
      {
        return eipClient.AssemblyObject.getInstance(Settings.AssemblyIn);
      }
      catch (Exception)
      {
        return new byte[0];
      }
    }
    public byte[] GetOutputs()
    {
      try
      {
        return eipClient.AssemblyObject.getInstance(Settings.AssemblyOut);
      }
      catch (Exception)
      {
        return new byte[0];
      }
    }
    public EIP.ObjectLibrary.IdentityObject.StateEnum GetState()
    {
      try
      {
        return eipClient.IdentityObject.State;
      }
      catch (Exception)
      {
        return EIP.ObjectLibrary.IdentityObject.StateEnum.Nonexistent;
      }
    }
    public void SetBit(short Word, short Bit, bool Value)
    {
      Bit += 1;
      //Console.WriteLine("setting value: " + Value);
      if (Value)
      {
        eipClient.O_T_IOData[Word] = TurnBitOn(eipClient.O_T_IOData[Word], Bit);
      }
      else
      {
        eipClient.O_T_IOData[Word] = TurnBitOff(eipClient.O_T_IOData[Word], Bit);
      }
    }
    private static byte TurnBitOn(byte value, int bitIndex)
    {
      return (byte)(value | bitIndex);
    }
    private static byte TurnBitOff(byte value, int bitIndex)
    {
      return (byte)(value & ~bitIndex);
    }
    private static byte FlipBit(byte value, int bitIndex)
    {
      return (byte)(value ^ bitIndex);
    }
    public void SetNumber(short StartWord, byte Value)
    {
      eipClient.O_T_IOData[StartWord] = Value;
    }
  }
}

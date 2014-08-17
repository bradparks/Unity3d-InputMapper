﻿#if UNITY_ANDROID
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ws.winx.devices;
using UnityEngine;
using System.Timers;

namespace ws.winx.platform.android
{
    public class GenericHIDDevice : HIDDevice  //TODO thru  public ReadWriteListenerProxy(int index ) : base("ws.winx.hid.IReadWriteListener") {}
    {
        ReadWriteListenerProxy _listener;

        private int _numAxes;
        protected long _timestamp;
        private IHIDInterface _hidInterface;
        private  AndroidJavaObject _device;
        private HIDReport __lastHIDReport;
        private Timer timeoutTimer;

        public int numAxes
        {
            get { return _numAxes; }
            set { _numAxes = value; }
        }
        private int _numButtons;

        public int numButtons
        {
            get { return _numButtons; }
            set { _numButtons = value; }
        }


        int _InputReportByteLength=8;

        override public int InputReportByteLength
        {
            get { return _InputReportByteLength; }
            set {
                if (value < 2) throw new Exception("InputReportByteLength should be >1 ");  _InputReportByteLength = value; }
        }
        
        int _OutputReportByteLength=8;

        override public int OutputReportByteLength
        {
            get { return _OutputReportByteLength; }
            set { if (value < 2) throw new Exception("InputReportByteLength should be >1 ");  _OutputReportByteLength = value; }
        }

        ElapsedEventHandler _timeOutEventHandler;
        Action<object, ElapsedEventArgs> timeoutActionHandler;

        public GenericHIDDevice(int inx, AndroidJavaObject device, IHIDInterface hidInterface)
            : base(inx, device.Get<int>("VID"), device.Get<int>("PID"), IntPtr.Zero, hidInterface, device.Get<string>("path"))
        {
            //0.4s is Default Long so should be greater then >400ms
           timeoutTimer = new System.Timers.Timer(1550);
           timeoutTimer.AutoReset = false;
           timeoutActionHandler = (sender, args) => { onReadTimeOut(null, sender, args); };  
            _timeOutEventHandler=new ElapsedEventHandler(timeoutActionHandler);
           timeoutTimer.Elapsed += _timeOutEventHandler;
               

             
            _device=device;
            _listener = new ReadWriteListenerProxy();
        }

      

        //	public void read(byte[] into,IReadWriteListener listener, int timeout)
        public override void Read(HIDDevice.ReadCallback callback)
        {
            timeoutTimer.Stop();

           

            //TODO: create fields (as read should be called after onRead) or better Object.pool
            _listener.ReadComplete =  new ReadCallback((bytes) => {
                UnityEngine.Debug.Log("ReadComplete");

                timeoutTimer.Stop();
               
                //if (this.__lastHIDReport == null)
                //{
                //    this.__lastHIDReport = new HIDReport(this.index, (byte[])bytes, HIDReport.ReadStatus.Success);
                //}
                //else
                //{
                //    this.__lastHIDReport.index=this.index;
                //    this.__lastHIDReport.Data=(byte[])bytes;
                //    this.__lastHIDReport.Status=HIDReport.ReadStatus.Success;
                //}

                this.__lastHIDReport = new HIDReport(this.index, (byte[])bytes, HIDReport.ReadStatus.Success);
                
                callback.Invoke(this.__lastHIDReport);
            });

           

            byte[] from=new byte[_InputReportByteLength];


            timeoutActionHandler = (sender, args) => { onReadTimeOut(callback, sender, args); };       
            timeoutTimer.Start(); 
         //   UnityEngine.Debug.Log("GenericHIDDevice >>>>> try read");  
            _device.Call("read", from, _listener, 0);
          //  UnityEngine.Debug.Log("GenericHIDDevice >>>>> read out"); 
           
        }

        private void onReadTimeOut(ReadCallback callback,object sender, ElapsedEventArgs args)
        {
            System.Timers.Timer timer = ((System.Timers.Timer)sender);
            timer.Stop();
            timer = null;
        

            UnityEngine.Debug.Log("timeout");

            if ((callback != null)) callback.Invoke(__lastHIDReport);
           
        }

        //public void write(final byte[] from, IReadWriteListener listener, int timeout)
        public override void Write(object data)
        {
            _device.Call("write",(byte[]) data,0);
           
        }


        public override void Write(object data, HIDDevice.WriteCallback callback)
        {
            _listener.WriteComplete = callback;
            _device.Call("write", (byte[])data, _listener, 0);
        }




       

        public long timestamp
        {
            get { return _timestamp; }
            set { _timestamp = value; }
        }


         override public void Dispose()
        {
            if (timeoutTimer != null)
            {
                timeoutTimer.Stop();
                timeoutTimer.Elapsed -= _timeOutEventHandler;
            }

            if (IsOpen) CloseDevice();
        }

      

       
    }
}
#endif
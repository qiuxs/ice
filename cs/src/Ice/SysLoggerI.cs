// **********************************************************************
//
// Copyright (c) 2001
// ZeroC, Inc.
// Billerica, MA, USA
//
// All Rights Reserved.
//
// Ice is free software; you can redistribute it and/or modify it under
// the terms of the GNU General Public License version 2 as published by
// the Free Software Foundation.
//
// **********************************************************************
namespace Ice
{
	
    public sealed class SysLoggerI : LocalObjectImpl, Logger
    {
	public SysLoggerI(string ident)
	{
	    _ident = ident;
	    
	    //
	    // Open a datagram socket to communicate with the localhost
	    // syslog daemon.
	    // 
	    try
	    {
		_host = System.Net.Dns.GetHostByName(System.Net.Dns.GetHostName()).AddressList[0];
		_socket = new SupportClass.UdpClientSupport();
		_socket.Connect(_host, _port);
	    }
	    catch(System.Exception ex)
	    {
		throw new Ice.DNSException(ex);
	    }
	}
	
	public void trace(string category, string message)
	{
	    log(LOG_INFO, category + ": " + message);
	}
	
	public void warning(string message)
	{
	    log(LOG_WARNING, message);
	}
	
	public void error(string message)
	{
	    log(LOG_ERR, message);
	}
	
	private void log(int severity, string message)
	{
	    try
	    {
		//
		// Create a syslog message as defined by the RFC 3164:
		// <PRI>HEADER MSG. PRI is the priority and is calculated
		// from the facility and the severity. We don't specify
		// the HEADER. MSG contains the identifier followed by a
		// colon character and the message.
		//
		
		int priority = (LOG_USER << 3) | severity;
		
		string msg = '<' + System.Convert.ToString(priority) + '>' + _ident + ": " + message;
		
		byte[] buf = SupportClass.ToByteArray(msg);
		SupportClass.PacketSupport p = new SupportClass.PacketSupport(buf, buf.Length, new System.Net.IPEndPoint(_host, _port));
		SupportClass.UdpClientSupport.Send(_socket, p);
	    }
	    catch(System.IO.IOException ex)
	    {
		Ice.SocketException se = new Ice.SocketException(ex);
		throw se;
	    }
	}
	
	private string _ident;
	private SupportClass.UdpClientSupport _socket;
	private System.Net.IPAddress _host;
	private static int _port = 514;
	
	//
	// Syslog facilities facilities (as defined in syslog.h)
	// 
	private static readonly int LOG_USER = 1;
	
	//
	// Syslog priorities (as defined in syslog.h)
	// 
	private static readonly int LOG_ERR = 3;
	private static readonly int LOG_WARNING = 4;
	private static readonly int LOG_INFO = 6;
    }

}

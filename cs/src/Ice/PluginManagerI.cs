// **********************************************************************
//
// Copyright (c) 2003
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
	
    using System.Collections;
    using System.Diagnostics;

    public sealed class PluginManagerI : LocalObjectImpl, PluginManager
    {
	private static string _kindOfObject = "plug-in";

	public Plugin getPlugin(string name)
	{
	    lock(this)
	    {
		if(_communicator == null)
		{
		    throw new CommunicatorDestroyedException();
		}
	    
		Plugin p = (Plugin)_plugins[name];
		if(p != null)
		{
		    return p;
		}
		NotRegisteredException ex = new NotRegisteredException();
		ex.id = name;
		ex.kindOfObject = _kindOfObject;
		throw ex;
	    }
	}

	public void addPlugin(string name, Plugin plugin)
	{
	    lock(this)
	    {
		if(_communicator == null)
		{
		    throw new CommunicatorDestroyedException();
		}
	    
		if(_plugins.Contains(name))
		{
		    AlreadyRegisteredException ex = new AlreadyRegisteredException();
		    ex.id = name;
		    ex.kindOfObject = _kindOfObject;
		    throw ex;
		}
		_plugins[name] = plugin;
	    }
	}

	public void destroy()
	{
	    lock(this)
	    {
		if(_communicator != null)
		{
		    foreach(Plugin plugin in _plugins.Values)
		    {
			plugin.destroy();
		    }
		
		    _communicator = null;
		}
	    }
	}
	
	public PluginManagerI(Communicator communicator)
	{
	    _communicator = communicator;
	    _plugins = new Hashtable();
	}

	public void loadPlugins(ref string[] cmdArgs)
	{
	    Debug.Assert(_communicator != null);
	    
	    //
	    // Load and initialize the plug-ins defined in the property set
	    // with the prefix "Ice.Plugin.". These properties should
	    // have the following format:
	    //
	    // Ice.Plugin.name=entry_point [args]
	    //
	    string prefix = "Ice.Plugin.";
	    Ice.Properties properties = _communicator.getProperties();
	    PropertyDict plugins = properties.getPropertiesForPrefix(prefix);
	    foreach(DictionaryEntry entry in plugins)
	    {
		string name = ((string)entry.Key).Substring(prefix.Length);
		string val = (string)entry.Value;
		
		//
		// Separate the entry point from the arguments.
		//
		string className;
		string[] args;
		int pos = val.IndexOf(' ');
		if(pos == -1)
		{
		    pos = val.IndexOf('\t');
		}
		if(pos == -1)
		{
		    pos = val.IndexOf('\n');
		}
		if(pos == -1)
		{
		    className = val;
		    args = new string[0];
		}
		else
		{
		    className = val.Substring(0, pos);
		    char[] delims = { ' ', '\t', '\n' };
		    args = val.Substring(pos).Trim().Split(delims, pos);
		}
		
		//
		// Convert command-line options into properties. First we
		// convert the options from the plug-in configuration, then
		// we convert the options from the application command-line.
		//
		StringSeq argSeq = new StringSeq(args);
		argSeq = properties.parseCommandLineOptions(name, argSeq);
		StringSeq cmdSeq = new StringSeq(cmdArgs);
		cmdSeq = properties.parseCommandLineOptions(name, cmdSeq);
		cmdArgs = cmdSeq.ToArray();
		
		loadPlugin(name, className, argSeq);
	    }
	}
	
	private void loadPlugin(string name, string className, StringSeq args)
	{
	    Debug.Assert(_communicator != null);
	    
	    //
	    // Instantiate the class.
	    //
	    PluginFactory factory = null;
	    try
	    {
		System.Type c = System.Type.GetType(className);
		System.Object obj = SupportClass.CreateNewInstance(c);
		try
		{
		    factory = (PluginFactory) obj;
		}
		catch(System.InvalidCastException ex)
		{
		    PluginInitializationException e = new PluginInitializationException(ex);
		    e.reason = "class " + className + " does not implement Ice.PluginFactory";
		    throw e;
		}
	    }
	    catch(System.UnauthorizedAccessException ex)
	    {
		PluginInitializationException e = new PluginInitializationException(ex);
		e.reason = "unable to access default constructor in class " + className;
		throw e;
	    }
	    catch(System.Exception ex)
	    {
		PluginInitializationException e = new PluginInitializationException(ex);
		e.reason = "unable to instantiate class " + className;
		throw e;
	    }
	    
	    //
	    // Invoke the factory.
	    //
	    Plugin plugin = null;
	    try
	    {
		plugin = factory.create(_communicator, name, args);
	    }
	    catch(System.Exception ex)
	    {
		PluginInitializationException e = new PluginInitializationException(ex);
		e.reason = "exception in factory " + className;
		throw e;
	    }
	    
	    _plugins[name] = plugin;
	}
	
	private Communicator _communicator;
	private Hashtable _plugins;
    }

}

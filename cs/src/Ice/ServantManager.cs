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

namespace IceInternal
{

using System.Collections;
using System.Diagnostics;

public sealed class ServantManager : SupportClass.ThreadClass
{
    public void addServant(Ice.Object servant, Ice.Identity ident)
    {
	lock(this)
	{
	    Debug.Assert(_instance != null); // Must not be called after destruction.
	    
	    Ice.Object o = (Ice.Object)_servantMap[ident];
	    if(o != null)
	    {
		Ice.AlreadyRegisteredException ex = new Ice.AlreadyRegisteredException();
		ex.id = Ice.Util.identityToString(ident);
		ex.kindOfObject = "servant";
		throw ex;
	    }
	    
	    _servantMap[ident] = servant;
	}
    }
    
    public void removeServant(Ice.Identity ident)
    {
	lock(this)
	{
	    Debug.Assert(_instance != null); // Must not be called after destruction.
	    
	    Ice.Object o = (Ice.Object)_servantMap[ident];
	    if(o == null)
	    {
		Ice.NotRegisteredException ex = new Ice.NotRegisteredException();
		ex.id = Ice.Util.identityToString(ident);
		ex.kindOfObject = "servant";
		throw ex;
	    }
	    
	    _servantMap.Remove(ident);
	}
    }
    
    public Ice.Object findServant(Ice.Identity ident)
    {
	lock(this)
	{
	    Debug.Assert(_instance != null); // Must not be called after destruction.
	    
	    return (Ice.Object)_servantMap[ident];
	}
    }
    
    public void addServantLocator(Ice.ServantLocator locator, string prefix)
    {
	lock(this)
	{
	    Debug.Assert(_instance != null); // Must not be called after destruction.
	    
	    Ice.ServantLocator l = (Ice.ServantLocator)_locatorMap[prefix];
	    if(l != null)
	    {
		Ice.AlreadyRegisteredException ex = new Ice.AlreadyRegisteredException();
		ex.id = prefix;
		ex.kindOfObject = "servant locator";
		throw ex;
	    }
	    
	    _locatorMap[prefix] = locator;
	}
    }
    
    public Ice.ServantLocator findServantLocator(string prefix)
    {
	lock(this)
	{
	    Debug.Assert(_instance != null); // Must not be called after destruction.
	    
	    return (Ice.ServantLocator)_locatorMap[prefix];
	}
    }
    
    //
    // Only for use by Ice.ObjectAdapterI.
    //
    public ServantManager(Instance instance, string adapterName)
    {
	//UPGRADE_TODO: Field java.util was not converted. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1095"'
	_servantMap = new Hashtable();
	_locatorMap = new Hashtable();
	_instance = instance;
	_adapterName = adapterName;
    }
    
    ~ServantManager()
    {
	//
	// Don't check whether destroy() has been called. It might have
	// not been called if the associated object adapter was not
	// properly deactivated.
	//
	//Debug.Assert(_instance == null);
    }
    
    //UPGRADE_TODO: The equivalent of method 'java.lang.Thread.destroy' is not an override method. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1143"'
    //
    // Only for use by Ice.ObjectAdatperI.
    //
    public void destroy()
    {
	lock(this)
	{
	    Debug.Assert(_instance != null); // Must not be called after destruction.
	    
	    _servantMap.Clear();
	    
	    foreach(DictionaryEntry p in _locatorMap)
	    {
		Ice.ServantLocator locator = (Ice.ServantLocator)p.Value;
		try
		{
		    locator.deactivate((string)p.Key);
		}
		catch(System.Exception ex)
		{
		    string s = "exception during locator deactivation:\n" + "object adapter: `"
		               + _adapterName + "'\n" + "locator prefix: `" + p.Key + "'\n" + ex;
		    _instance.logger().error(s);
		}
	    }
	    
	    _locatorMap.Clear();

	    _instance = null;
	}
    }
    
    private Instance _instance;
    private readonly string _adapterName;
    private Hashtable _servantMap;
    private Hashtable _locatorMap;
}

}

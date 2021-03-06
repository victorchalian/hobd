﻿using System;
using System.Collections.Generic;

namespace hobd
{

public class CoreSensor : Sensor
{
    protected SensorRegistry registry;
    protected int listenerCount = 0;
        
    public CoreSensor()
    {
        // default values
    }
        
    public virtual double Value {get; protected set;}
    public long TimeStamp {get; set;}
    
    public virtual string ID { get; set; }
    
    public virtual string Name { get; set; }

    public virtual string Units {
        get;
        set;
    }
    
    public virtual void SetRegistry(SensorRegistry registry) {
        this.registry = registry;
    }
    public virtual void DetachRegistry() {
        this.registry = null;
    }
        
    
    public virtual void NotifyAddListener(Action<Sensor> listener)
    {
        listenerCount++;
        if (listenerCount == 1)
            Activate();
    }
    
    public virtual void NotifyRemoveListener(Action<Sensor> listener)
    {
        listenerCount--;
        if (listenerCount == 0)
            Deactivate();
    }

    public virtual void Activate()
    {
    }
    public virtual void Deactivate()
    {
    }
}

}

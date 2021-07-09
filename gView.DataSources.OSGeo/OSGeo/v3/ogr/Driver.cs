//------------------------------------------------------------------------------
// <auto-generated />
//
// This file was automatically generated by SWIG (http://www.swig.org).
// Version 3.0.12
//
// Do not make changes to this file unless you know what you are doing--modify
// the SWIG interface file instead.
//------------------------------------------------------------------------------

namespace OSGeo_v3.OGR {

using global::System;
using global::System.Runtime.InteropServices;

public class Driver : global::System.IDisposable {
  private HandleRef swigCPtr;
  protected bool swigCMemOwn;
  protected object swigParentRef;

  protected static object ThisOwn_true() { return null; }
  protected object ThisOwn_false() { return this; }

  public Driver(IntPtr cPtr, bool cMemoryOwn, object parent) {
    swigCMemOwn = cMemoryOwn;
    swigParentRef = parent;
    swigCPtr = new HandleRef(this, cPtr);
  }

  public static HandleRef getCPtr(Driver obj) {
    return (obj == null) ? new HandleRef(null, IntPtr.Zero) : obj.swigCPtr;
  }
  public static HandleRef getCPtrAndDisown(Driver obj, object parent) {
    if (obj != null)
    {
      obj.swigCMemOwn = false;
      obj.swigParentRef = parent;
      return obj.swigCPtr;
    }
    else
    {
      return new HandleRef(null, IntPtr.Zero);
    }
  }
  public static HandleRef getCPtrAndSetReference(Driver obj, object parent) {
    if (obj != null)
    {
      obj.swigParentRef = parent;
      return obj.swigCPtr;
    }
    else
    {
      return new HandleRef(null, IntPtr.Zero);
    }
  }

  public virtual void Dispose() {
    lock(this) {
      if (swigCPtr.Handle != global::System.IntPtr.Zero) {
        if (swigCMemOwn) {
          swigCMemOwn = false;
          throw new global::System.MethodAccessException("C++ destructor does not have public access");
        }
        swigCPtr = new global::System.Runtime.InteropServices.HandleRef(null, global::System.IntPtr.Zero);
      }
      global::System.GC.SuppressFinalize(this);
    }
  }

  public string name {
    get {
      string ret = OgrPINVOKE.Driver_name_get(swigCPtr);
      if (OgrPINVOKE.SWIGPendingException.Pending) throw OgrPINVOKE.SWIGPendingException.Retrieve();
      return ret;
    } 
  }

  public DataSource CreateDataSource(string utf8_path, string[] options) {
    IntPtr cPtr = OgrPINVOKE.Driver_CreateDataSource(swigCPtr, Ogr.StringToUtf8Bytes(utf8_path), (options != null)? new OgrPINVOKE.StringListMarshal(options)._ar : null);
    DataSource ret = (cPtr == IntPtr.Zero) ? null : new DataSource(cPtr, true, ThisOwn_true());
    if (OgrPINVOKE.SWIGPendingException.Pending) throw OgrPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public DataSource CopyDataSource(DataSource copy_ds, string utf8_path, string[] options) {
    IntPtr cPtr = OgrPINVOKE.Driver_CopyDataSource(swigCPtr, DataSource.getCPtr(copy_ds), Ogr.StringToUtf8Bytes(utf8_path), (options != null)? new OgrPINVOKE.StringListMarshal(options)._ar : null);
    DataSource ret = (cPtr == IntPtr.Zero) ? null : new DataSource(cPtr, true, ThisOwn_true());
    if (OgrPINVOKE.SWIGPendingException.Pending) throw OgrPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public DataSource Open(string utf8_path, int update) {
    IntPtr cPtr = OgrPINVOKE.Driver_Open(swigCPtr, Ogr.StringToUtf8Bytes(utf8_path), update);
    DataSource ret = (cPtr == IntPtr.Zero) ? null : new DataSource(cPtr, true, ThisOwn_true());
    if (OgrPINVOKE.SWIGPendingException.Pending) throw OgrPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public int DeleteDataSource(string utf8_path) {
    int ret = OgrPINVOKE.Driver_DeleteDataSource(swigCPtr, Ogr.StringToUtf8Bytes(utf8_path));
    if (OgrPINVOKE.SWIGPendingException.Pending) throw OgrPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public bool TestCapability(string cap) {
    bool ret = OgrPINVOKE.Driver_TestCapability(swigCPtr, cap);
    if (OgrPINVOKE.SWIGPendingException.Pending) throw OgrPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public string GetName() {
    string ret = OgrPINVOKE.Driver_GetName(swigCPtr);
    if (OgrPINVOKE.SWIGPendingException.Pending) throw OgrPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public void Register() {
    OgrPINVOKE.Driver_Register(swigCPtr);
    if (OgrPINVOKE.SWIGPendingException.Pending) throw OgrPINVOKE.SWIGPendingException.Retrieve();
  }

  public void Deregister() {
    OgrPINVOKE.Driver_Deregister(swigCPtr);
    if (OgrPINVOKE.SWIGPendingException.Pending) throw OgrPINVOKE.SWIGPendingException.Retrieve();
  }

}

}

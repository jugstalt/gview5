/* ----------------------------------------------------------------------------
 * This file was automatically generated by SWIG (http://www.swig.org).
 * Version 1.3.39
 *
 * Do not make changes to this file unless you know what you are doing--modify
 * the SWIG interface file instead.
 * ----------------------------------------------------------------------------- */

namespace OSGeo_v1.OGR
{

    using System;
    using System.Runtime.InteropServices;

    public class FeatureDefn : IDisposable
    {
        private HandleRef swigCPtr;
        protected bool swigCMemOwn;
        protected object swigParentRef;

        protected static object ThisOwn_true() { return null; }
        protected object ThisOwn_false() { return this; }

        public FeatureDefn(IntPtr cPtr, bool cMemoryOwn, object parent)
        {
            swigCMemOwn = cMemoryOwn;
            swigParentRef = parent;
            swigCPtr = new HandleRef(this, cPtr);
        }

        public static HandleRef getCPtr(FeatureDefn obj)
        {
            return (obj == null) ? new HandleRef(null, IntPtr.Zero) : obj.swigCPtr;
        }
        public static HandleRef getCPtrAndDisown(FeatureDefn obj, object parent)
        {
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
        public static HandleRef getCPtrAndSetReference(FeatureDefn obj, object parent)
        {
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

        ~FeatureDefn()
        {
            Dispose();
        }

        public virtual void Dispose()
        {
            lock (this)
            {
                if (swigCPtr.Handle != IntPtr.Zero && swigCMemOwn)
                {
                    swigCMemOwn = false;
                    OgrPINVOKE.delete_FeatureDefn(swigCPtr);
                }
                swigCPtr = new HandleRef(null, IntPtr.Zero);
                swigParentRef = null;
                GC.SuppressFinalize(this);
            }
        }

        public FeatureDefn(string name_null_ok) : this(OgrPINVOKE.new_FeatureDefn(name_null_ok), true, null)
        {
            if (OgrPINVOKE.SWIGPendingException.Pending)
            {
                throw OgrPINVOKE.SWIGPendingException.Retrieve();
            }
        }

        public string GetName()
        {
            string ret = OgrPINVOKE.FeatureDefn_GetName(swigCPtr);
            if (OgrPINVOKE.SWIGPendingException.Pending)
            {
                throw OgrPINVOKE.SWIGPendingException.Retrieve();
            }

            return ret;
        }

        public int GetFieldCount()
        {
            int ret = OgrPINVOKE.FeatureDefn_GetFieldCount(swigCPtr);
            if (OgrPINVOKE.SWIGPendingException.Pending)
            {
                throw OgrPINVOKE.SWIGPendingException.Retrieve();
            }

            return ret;
        }

        public FieldDefn GetFieldDefn(int i)
        {
            IntPtr cPtr = OgrPINVOKE.FeatureDefn_GetFieldDefn(swigCPtr, i);
            FieldDefn ret = (cPtr == IntPtr.Zero) ? null : new FieldDefn(cPtr, false, ThisOwn_false());
            if (OgrPINVOKE.SWIGPendingException.Pending)
            {
                throw OgrPINVOKE.SWIGPendingException.Retrieve();
            }

            return ret;
        }

        public int GetFieldIndex(string name)
        {
            int ret = OgrPINVOKE.FeatureDefn_GetFieldIndex(swigCPtr, name);
            if (OgrPINVOKE.SWIGPendingException.Pending)
            {
                throw OgrPINVOKE.SWIGPendingException.Retrieve();
            }

            return ret;
        }

        public void AddFieldDefn(FieldDefn defn)
        {
            OgrPINVOKE.FeatureDefn_AddFieldDefn(swigCPtr, FieldDefn.getCPtr(defn));
            if (OgrPINVOKE.SWIGPendingException.Pending)
            {
                throw OgrPINVOKE.SWIGPendingException.Retrieve();
            }
        }

        public wkbGeometryType GetGeomType()
        {
            wkbGeometryType ret = (wkbGeometryType)OgrPINVOKE.FeatureDefn_GetGeomType(swigCPtr);
            if (OgrPINVOKE.SWIGPendingException.Pending)
            {
                throw OgrPINVOKE.SWIGPendingException.Retrieve();
            }

            return ret;
        }

        public void SetGeomType(wkbGeometryType geom_type)
        {
            OgrPINVOKE.FeatureDefn_SetGeomType(swigCPtr, (int)geom_type);
            if (OgrPINVOKE.SWIGPendingException.Pending)
            {
                throw OgrPINVOKE.SWIGPendingException.Retrieve();
            }
        }

        public int GetReferenceCount()
        {
            int ret = OgrPINVOKE.FeatureDefn_GetReferenceCount(swigCPtr);
            if (OgrPINVOKE.SWIGPendingException.Pending)
            {
                throw OgrPINVOKE.SWIGPendingException.Retrieve();
            }

            return ret;
        }

        public int IsGeometryIgnored()
        {
            int ret = OgrPINVOKE.FeatureDefn_IsGeometryIgnored(swigCPtr);
            if (OgrPINVOKE.SWIGPendingException.Pending)
            {
                throw OgrPINVOKE.SWIGPendingException.Retrieve();
            }

            return ret;
        }

        public void SetGeometryIgnored(int bIgnored)
        {
            OgrPINVOKE.FeatureDefn_SetGeometryIgnored(swigCPtr, bIgnored);
            if (OgrPINVOKE.SWIGPendingException.Pending)
            {
                throw OgrPINVOKE.SWIGPendingException.Retrieve();
            }
        }

        public int IsStyleIgnored()
        {
            int ret = OgrPINVOKE.FeatureDefn_IsStyleIgnored(swigCPtr);
            if (OgrPINVOKE.SWIGPendingException.Pending)
            {
                throw OgrPINVOKE.SWIGPendingException.Retrieve();
            }

            return ret;
        }

        public void SetStyleIgnored(int bIgnored)
        {
            OgrPINVOKE.FeatureDefn_SetStyleIgnored(swigCPtr, bIgnored);
            if (OgrPINVOKE.SWIGPendingException.Pending)
            {
                throw OgrPINVOKE.SWIGPendingException.Retrieve();
            }
        }

    }

}

/* ----------------------------------------------------------------------------
 * This file was automatically generated by SWIG (http://www.swig.org).
 * Version 1.3.39
 *
 * Do not make changes to this file unless you know what you are doing--modify
 * the SWIG interface file instead.
 * ----------------------------------------------------------------------------- */

namespace OSGeo_v1.GDAL
{

    using System;
    using System.Runtime.InteropServices;

    public class ColorEntry : IDisposable
    {
        private HandleRef swigCPtr;
        protected bool swigCMemOwn;
        protected object swigParentRef;

        protected static object ThisOwn_true() { return null; }
        protected object ThisOwn_false() { return this; }

        public ColorEntry(IntPtr cPtr, bool cMemoryOwn, object parent)
        {
            swigCMemOwn = cMemoryOwn;
            swigParentRef = parent;
            swigCPtr = new HandleRef(this, cPtr);
        }

        public static HandleRef getCPtr(ColorEntry obj)
        {
            return (obj == null) ? new HandleRef(null, IntPtr.Zero) : obj.swigCPtr;
        }
        public static HandleRef getCPtrAndDisown(ColorEntry obj, object parent)
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
        public static HandleRef getCPtrAndSetReference(ColorEntry obj, object parent)
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

        ~ColorEntry()
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
                    GdalPINVOKE.delete_ColorEntry(swigCPtr);
                }
                swigCPtr = new HandleRef(null, IntPtr.Zero);
                swigParentRef = null;
                GC.SuppressFinalize(this);
            }
        }

        public short c1
        {
            set
            {
                GdalPINVOKE.ColorEntry_c1_set(swigCPtr, value);
                if (GdalPINVOKE.SWIGPendingException.Pending)
                {
                    throw GdalPINVOKE.SWIGPendingException.Retrieve();
                }
            }
            get
            {
                short ret = GdalPINVOKE.ColorEntry_c1_get(swigCPtr);
                if (GdalPINVOKE.SWIGPendingException.Pending)
                {
                    throw GdalPINVOKE.SWIGPendingException.Retrieve();
                }

                return ret;
            }
        }

        public short c2
        {
            set
            {
                GdalPINVOKE.ColorEntry_c2_set(swigCPtr, value);
                if (GdalPINVOKE.SWIGPendingException.Pending)
                {
                    throw GdalPINVOKE.SWIGPendingException.Retrieve();
                }
            }
            get
            {
                short ret = GdalPINVOKE.ColorEntry_c2_get(swigCPtr);
                if (GdalPINVOKE.SWIGPendingException.Pending)
                {
                    throw GdalPINVOKE.SWIGPendingException.Retrieve();
                }

                return ret;
            }
        }

        public short c3
        {
            set
            {
                GdalPINVOKE.ColorEntry_c3_set(swigCPtr, value);
                if (GdalPINVOKE.SWIGPendingException.Pending)
                {
                    throw GdalPINVOKE.SWIGPendingException.Retrieve();
                }
            }
            get
            {
                short ret = GdalPINVOKE.ColorEntry_c3_get(swigCPtr);
                if (GdalPINVOKE.SWIGPendingException.Pending)
                {
                    throw GdalPINVOKE.SWIGPendingException.Retrieve();
                }

                return ret;
            }
        }

        public short c4
        {
            set
            {
                GdalPINVOKE.ColorEntry_c4_set(swigCPtr, value);
                if (GdalPINVOKE.SWIGPendingException.Pending)
                {
                    throw GdalPINVOKE.SWIGPendingException.Retrieve();
                }
            }
            get
            {
                short ret = GdalPINVOKE.ColorEntry_c4_get(swigCPtr);
                if (GdalPINVOKE.SWIGPendingException.Pending)
                {
                    throw GdalPINVOKE.SWIGPendingException.Retrieve();
                }

                return ret;
            }
        }

        public ColorEntry() : this(GdalPINVOKE.new_ColorEntry(), true, null)
        {
            if (GdalPINVOKE.SWIGPendingException.Pending)
            {
                throw GdalPINVOKE.SWIGPendingException.Retrieve();
            }
        }

    }

}

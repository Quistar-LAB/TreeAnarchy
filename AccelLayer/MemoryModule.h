#pragma once
#ifndef __MEMORYMODULE_H__
#define __MEMORYMODULE_H__

using namespace System;
using namespace System::Runtime::InteropServices;

namespace TreeAnarchy {
    public ref class MemoryModule {
    private:
        enum DllReason : unsigned int {
            DLL_PROCESS_ATTACH = 1,
            DLL_THREAD_ATTACH = 2,
            DLL_THREAD_DETACH = 3,
            DLL_PROCESS_DETACH = 0
        };
    public:
        ref class DllException : Exception {
        public:
            DllException() : Exception() { }
            DllException(String^ message) : Exception(message) { }
            DllException(String^ message, Exception^ innerException) : Exception(message, innerException) { }
        };

        property bool isDll { bool get(); private: void set(bool); }
        
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        delegate bool DllEntryDelegate(IntPtr hinstDLL, DllReason fdwReason, IntPtr lpReserved);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        delegate int ExeEntryDelegate();

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        delegate void ImageTlsDelegate(IntPtr dllHandle, DllReason reason, IntPtr reserved);

        IntPtr pCode = IntPtr::Zero;
        IntPtr pNTHeaders = IntPtr::Zero;
        array<IntPtr>^ ImportModules;
        bool _initialized = false;
        DllEntryDelegate^ _dllEntry = nullptr;
        ExeEntryDelegate^ _exeEntry = nullptr;
        bool _isRelocated = false;

        /// <summary>
        /// Returns a delegate for a function inside the DLL.
        /// </summary>
        /// <typeparam name="TDelegate">The type of the delegate.</typeparam>
        /// <param name="funcName">The name of the function to be searched.</param>
        /// <returns>A delegate instance of type TDelegate</returns>
        /// 
        /*
        generic<class TDelegate> delegate TDelegate GetDelegateFromFuncName(String^ funcName) {
            
        }
        template<TDelegate> GetDelegateFromFuncName<TDelegate>(string funcName) where TDelegate : class {
            if (!typeof(Delegate).IsAssignableFrom(typeof(TDelegate))) throw new ArgumentException(typeof(TDelegate).Name + " is not a delegate");
            if (!(Marshal.GetDelegateForFunctionPointer((IntPtr)GetPtrFromFuncName(funcName), typeof(TDelegate)) is TDelegate res))
                throw new DllException("Unable to get managed delegate");
            return res;
        }
        */


        ~MemoryModule() {
            this->!MemoryModule();
        }
    protected:
        !MemoryModule() {

        }

    };
}

#endif /* __ MEMORYMODULE_H__ */
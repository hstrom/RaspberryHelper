using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace RaspberryHelper
{
    public class Shortcut
    {
        private const int MAX_DESCRIPTION_LENGTH = 512;
        private const int MAX_PATH = 512;

        private NativeClasses.IShellLinkW _link;

        public Shortcut()
        {
            this._link = NativeClasses.CreateShellLink();
        }

        public Shortcut(string path)
            : this()
        {
            Marshal.ThrowExceptionForHR(this._link.SetPath(path));
        }

        public string Path
        {
            get
            {
                NativeClasses._WIN32_FIND_DATAW fdata = new NativeClasses._WIN32_FIND_DATAW();
                StringBuilder path = new StringBuilder(MAX_PATH, MAX_PATH);

                Marshal.ThrowExceptionForHR(
                    this._link.GetPath(path, path.MaxCapacity, ref fdata, NativeClasses.SLGP_UNCPRIORITY)
                    );

                return path.ToString();
            }

            set
            {
                Marshal.ThrowExceptionForHR(
                    this._link.SetPath(value)
                    );
            }
        }

        public string Description
        {
            get
            {
                StringBuilder desc = new StringBuilder(MAX_DESCRIPTION_LENGTH, MAX_DESCRIPTION_LENGTH);
                Marshal.ThrowExceptionForHR(
                    this._link.GetDescription(desc, desc.MaxCapacity)
                    );

                return desc.ToString();
            }

            set
            {
                Marshal.ThrowExceptionForHR(
                    this._link.SetDescription(value)
                    );
            }
        }

        public string RelativePath
        {
            set
            {
                Marshal.ThrowExceptionForHR(
                    this._link.SetRelativePath(value, 0)
                    );
            }
        }

        public string WorkingDirectory
        {
            get
            {
                StringBuilder dir = new StringBuilder(MAX_PATH, MAX_PATH);
                Marshal.ThrowExceptionForHR(
                    this._link.GetWorkingDirectory(dir, dir.MaxCapacity)
                    );

                return dir.ToString();
            }

            set
            {
                Marshal.ThrowExceptionForHR(
                    this._link.SetWorkingDirectory(value)
                    );
            }
        }

        public string Arguments
        {
            get
            {
                StringBuilder args = new StringBuilder(MAX_PATH, MAX_PATH);
                Marshal.ThrowExceptionForHR(
                    this._link.GetArguments(args, args.MaxCapacity)
                    );

                return args.ToString();
            }

            set
            {
                Marshal.ThrowExceptionForHR(
                    this._link.SetArguments(value)
                    );
            }
        }

        public ushort HotKey
        {
            get
            {
                ushort key = 0;
                Marshal.ThrowExceptionForHR(
                    this._link.GetHotkey(out key)
                    );

                return key;
            }

            set
            {
                Marshal.ThrowExceptionForHR(
                    this._link.SetHotkey(value)
                    );
            }
        }

        public void Resolve(IntPtr hwnd, uint flags)
        {
            Marshal.ThrowExceptionForHR(
                this._link.Resolve(hwnd, flags)
                );
        }

        public void Resolve(IWin32Window window)
        {
            this.Resolve(window.Handle, 0);
        }

        public void Resolve()
        {
            this.Resolve(IntPtr.Zero, (uint)NativeClasses.SLR_MODE.SLR_NO_UI);
        }

        private NativeClasses.IPersistFile AsPersist
        {
            get { return ((NativeClasses.IPersistFile)this._link); }
        }

        public void Save(string fileName)
        {
            int hres = this.AsPersist.Save(fileName, true);

            Marshal.ThrowExceptionForHR(hres);
        }

        public void Load(string fileName)
        {
            int hres = this.AsPersist.Load(fileName, (uint)NativeClasses.STGM_ACCESS.STGM_READ);

            Marshal.ThrowExceptionForHR(hres);
        }
    }
}
